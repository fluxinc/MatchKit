# Grabador

Grabador is a .NET console application designed to extract text from the UI of other Windows applications using the UI Automation framework and apply a regular expression to find specific data.

## Usage

```bash
GrabadorConsole <window_identifier> <regex_pattern>
```

### Parameters

*   `<window_identifier>`: Specifies the target window. This can be either the main window title or the process name of the application.
*   `<regex_pattern>`: The regular expression to search for within the extracted text from the target window's controls.

### Examples

1.  **Extracting text matching a pattern from Notepad:**

    ```bash
    GrabadorConsole "Untitled - Notepad" "\\d{4}"
    ```
    This command attempts to find a window with the title "Untitled - Notepad" and then searches for any sequence of four digits within its text content.

2.  **Extracting text matching a pattern from a process named `MyApplication`:**

    ```bash
    GrabadorConsole MyApplication "Invoice Number: (\\w+)"
    ```
    This command attempts to find a window belonging to the process `MyApplication` and extracts the value following "Invoice Number: ". The parentheses capture the specific value.

## Finding Window Title or Process Name

To find the correct `<window_identifier>` for a running application, you can use tools like:

*   **Task Manager:** Open Task Manager (Ctrl+Shift+Esc), go to the 'Details' tab to see process names, or the 'Applications' tab (in older Windows versions) or 'Processes' tab to see window titles.
*   **PowerShell (for process names):** Use the command `Get-Process | Select-Object Name, Id` to list process names and their IDs.
*   **UI Automation Inspect Tool (inspect.exe):** This developer tool from the Windows SDK allows you to inspect UI elements and view their properties, including window titles and process IDs. You can search for "Inspect" in the Windows search bar if you have the SDK installed.

When using the window title, ensure it exactly matches the title bar text of the target window. Using the process name can be more reliable if the window title changes dynamically.
