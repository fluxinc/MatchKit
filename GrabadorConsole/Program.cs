using System;
using System.Linq; // Potentially used by System.CommandLine or future extensions
using System.CommandLine;
using System.Threading.Tasks;
// Remove using System.Diagnostics; // If only used by removed Process.GetProcessesByName
// Remove using System.Windows.Automation; // Will be handled by Grabador.Core
// Remove using System.Text.RegularExpressions; // Will be handled by Grabador.Core
// Remove using System.Runtime.InteropServices; // If all P/Invokes are gone
// Remove using System.Windows.Forms; // If only used for hotkeys

using Grabador.Core; // For TextAutomationService

namespace GrabadorConsole
{
    internal class Program
    {
        // All P/Invoke declarations for global hotkey, message loop structures (MSG, POINT),
        // and related constants (HOTKEY_ID, MOD_*, WM_HOTKEY, ERROR_*) are removed.
        // s_hotkeyAction is removed.

        public static async Task<int> Main(string[] args)
        {
            var rootCommand = new RootCommand("A tool to extract text from a window and apply a regex pattern.");

            var windowIdentifierArgument = new Argument<string?>(
                name: "window_identifier",
                description: "Process name (exact match) or main window title (regex match) of the target application.",
                getDefaultValue: () => null);

            var regexPatternArgument = new Argument<string?>(
                name: "regex_pattern",
                description: "Regular expression to search for within the window's controls.",
                getDefaultValue: () => null);

            var listWindowsOption = new Option<bool>(
                "--list-windows",
                description: "Lists the titles and process IDs of available top-level windows.");

            var debugOption = new Option<bool>(
                name: "--debug",
                description: "Enable detailed debug logging from the TextAutomationService and console app.",
                getDefaultValue: () => false);

            rootCommand.AddArgument(windowIdentifierArgument);
            rootCommand.AddArgument(regexPatternArgument);
            rootCommand.AddOption(listWindowsOption);
            rootCommand.AddOption(debugOption);

            if (args.Length == 0 && !args.Contains("--list-windows"))
            {
                return await rootCommand.InvokeAsync("--help");
            }

            rootCommand.SetHandler(async (windowIdentifier, regexPattern, listWindows, debugEnabled) =>
            {
                TextAutomationService.DebugMode = debugEnabled;

                var automationService = new TextAutomationService();

                if (listWindows)
                {
                    if (TextAutomationService.DebugMode) Console.WriteLine("[GrabadorConsole] --list-windows flag active. Listing windows...");
                    automationService.ListAvailableWindows();
                }
                else
                {
                    if (string.IsNullOrEmpty(windowIdentifier) || string.IsNullOrEmpty(regexPattern))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Error: window_identifier and regex_pattern are required when --list-windows is not used.");
                        Console.ResetColor();
                        return;
                    }

                    if (TextAutomationService.DebugMode) Console.WriteLine($"[GrabadorConsole] Processing window: {windowIdentifier} with regex: {regexPattern}");

                    var (matchedText, error) = await automationService.ExtractAndMatchAsync(windowIdentifier, regexPattern);

                    if (error != null)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Error: {error}");
                        Console.ResetColor();
                    }
                    else if (matchedText != null)
                    {
                        if (TextAutomationService.DebugMode)
                        {
                             Console.ForegroundColor = ConsoleColor.Green;
                             Console.WriteLine("Match found by service (see service logs for details):");
                             Console.ResetColor();
                        }
                        Console.WriteLine(matchedText);
                    }
                    else
                    {
                        if (TextAutomationService.DebugMode) Console.WriteLine("[GrabadorConsole] Service reported no match and no error.");
                        else Console.WriteLine("No match found.");
                    }
                }
            }, windowIdentifierArgument, regexPatternArgument, listWindowsOption, debugOption);

            try
            {
                return await rootCommand.InvokeAsync(args);
            }
            finally
            {
                // Cleanup, if any, not related to hotkeys.
            }
        }

        // Old ListWindows() method removed.
        // Old ProcessWindowAsync() method removed.
        // Old FindWindow() method removed.
        // Old GetTextFromElement() method removed.
        // Old ParseHotkey() method removed.
    }
}
