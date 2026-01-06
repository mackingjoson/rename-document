using System.Text.RegularExpressions;
using System.Diagnostics;
using Tesseract;
using Ghostscript.NET.Rasterizer;
using Serilog;

public class PdfRenamer
{
    private readonly Options _options;

    public PdfRenamer(Options options)
    {
        _options = options;
    }

    public async Task<RenameReport> ProcessDirectoryAsync(string directory)
    {
        var report = new RenameReport();
        var files = Directory.GetFiles(directory, "*.pdf");

        foreach (var file in files)
        {
            Log.Information("Processing {File}", Path.GetFileName(file));
            try
            {
                var text = ExtractTextFromPdf(file);
                if (string.IsNullOrWhiteSpace(text))
                {
                    Log.Debug("No text found, using OCR for {File}", Path.GetFileName(file));
                    text = OcrPdf(file);
                }

                var match = FindCaseIdentifier(text);
                if (match == null)
                {
                    report.Unmatched.Add(Path.GetFileName(file));
                    Log.Warning("No case id found in {File}", Path.GetFileName(file));
                    continue;
                }

                var newName = BuildFileName(match);

                var newPath = Path.Combine(directory, newName);
                if (File.Exists(newPath))
                {
                    if (_options.ConflictPolicy?.ToLower() == "increment")
                    {
                        newPath = Utilities.MakeIncrementalPath(newPath);
                    }
                    else
                    {
                        report.Duplicates.Add((Path.GetFileName(file), newName));
                        Log.Warning("Conflict for {File} -> {New}, skipping", file, newName);
                        continue;
                    }
                }

                if (_options.DryRun)
                {
                    report.Renamed.Add((Path.GetFileName(file), Path.GetFileName(newPath)));
                    Log.Information("Dry-run: would rename {File} -> {New}", Path.GetFileName(file), Path.GetFileName(newPath));
                }
                else
                {
                    File.Move(file, newPath);
                    report.Renamed.Add((Path.GetFileName(file), Path.GetFileName(newPath)));
                    Log.Information("Renamed {File} -> {New}", Path.GetFileName(file), Path.GetFileName(newPath));
                }
            }
            catch (Exception ex)
            {
                report.Errors.Add((Path.GetFileName(file), ex.Message));
                Log.Error(ex, "Error processing {File}", Path.GetFileName(file));
            }
        }

        return report;
    }

    internal static string? FindCaseIdentifier(string text)
    {
        // Robust regex allowing spacing and optional dashes and suffixes
        var pattern = @"CA-?G\.?R\.?\s*SP\s*No\.?\s*([0-9]+(?:\s*[-–]\s*[A-Za-z0-9]+)*)";
        var rx = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        var m = rx.Match(text);
        if (!m.Success) return null;

        var numberAndSuffix = Regex.Replace(m.Groups[1].Value, "\s*[-–]\s*", "-");
        numberAndSuffix = numberAndSuffix.Trim();
        return $"SP No. {numberAndSuffix}";
    }

    internal static string BuildFileName(string matchedSPNo)
    {
        var safe = string.Join("", matchedSPNo.Split(Path.GetInvalidFileNameChars()));
        return safe + ".pdf";
    }

    // Use Utilities.MakeIncrementalPath for conflict resolution


    private static string ExtractTextFromPdf(string path)
    {
        // Prefer using `pdftotext` (from poppler-utils) for fast extraction if available.
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "pdftotext",
                Arguments = $"-layout \"{path}\" -",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var p = Process.Start(psi);
            if (p == null) return string.Empty;
            var outp = p.StandardOutput.ReadToEnd();
            p.WaitForExit(5000);
            if (!string.IsNullOrWhiteSpace(outp)) return outp;
        }
        catch { }

        // No embedded text extraction available; caller will fallback to OCR
        return string.Empty;
    }

    private static string OcrPdf(string path)
    {
        var sb = new System.Text.StringBuilder();
        var tempDir = Path.Combine(Path.GetTempPath(), "RenameDocument_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var gs = FindGhostscriptExe();
            if (gs == null)
                throw new InvalidOperationException("Ghostscript executable not found. Please install Ghostscript and ensure it's available in PATH.");

            var outPattern = Path.Combine(tempDir, "page-%03d.png");
            var args = $"-dNOPAUSE -dBATCH -sDEVICE=png16m -r200 -sOutputFile=\"{outPattern}\" \"{path}\"";

            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = gs,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var proc = System.Diagnostics.Process.Start(psi))
            {
                proc.WaitForExit();
                if (proc.ExitCode != 0)
                {
                    var err = proc.StandardError.ReadToEnd();
                    throw new InvalidOperationException($"Ghostscript conversion failed: {err}");
                }
            }

            var images = Directory.GetFiles(tempDir, "page-*.png").OrderBy(x => x).ToArray();

            using var engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default);
            foreach (var img in images)
            {
                using var pix = Pix.LoadFromFile(img);
                using var page = engine.Process(pix);
                sb.AppendLine(page.GetText());
            }
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { }
        }

        return sb.ToString();
    }

    private static string? FindGhostscriptExe()
    {
        var candidates = new[] { "gs", "gswin64c.exe", "gswin32c.exe", "gs.exe" };
        foreach (var c in candidates)
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo { FileName = c, Arguments = "--version", RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
                using var p = System.Diagnostics.Process.Start(psi);
                if (p == null) continue;
                p.WaitForExit(2000);
                if (p.ExitCode == 0) return c;
            }
            catch { }
        }
        return null;
    }
}

public class RenameReport
{
    public List<(string Source, string Target)> Renamed { get; } = new();
    public List<string> Unmatched { get; } = new();
    public List<(string Source, string Target)> Duplicates { get; } = new();
    public List<(string Source, string Error)> Errors { get; } = new();

    public string GetSummary()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Renamed files:");
        foreach (var r in Renamed)
            sb.AppendLine($"{r.Source} -> {r.Target}");
        sb.AppendLine();
        sb.AppendLine("Unmatched files:");
        foreach (var u in Unmatched)
            sb.AppendLine(u);
        sb.AppendLine();
        sb.AppendLine("Duplicates/conflicts:");
        foreach (var d in Duplicates)
            sb.AppendLine($"{d.Source} -> {d.Target}");
        sb.AppendLine();
        sb.AppendLine("Errors:");
        foreach (var e in Errors)
            sb.AppendLine($"{e.Source} : {e.Error}");

        return sb.ToString();
    }
}