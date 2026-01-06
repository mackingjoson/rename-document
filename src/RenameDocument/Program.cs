using System.CommandLine;
using CommandLine;
using Serilog;

class Program
{
    static async Task<int> Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();

        var parser = new Parser(opts => opts.HelpWriter = Console.Out);
        var result = parser.ParseArguments<Options>(args)
            .WithParsedAsync(RunOptionsAsync);

        await result;
        return 0;
    }

    static async Task RunOptionsAsync(Options opts)
    {
        Log.Information("Starting PDF renamer in {Dir}", opts.Directory);

        var processor = new PdfRenamer(opts);
        var report = await processor.ProcessDirectoryAsync(opts.Directory);

        // Write report to console and file
        var summary = report.GetSummary();
        Console.WriteLine(summary);

        File.WriteAllText(Path.Combine(opts.Directory, "rename_report.txt"), summary);

        Log.Information("Completed");
    }
}

class Options
{
    [Option('d', "directory", Required = true, HelpText = "Directory containing PDFs to process.")]
    public string Directory { get; set; } = string.Empty;

    [Option('n', "dry-run", Required = false, HelpText = "Perform a dry run without renaming files.")]
    public bool DryRun { get; set; } = false;

    [Option('c', "conflict", Required = false, HelpText = "Conflict policy: skip or increment", Default = "increment")]
    public string ConflictPolicy { get; set; } = "increment";
}