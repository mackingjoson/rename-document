# Rename Document (PDF Auto-Renamer)

A small .NET console app that scans PDFs and renames them using the case identifier found inside each PDF.

Goal: Look for `CA-G.R. SP No. <number-and-suffix>` and rename the file to `SP No. <number-and-suffix>.pdf`.

## Features
- Extract embedded text using UglyToad.PdfPig
- If no embedded text is found, render pages to images using Ghostscript.NET and run OCR with Tesseract
- Robust regex to handle spacing/dash variations
- Dry-run option and conflict handling (incremental `(1)`, `(2)` ...)
- Produces `rename_report.txt` with summary

## Requirements
- .NET 8 SDK (pinned via `global.json` to **8.0.415**)
- For OCR fallback: Ghostscript (CLI) and Tesseract (install on system). For faster embedded text extraction, install `pdftotext` (poppler-utils).
  - Ubuntu: `sudo apt-get install tesseract-ocr ghostscript poppler-utils`
  - Windows: install Ghostscript, Tesseract, and Poppler (pdftotext) (available via Chocolatey)

## Usage

dotnet run --project src/RenameDocument -- -d "C:\path\to\pdfs" [-n] [-c increment|skip]

- `-d`, `--directory`: required. Process all PDF files in this directory.
- `-n`, `--dry-run`: do not rename, only simulate.
- `-c`, `--conflict`: `increment` (default) or `skip`.

## Notes
- Place Tesseract `tessdata` folder in the working directory or system tessdata path for OCR to work.
- The solution contains unit tests covering the extraction regex logic.

## License
MIT
