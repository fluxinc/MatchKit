# DXA Desk Integration Guide for Grabador

This guide explains how to use Grabador to integrate with DXA Desk bone density reporting system, replacing the AutoIt scripts (PSOne.au3 and Mmodal.au3) with a more maintainable solution.

## Overview

DXA Desk provides a REST API that returns bone density reports in JSON format. The typical workflow:

1. Extract accession number from radiology reporting software (PowerScribe One or MModal Fluency)
2. Call DXA Desk API with the accession number and desired template
3. Extract the report text from JSON response
4. Paste the report into the radiology software

## DXA Desk API Structure

### Base URL Pattern

```
http://<host>/en/show/exam/<exam_id>/template/<template_id>/format/text.json
```

For MModal with prefix:

```
http://<host>/en/show/exam/prefix/<exam_id>/template/<template_id>/format/text.json
```

### Parameters

- `<host>`: DXA Desk server (e.g., `10.200.63.74:3000` or `192.168.49.3:3000`)
- `<exam_id>`: Accession number extracted from radiology software
- `<template_id>`: Report template number:
  - 1: Basic Report
  - 4: Concise Report
  - 5: Full Report
  - 6: Alternate Report
  - 8: Hybrid Report
  - 9: Linear Complete Report

### Response Format

```json
{
  "body": "Report text content...",
  "instance_id": 12345,
  "exam_id": "000955612",
  "template": 5,
  // other fields...
}
```

## PowerScribe One Integration

### Extracting Accession Number

PowerScribe One displays the accession number in the report header in format:

```
Patient Name - MRN - AccessionNumber\Other Info
```

### Configuration Examples

#### Template 1 - Basic Report

```bash
Grabador.Tray.exe ^
  -w "Nuance PowerScribe One" ^
  -r "(\d{8,})" ^
  -u "http://10.200.63.74:3000/en/show/exam/$1/template/1/format/text.json" ^
  -j "body" ^
  -h "Ctrl+1"
```

#### Template 4 - Concise Report (Ctrl+R)

```bash
Grabador.Tray.exe ^
  -w "Nuance PowerScribe One" ^
  -r "(\d{8,})" ^
  -u "http://10.200.63.74:3000/en/show/exam/$1/template/4/format/text.json" ^
  -j "body" ^
  -h "Ctrl+R"
```

#### Template 5 - Full Report (Ctrl+T)

```bash
Grabador.Tray.exe ^
  -w "Nuance PowerScribe One" ^
  -r "(\d{8,})" ^
  -u "http://10.200.63.74:3000/en/show/exam/$1/template/5/format/text.json" ^
  -j "body" ^
  -h "Ctrl+T"
```

#### Template 8 - Hybrid Report (Ctrl+D)

```bash
Grabador.Tray.exe ^
  -w "Nuance PowerScribe One" ^
  -r "(\d{8,})" ^
  -u "http://10.200.63.74:3000/en/show/exam/$1/template/8/format/text.json" ^
  -j "body" ^
  -h "Ctrl+D"
```

## MModal Fluency Integration

MModal stores accession numbers in log files, but you can also extract from the UI.

### Configuration for MModal

```bash
Grabador.Tray.exe ^
  -w "Fluency for Imaging Reporting" ^
  -r "(\d{8,}\w*)" ^
  -u "http://192.168.49.3:3000/en/show/exam/prefix/$1/template/4/format/text.json" ^
  -j "body" ^
  -h "Ctrl+R"
```

Note: MModal API uses `/prefix/` in the URL path.

## SpeechQ Integration

SpeechQ is another radiology reporting system that can be integrated with DXA Desk.

### Configuration for SpeechQ

#### Template 4 - Concise Report (Ctrl+R)
```bash
Grabador.Tray.exe ^
  -w "SpeechQ" ^
  -r "(\d{8,}\w*)" ^
  -u "http://192.168.173.30:3000/en/show/exam/$1/template/4.json" ^
  -j "body" ^
  -h "Ctrl+R"
```

#### Template 5 - Full Report (Ctrl+T)
```bash
Grabador.Tray.exe ^
  -w "SpeechQ" ^
  -r "(\d{8,}\w*)" ^
  -u "http://192.168.173.30:3000/en/show/exam/$1/template/5.json" ^
  -j "body" ^
  -h "Ctrl+T"
```

