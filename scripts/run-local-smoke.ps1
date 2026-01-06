param(
    [switch]$ForceOcr
)

# Creates a sample PostScript file, converts to PDF using Ghostscript (or ps2pdf), runs the renamer and verifies output
$sampleDir = Join-Path -Path (Get-Location) -ChildPath "ci-sample"
if (Test-Path $sampleDir) { Remove-Item $sampleDir -Recurse -Force }
New-Item -Path $sampleDir -ItemType Directory | Out-Null

$ps = @'
%!PS-Adobe-3.0
/Times-Roman findfont 24 scalefont setfont
72 720 moveto
(CA-G.R. SP No. 06110-MIN) show
showpage
'@
$psPath = Join-Path $sampleDir "sample.ps"
Set-Content -Path $psPath -Value $ps -Encoding Ascii

# Convert to PDF
$gs = Get-Command gswin64c -ErrorAction SilentlyContinue
if ($gs) {
    & $gs.Path -dNOPAUSE -dBATCH -sDEVICE=pdfwrite -sOutputFile="$sampleDir\sample.pdf" $psPath
} else {
    $ps2pdf = Get-Command ps2pdf -ErrorAction SilentlyContinue
    if ($ps2pdf) {
        & $ps2pdf.Path $psPath $sampleDir\sample.pdf
    } else {
        Write-Error "Neither gswin64c nor ps2pdf found. Please install Ghostscript or Poppler (ps2pdf)."; exit 1
    }
}

# Run renamer
$args = @("-d", $sampleDir)
if ($ForceOcr) { $args += "-o" }
Write-Host "Running: dotnet run --project src/RenameDocument -- $($args -join ' ')"
dotnet run --project src/RenameDocument -- $args

# Verify
$expected = Join-Path $sampleDir "SP No. 06110-MIN.pdf"
if (Test-Path $expected) { Write-Host "Smoke test passed: $expected"; exit 0 } else { Write-Error "Smoke test failed: expected file missing"; Get-ChildItem $sampleDir; exit 1 }
