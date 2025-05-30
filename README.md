# MatchKit

A powerful Windows automation tool that extracts text from any application using UI Automation, processes it with regex patterns, optionally calls web APIs, and can paste results back into applications. Available as both a console application and a system tray application with global hotkey support.

## Features

- **Text Extraction**: Extract text from any Windows application using UI Automation
- **Regex Matching**: Apply regular expressions to find specific patterns in extracted text
- **API Integration**: Call REST APIs with extracted data and process JSON responses
- **JSON Path Extraction**: Extract specific values from JSON responses using dot notation
- **Hotkey Automation**: System tray app with configurable global hotkeys
- **Clipboard Integration**: Automatically paste results at cursor position
- **Debug Mode**: Detailed logging for troubleshooting

## Components

### MatchKit.Core

Shared library providing core functionality:

- `TextAutomationService`: UI Automation for window finding and text extraction
- `HttpUtilityService`: HTTP client for API calls
- `AutomationOrchestrator`: Workflow orchestration used by both console and tray apps

### MatchKit (Console Application)

Command-line tool for direct automation tasks:

- List available windows
- Extract text using regex patterns
- Call APIs and process responses
- Perfect for testing configurations

### MatchKit.Tray (System Tray Application)

Windows system tray application for hotkey-triggered automation:

- Global hotkey registration
- Clipboard-based pasting
- User-friendly notifications
- Persistent configuration

## Installation

### Prerequisites

- Windows OS (Windows 7 or later)
- .NET Framework 4.8.1
- Visual Studio 2019 or later (for building from source)

### Building from Source

```bash
# Clone the repository
git clone https://github.com/fluxinc/MatchKit.git
cd MatchKit

# Build the solution
msbuild MatchKit.sln /p:Configuration=Release

# Or build individual projects
msbuild MatchKit.Core\MatchKit.Core.csproj
msbuild MatchKit\MatchKit.csproj
msbuild MatchKit.Tray\MatchKit.Tray.csproj
```

### Binary Installation