Note: SpeechQ API endpoints use `.json` instead of `/format/text.json`. Accession numbers should be extracted from the UI using a regex, just like with other systems.

## Talk Technology Integration

Talk Technology (also known as talktech) is yet another radiology reporting platform.

### Configuration for Talk

#### Template 4 - Concise Report (Ctrl+R)
```bash
Grabador.Tray.exe ^
  -w "Talk" ^
  -r "(\d{8,}\w*)" ^
  -u "http://10.200.63.74:3000/en/show/exam/$1/template/4/format/text.json" ^
  -j "body" ^
  -h "Ctrl+R"
```

#### Template 8 - Hybrid Report (Ctrl+T)
```bash
Grabador.Tray.exe ^
  -w "Talk" ^
  -r "(\d{8,}\w*)" ^
  -u "http://10.200.63.74:3000/en/show/exam/$1/template/8/format/text.json" ^
  -j "body" ^
  -h "Ctrl+T"
```

Note: Talk uses template 8 for what other systems call "Full Report"

## Complete Setup Example

### Step 1: Test with Console

First, verify the configuration works:

```bash
# Open PowerScribe with a patient loaded
Grabador.exe -w "PowerScribe" -r "(\d{8,})" -d

# If accession found, test API call
Grabador.exe ^
  -w "PowerScribe" ^
  -r "(\d{8,})" ^
  -u "http://10.200.63.74:3000/en/show/exam/$1/template/1/format/text.json" ^
  -j "body" ^
  -d
```

### Step 2: Deploy Multiple Hotkeys

Create a batch file to start multiple instances for different templates:

**start-dxa-desk-powerscribe.bat**

```batch
@echo off
echo Starting DXA Desk Integration for PowerScribe...

REM Template 1 - Basic (Ctrl+1)
start "" Grabador.Tray.exe -w "Nuance PowerScribe One" -r "(\d{8,})" ^
  -u "http://10.200.63.74:3000/en/show/exam/$1/template/1/format/text.json" ^
  -j "body" -h "Ctrl+1"

REM Template 4 - Concise (Ctrl+R)
start "" Grabador.Tray.exe -w "Nuance PowerScribe One" -r "(\d{8,})" ^
  -u "http://10.200.63.74:3000/en/show/exam/$1/template/4/format/text.json" ^
  -j "body" -h "Ctrl+R"

REM Template 5 - Full (Ctrl+T)
start "" Grabador.Tray.exe -w "Nuance PowerScribe One" -r "(\d{8,})" ^
  -u "http://10.200.63.74:3000/en/show/exam/$1/template/5/format/text.json" ^
  -j "body" -h "Ctrl+T"

REM Template 8 - Hybrid (Ctrl+D)
start "" Grabador.Tray.exe -w "Nuance PowerScribe One" -r "(\d{8,})" ^
  -u "http://10.200.63.74:3000/en/show/exam/$1/template/8/format/text.json" ^
  -j "body" -h "Ctrl+D"

echo All hotkeys registered. Check system tray.
```

## System Comparison Table

| System | Window Title | API Format | Server | Special Notes |
|--------|-------------|------------|---------|---------------|
| PowerScribe One | "Nuance PowerScribe One" | `/template/{id}/format/text.json` | 10.200.63.74:3000 | Most common |
| MModal Fluency | "Fluency for Imaging Reporting" | `/prefix/` + `/template/{id}/format/text.json` | 192.168.49.3:3000 | Uses prefix in path |
| SpeechQ | "SpeechQ" | `/template/{id}.json` | 192.168.173.30:3000 | Different JSON endpoint |
| Talk Technology | "Talk" | `/template/{id}/format/text.json` | 10.200.63.74:3000 | Template 8 for Full Report |

## All-in-One Configuration Script

For environments with multiple reporting systems, create separate batch files for each:

