#!/usr/bin/env bash
set -euo pipefail
SAMPLEDIR=./ci-sample
rm -rf "$SAMPLEDIR"
mkdir -p "$SAMPLEDIR"
cat > "$SAMPLEDIR/sample.ps" <<'PS'
% !PS-Adobe-3.0
/Times-Roman findfont 24 scalefont setfont
72 720 moveto
(CA-G.R. SP No. 06110-MIN) show
showpage
PS

if command -v gs >/dev/null 2>&1; then
  gs -dNOPAUSE -dBATCH -sDEVICE=pdfwrite -sOutputFile="$SAMPLEDIR/sample.pdf" "$SAMPLEDIR/sample.ps"
elif command -v ps2pdf >/dev/null 2>&1; then
  ps2pdf "$SAMPLEDIR/sample.ps" "$SAMPLEDIR/sample.pdf"
else
  echo "Neither gs nor ps2pdf found. Please install Ghostscript or Poppler (ps2pdf)." >&2
  exit 1
fi

# Run renamer
if [ "${1:-}" = "--force-ocr" ]; then
  dotnet run --project src/RenameDocument -- -d "$SAMPLEDIR" -o
else
  dotnet run --project src/RenameDocument -- -d "$SAMPLEDIR"
fi

if [ -f "$SAMPLEDIR/SP No. 06110-MIN.pdf" ]; then
  echo "Smoke test passed: $SAMPLEDIR/SP No. 06110-MIN.pdf"
  exit 0
else
  echo "Smoke test failed: expected file missing" >&2
  ls -la "$SAMPLEDIR"
  exit 1
fi