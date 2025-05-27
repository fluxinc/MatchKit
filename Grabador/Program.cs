using System;
using System.Linq; // Potentially used by System.CommandLine or future extensions
using System.CommandLine;
using System.Threading.Tasks;
// Remove using System.Net.Http; // No longer needed here
// Remove using System.Diagnostics; // If only used by removed Process.GetProcessesByName
// Remove using System.Windows.Automation; // Will be handled by Grabador.Core
// Remove using System.Text.RegularExpressions; // Will be handled by Grabador.Core
// Remove using System.Runtime.InteropServices; // If all P/Invokes are gone
// Remove using System.Windows.Forms; // If only used for hotkeys
using Grabador.Core; // For TextAutomationService and HttpUtilityService

namespace Grabador
{
    internal class Program
    {
        // All P/Invoke declarations for global hotkey, message loop structures (MSG, POINT),
        // and related constants (HOTKEY_ID, MOD_*, WM_HOTKEY, ERROR_*) are removed.
        // s_hotkeyAction is removed.

        public static async Task<int> Main(string[] args)
        {
            RootCommand rootCommand = new RootCommand("A tool to extract text from a window and apply a regex pattern.");

            Option<string> windowIdentifierOption = new Option<string?>(
                aliases: new[] { "--window-identifier", "-w" },
                description: "Process name (exact match) or main window title (regex match) of the target application.");

            Option<string> regexPatternOption = new Option<string?>(
                aliases: new[] { "--regex-pattern", "-r" },
                description: "Regular expression to search for within the window's controls.");

            Option<string> urlTemplateOption = new Option<string?>(
                aliases: new[] { "--url-template", "-u" },
                description: "URL template with $1 placeholder for the found text (e.g., http://host/api?text=$1).");

            Option<bool> listWindowsOption = new Option<bool>(
                aliases: new[] { "--list-windows", "-l" },
                description: "Lists the titles and process IDs of available top-level windows.");

            Option<bool> debugOption = new Option<bool>(
                aliases: new[] { "--debug", "-d" },
                description: "Enable detailed debug logging from the TextAutomationService and console app.",
                getDefaultValue: () => false);

            Option<string> jsonKeyOption = new Option<string?>(
                aliases: new[] { "--json-key", "-j" },
                description: "The key or dot-separated path of the value to extract from the JSON response (e.g., 'data.details.color').");

            rootCommand.AddOption(windowIdentifierOption);
            rootCommand.AddOption(regexPatternOption);
            rootCommand.AddOption(urlTemplateOption);
            rootCommand.AddOption(listWindowsOption);
            rootCommand.AddOption(debugOption);
            rootCommand.AddOption(jsonKeyOption);

            if (args.Length == 0 && !args.Any(arg => arg == "--list-windows" || arg == "-l"))
            {
                return await rootCommand.InvokeAsync("--help");
            }

            rootCommand.SetHandler(async (windowIdentifier, regexPattern, urlTemplate, listWindows, debugEnabled, jsonKey) =>
            {
                var orchestrator = new AutomationOrchestrator(debugEnabled);

                if (listWindows)
                {
                    if (debugEnabled) Console.WriteLine("[Grabador] --list-windows / -l flag active. Listing windows...");
                    orchestrator.ListAvailableWindows();
                }
                else
                {
                    if (string.IsNullOrEmpty(windowIdentifier) || string.IsNullOrEmpty(regexPattern))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Error: --window-identifier (-w) and --regex-pattern (-r) are required when --list-windows (-l) is not used.");
                        Console.ResetColor();
                        return;
                    }

                    var config = new AutomationOrchestrator.AutomationConfig
                    {
                        WindowIdentifier = windowIdentifier,
                        RegexPattern = regexPattern,
                        UrlTemplate = urlTemplate,
                        JsonKey = jsonKey,
                        DebugMode = debugEnabled
                    };

                    var result = await orchestrator.ExecuteAsync(config);

                    if (!result.Success)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Error: {result.Error}");
                        Console.ResetColor();
                    }
                    else
                    {
                        if (debugEnabled)
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("Operation completed successfully:");
                            Console.ResetColor();
                            if (!string.IsNullOrEmpty(result.ExtractedText))
                            {
                                Console.WriteLine($"Extracted text: {result.ExtractedText}");
                            }
                            if (!string.IsNullOrEmpty(result.RawResponse) && result.RawResponse != result.Value)
                            {
                                Console.WriteLine($"Raw response length: {result.RawResponse.Length} characters");
                            }
                        }
                        Console.WriteLine(result.Value);
                    }
                }
            }, windowIdentifierOption, regexPatternOption, urlTemplateOption, listWindowsOption, debugOption, jsonKeyOption);

            try
            {
                return await rootCommand.InvokeAsync(args);
            }
            finally
            {
                // Cleanup, if any.
            }
        }

        // Old ListWindows() method removed.
        // Old ProcessWindowAsync() method removed.
        // Old FindWindow() method removed.
        // Old GetTextFromElement() method removed.
        // Old ParseHotkey() method removed.
    }
}