**start-all-dxa-systems.bat**
```batch
@echo off
echo Starting DXA Desk Integration for all systems...

REM Check which system is running and start appropriate configuration
tasklist /FI "IMAGENAME eq PowerScribe*" 2>NUL | find /I /N "PowerScribe">NUL
if "%ERRORLEVEL%"=="0" call start-dxa-desk-powerscribe.bat

tasklist /FI "IMAGENAME eq Fluency*" 2>NUL | find /I /N "Fluency">NUL
if "%ERRORLEVEL%"=="0" call start-dxa-desk-mmodal.bat

tasklist /FI "IMAGENAME eq SpeechQ*" 2>NUL | find /I /N "SpeechQ">NUL
if "%ERRORLEVEL%"=="0" call start-dxa-desk-speechq.bat

tasklist /FI "IMAGENAME eq Talk*" 2>NUL | find /I /N "Talk">NUL
if "%ERRORLEVEL%"=="0" call start-dxa-desk-talk.bat
```

## Regex Patterns for Accession Numbers

### Standard Patterns

- `(\d{8,})` - 8 or more digits (most common)
- `(\d{8,}\w*)` - 8+ digits possibly followed by letters
- `(\d{10})` - Exactly 10 digits
- `([A-Z]{2}\d{8})` - 2 letters followed by 8 digits

### Testing Your Pattern

```bash
# List windows to find exact title
Grabador.exe --list-windows

# Test extraction
Grabador.exe -w "YourWindowTitle" -r "YourRegexPattern" -d
```

## Troubleshooting

### No Accession Found

1. Check window title matches exactly
2. Verify regex pattern matches your accession format
3. Use debug mode to see all extracted text
4. Try a broader pattern like `(\w{8,})`

### API Connection Failed

1. Verify DXA Desk server is accessible
2. Check firewall settings
3. Test URL in browser: `http://your-server:3000/en/show/exam/12345678/template/1/format/text.json`

### Wrong Report Retrieved

1. Verify accession number format
2. Check if prefix is needed in URL
3. Confirm template ID matches desired report type

### Report Not Pasting

1. Ensure cursor is in the correct field
2. Check if application allows paste operations
3. Try manual Ctrl+V after extraction

## Advanced Configuration

### Custom JSON Paths

If DXA Desk API structure changes:

```bash
# For nested response
-j "data.report.body"

# For array response
-j "reports[0].body"
```

### Multiple Servers

For failover between servers:

```batch
REM Primary server
start "" Grabador.Tray.exe -w "PowerScribe" -r "(\d{8,})" ^
  -u "http://10.200.63.74:3000/en/show/exam/$1/template/5/format/text.json" ^
  -j "body" -h "Ctrl+T"

REM Backup server (different hotkey)
start "" Grabador.Tray.exe -w "PowerScribe" -r "(\d{8,})" ^
  -u "http://192.168.49.3:3000/en/show/exam/$1/template/5/format/text.json" ^
  -j "body" -h "Ctrl+Shift+T"
```

## Migration from AutoIt

### PSOne.au3 Hotkey Mapping

- `Ctrl+R` → Template 4 (Concise)
- `Ctrl+T` → Template 5 (Full)
- `Ctrl+D` → Template 8 (Hybrid)
- `Ctrl+8` → Template 8 (Hybrid)
- `Ctrl+9` → Template 5 (Full)

### MModal.au3 Hotkey Mapping

- `Ctrl+R` → Template 4 (Concise)
- `Ctrl+D` → Template 8 (Hybrid)
- `Ctrl+U` → Template 6 (Alternate)
- `Ctrl+H` → Template 9 (Linear Complete)

### SpeechQ.au3 Hotkey Mapping

- `Ctrl+R` → Template 4 (Concise)
- `Ctrl+T` → Template 5 (Full)
- `Ctrl+I` → Display Instance (web view)

### Talk.au3 Hotkey Mapping

- `Ctrl+R` → Template 4 (Concise)
- `Ctrl+T` → Template 8 (Hybrid) - Note: Different template!
- `Ctrl+I` → Display Instance (web view)

## Security Considerations

1. **Network Access**: Ensure workstations can reach DXA Desk server
2. **HTTPS**: If available, use `https://` URLs
3. **Authentication**: Current DXA Desk API doesn't require auth, but be prepared if this changes

## Support

For DXA Desk API issues, contact your PACS administrator.
For Grabador issues, see the main [README](README.md) or submit a GitHub issue.
