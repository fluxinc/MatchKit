MatchKit - Post-Installation Guide

## Getting Started: Your First Automation

Create a simple automation: extract a number from Notepad.

**1. Prepare Target:**
   Open Notepad. Type: "Order ID: 12345"

**2. Find Window Identifier:**
   If unsure of window title or process name, use `MatchKit.exe --list-windows`.
   Look for "Notepad" or "notepad.exe".

**3. Test with Console (`MatchKit.exe`):**
   To extract "12345" (digits `\d+`) after "Order ID: ", use regex `Order ID: (\d+)`.
   `()` creates a capture group; `$1` refers to the number.

   ```bash
   # Extract any 4-digit number (using process name)
   MatchKit.exe -w "notepad.exe" -r "\d{4}"

   # Extract number after "Order ID: "
   MatchKit.exe -w "notepad.exe" -r "Order ID: (\d+)"
   ```
   MatchKit prints the extracted number if successful.

**4. (Optional) API Call & JSON Extraction:**
   ```bash
   MatchKit.exe -w "notepad.exe" -r "Order ID: (\d+)" -u "http://api.example.com/orders/$1" -j "orderDetails.status"
   ```
   Use `-d` (debug) to see the full API response for JSON path troubleshooting.

**5. (Optional) Save Configuration (`MatchKit.exe`):**
   Save settings for `MatchKit.Tray.exe` or `MatchKit.exe` defaults (requires admin):
   ```bash
   MatchKit.exe -w "notepad.exe" -r "Order ID: (\d+)" -u "http://api.example.com/orders/$1" -j "orderDetails.status" -k "Ctrl+Shift+N" --save
   ```
   Settings are stored in the registry.

**6. Use System Tray (`MatchKit.Tray.exe`):**
   - If configuration (including hotkey) was saved with `MatchKit.exe --save`:
     ```bash
     MatchKit.Tray.exe
     ```
     The tray app loads saved settings and registers the hotkey.
   - To run `MatchKit.Tray.exe` with a temporary configuration:
     ```bash
     MatchKit.Tray.exe -w "notepad.exe" -r "Order ID: (\d+)" -k "Ctrl+Alt+N"
     ```
   Press the hotkey (e.g., Ctrl+Shift+N) to trigger automation and paste.

**7. Configure Interactively:**
   Manage saved configurations (requires admin):
   - `MatchKit.exe --config`: Command-line configuration.
   - `MatchKit.Tray.exe --config`: GUI configuration editor.

## Usage Notes

### Command-Line Options

Key options for `MatchKit.exe` and `MatchKit.Tray.exe`:

- `-w, --window`: Window identifier (process name, title, or regex). **[Required]**
- `-r, --regex`: Regex pattern for extraction. Use `()` for capture groups. **[Required]**
- `-u, --url`: (Optional) URL template for API GET requests (e.g., `http://api.example.com/items/$1`).
- `-j, --json-key`: (Optional) Dot notation path for JSON value (e.g., `data.name`).
- `-d, --debug`: (Optional) Enable detailed debug logging.
- `-k, --hotkey`: (Tray app) Global hotkey (e.g., "Ctrl+Shift+R"). Also used with `MatchKit.exe --save`.
- `-c, --config`: Interactive configuration mode (admin rights needed to save).
- `-s, --save`: (`MatchKit.exe` only) Saves command-line settings to registry (admin rights needed).
- `-l, --list-windows`: (`MatchKit.exe` only) Lists open windows.

### Console Application (`MatchKit.exe`)

For direct automation and testing. Output is to console.

Examples:
```bash
MatchKit.exe --list-windows
MatchKit.exe -w "notepad.exe" -r "Invoice: (\w+)"
MatchKit.exe -w "MyApp" -r "ID: (\d+)" -u "http://api.example.com/items/$1" -j "data.name"
```

### System Tray Application (`MatchKit.Tray.exe`)

Runs in background for hotkey automation.

Examples:
```bash
# Run with saved configuration (set via --config or MatchKit.exe --save)
MatchKit.Tray.exe

# Override saved config for this session
MatchKit.Tray.exe -w "notepad" -r "\d{4}" -k "Ctrl+R"
```

Supported Hotkey Formats: `Ctrl+R`, `Alt+F1`, `Ctrl+Shift+R`, `F5`, `A`, etc.

## Testing Your Configuration

1.  **Console First**: Test regex and API calls with `MatchKit.exe`:
    ```bash
    MatchKit.exe -w "YourApp" -r "YourRegex" -u "YourAPI" -j "json.path" -d
    ```
2.  **Verify Output**: Check console for expected results. Use `-d` for debug details.
3.  **(Optional) Save for Tray**: If console command works, save with `MatchKit.exe --save`:
    ```bash
    MatchKit.exe -w "YourApp" -r "YourRegex" -u "YourAPI" -j "json.path" -k "Ctrl+R" --save
    ```
4.  **Deploy to Tray**: Run `MatchKit.Tray.exe` (ideally no arguments, to load saved config). Press hotkey.
    To test tray with direct parameters (no save):
    ```bash
    MatchKit.Tray.exe -w "YourApp" -r "YourRegex" -u "YourAPI" -j "json.path" -k "Ctrl+R"
    ```

## JSON Path Syntax

`-j` parameter uses standard JSON path:
- Property: `name`
- Nested: `data.customer.name`
- Array: `items[0].name`

## Troubleshooting

- **Window Not Found**: `MatchKit.exe --list-windows`. Try process name (`notepad.exe`). Use regex for dynamic titles.
- **Regex Not Matching**: Test at regex101.com. Use `-d` (debug). `()` creates capture groups; `$1` is the first.
- **API Issues**: Verify URL. Check auth. Use `-d` for full response. Test API with Postman.
- **Hotkey Conflicts**: Choose a unique hotkey. Try different combinations.
- **Clipboard/Paste**: Ensure target app has focus. Check for paste restrictions.

## Support

For issues or questions, see GitHub Issues for the project.