Download the latest release from the [Releases](https://github.com/fluxinc/MatchKit/releases) page.

## Usage

### Console Application (MatchKit.exe)

#### List Available Windows

```bash
MatchKit.exe --list-windows
```

#### Basic Text Extraction

```bash
# Extract a 4-digit number from Notepad
MatchKit.exe -w "notepad" -r "\d{4}"

# Extract using process name
MatchKit.exe -w "notepad.exe" -r "Invoice: (\w+)"
```

#### With API Integration

```bash
# Extract text, call API, and display response
MatchKit.exe -w "MyApp" -r "ID: (\d+)" -u "http://api.example.com/items/$1"

# Extract text, call API, and extract JSON field
MatchKit.exe -w "MyApp" -r "ID: (\d+)" -u "http://api.example.com/items/$1" -j "data.name"
```

#### Command-Line Options

- `-w, --window`: Window identifier (process name or title regex) **[Required]**
- `-r, --regex`: Regular expression pattern **[Required]**
- `-u, --url`: URL template with $1 placeholder for extracted text
- `-j, --json-key`: JSON path to extract from response (dot notation)
- `-l, --list-windows`: List all available windows
- `-d, --debug`: Enable debug logging
- `-c, --config`: Interactive configuration mode (requires admin privileges)
- `-s, --save`: Saves command line settings to registry (requires admin privileges)

### System Tray Application (MatchKit.Tray.exe)

#### Basic Usage

```bash
# Simple hotkey automation
MatchKit.Tray.exe -w "notepad" -r "\d{4}" -h "Ctrl+R"

# With API call and JSON extraction
MatchKit.Tray.exe -w "MyApp" -r "ID: (\d+)" -u "http://api.example.com/items/$1" -j "data.name" -h "Ctrl+D"
```

#### Command-Line Options

- `-w, --window`: Window identifier (process name or title regex) **[Required]**
- `-r, --regex`: Regular expression pattern **[Required]**
- `-h, --hotkey`: Hotkey combination (default: "Ctrl+D")
- `-u, --url`: URL template with $1 placeholder
- `-j, --json-key`: JSON path to extract from response
- `-d, --debug`: Enable debug logging

#### Supported Hotkey Formats

- Single key + modifier: `Ctrl+R`, `Alt+F1`, `Shift+A`
- Multiple modifiers: `Ctrl+Shift+R`, `Ctrl+Alt+D`
- Function keys: `F1` through `F12`
- Number keys: `0` through `9`
- Letter keys: `A` through `Z`

## Real-World Examples

### Extract and Look Up Invoice Numbers

```bash
# Console version
MatchKit.exe -w "InvoiceApp" -r "INV-(\d{6})" -u "http://erp.company.com/api/invoices/$1" -j "invoice.total"

# Tray version with hotkey
MatchKit.Tray.exe -w "InvoiceApp" -r "INV-(\d{6})" -u "http://erp.company.com/api/invoices/$1" -j "invoice.total" -h "Ctrl+I"
```

### Medical Report Integration (DXA/DEXA)

```bash
# Extract accession number and fetch report
MatchKit.Tray.exe -w "PowerScribe" -r "\d{8}" -u "http://10.200.63.74:3000/en/show/exam/$1/template/5/format/text.json" -j "body" -h "Ctrl+T"
```

### Customer ID Lookup

```bash
# Extract customer ID and fetch details
MatchKit.Tray.exe -w "CRM" -r "CUST(\d+)" -u "https://api.crm.com/customers/$1" -j "customer.email" -h "Alt+C"
```

## Testing Your Configuration

1. **Test with Console First**: Always test your regex and API calls with the console app:

   ```bash
   MatchKit.exe -w "YourApp" -r "YourRegex" -u "YourAPI" -j "json.path" -d
   ```

2. **Verify Output**: Ensure you're getting the expected results

3. **Deploy to Tray**: Once working, use the same parameters with MatchKit.Tray:

   ```bash
   MatchKit.Tray.exe -w "YourApp" -r "YourRegex" -u "YourAPI" -j "json.path" -h "Ctrl+R"
   ```

## JSON Path Syntax

The `-j` parameter supports standard JSON path notation:

- Simple property: `name`
- Nested property: `data.customer.name`
- Array index: `items[0]`
- Array property: `items[0].name`
- Deep nesting: `response.data.records[0].details.value`

## Troubleshooting

### Window Not Found

- Use `MatchKit.exe --list-windows` to see available windows
- Try using the exact process name (e.g., `notepad.exe`)
- For windows with changing titles, use a regex pattern

### Regex Not Matching

- Test your regex at [regex101.com](https://regex101.com)
- Use debug mode (`-d`) to see extracted text
- Remember that `()` creates capture groups - `$1` uses the first group

### API Issues

- Verify the URL is accessible
- Check if authentication is required
- Use debug mode to see the full response
- Test the API separately with tools like Postman

### Hotkey Conflicts

- Choose a hotkey not used by other applications
- Try different combinations if one doesn't work
- Some applications may intercept certain hotkeys

### Clipboard/Paste Issues

- Ensure the target application has focus
- Some applications may have paste restrictions
- Try increasing the delay in code if needed

## Architecture

```
MatchKit.sln
├── MatchKit.Core/           # Shared library
│   ├── TextAutomationService.cs
│   ├── HttpUtilityService.cs
│   └── AutomationOrchestrator.cs
├── MatchKit/                # Console application
│   └── Program.cs
└── MatchKit.Tray/          # System tray application
    ├── Program.cs
    ├── TrayApplicationContext.cs
    └── HotkeyParser.cs
```

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request. For major changes, please open an issue first to discuss what you would like to change.

### Development Guidelines

1. Maintain the separation between Core, Console, and Tray projects
2. Add debug logging for new features
3. Ensure error messages are user-friendly
4. Test with both console and tray applications
5. Update documentation for new features

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- Uses Windows UI Automation for text extraction
- Built with .NET Framework 4.8.1
- System.CommandLine for argument parsing
- Newtonsoft.Json for JSON processing

## Support

For issues, questions, or contributions, please use the [GitHub Issues](https://github.com/mostlydev/MatchKit/issues) page.
