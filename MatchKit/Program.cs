using System;
using System.Linq;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;
using System.Threading;
using MatchKit.Core;

namespace MatchKit
{
    internal class Program
    {
        private static Mutex _configMutex = null;
        private const string ConfigMutexName = "Global\\MatchKitConsoleConfigMutex";

        public static async Task<int> Main(string[] args)
        {
            ElevationHelper.ElevateIfConfigAndNotAdmin(args);

            Option<bool> configOption = new Option<bool>(
                aliases: new[] { "--config", "-c" },
                description: "Interactively set and save default settings to the registry.");

            Option<bool> saveOption = new Option<bool>(
                aliases: new[] { "--save", "-s" },
                description: "Save the provided operational arguments (window-identifier, regex-pattern, etc.) as default settings to the registry. Requires administrator privileges.");

            RootCommand rootCommand = new RootCommand("A tool to extract text from a window and apply a regex pattern.");
            rootCommand.AddOption(configOption);
            rootCommand.AddOption(saveOption);

            Option<string> windowIdentifierOption = new Option<string>(
                aliases: new[] { "--window-identifier", "-w" },
                description: "Process name (exact match) or main window title (regex match) of the target application.");

            Option<string> regexPatternOption = new Option<string>(
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

            Option<string> hotkeyOption = new Option<string?>(
                aliases: new[] { "--hotkey", "-k" },
                description: "Hotkey to be saved for the tray application (e.g., Ctrl+D). Not used by the console app during normal operation.");

            rootCommand.AddOption(windowIdentifierOption);
            rootCommand.AddOption(regexPatternOption);
            rootCommand.AddOption(urlTemplateOption);
            rootCommand.AddOption(listWindowsOption);
            rootCommand.AddOption(debugOption);
            rootCommand.AddOption(jsonKeyOption);
            rootCommand.AddOption(hotkeyOption);

            string[] originalArgs = (string[])args.Clone();

            rootCommand.SetHandler(async (InvocationContext context) =>
            {
                bool showConfig = context.ParseResult.GetValueForOption(configOption);
                bool saveSettings = context.ParseResult.GetValueForOption(saveOption);
                string windowIdentifier = context.ParseResult.GetValueForOption(windowIdentifierOption);
                string regexPattern = context.ParseResult.GetValueForOption(regexPatternOption);
                string urlTemplate = context.ParseResult.GetValueForOption(urlTemplateOption);
                bool listWindows = context.ParseResult.GetValueForOption(listWindowsOption);
                bool debugEnabled = context.ParseResult.GetValueForOption(debugOption);
                string jsonKey = context.ParseResult.GetValueForOption(jsonKeyOption);
                string hotkeyCliValue = context.ParseResult.GetValueForOption(hotkeyOption);

                if (saveSettings && showConfig)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    await Console.Error.WriteLineAsync("Error: --save and --config options are mutually exclusive.");
                    Console.ResetColor();
                    context.ExitCode = 1;
                    return;
                }

                if (saveSettings)
                {
                    if (!ElevationHelper.IsAdmin())
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        await Console.Error.WriteLineAsync("Error: Administrator privileges are required to save configuration with --save.");
                        await Console.Error.WriteLineAsync("Please re-run with --save as an administrator.");
                        Console.ResetColor();
                        context.ExitCode = 1;
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(windowIdentifier) || string.IsNullOrWhiteSpace(regexPattern))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Error.WriteLine("Error: --window-identifier (-w) and --regex-pattern (-r) are required when using --save.");
                        Console.ResetColor();
                        context.ExitCode = 1;
                        return;
                    }

                    ConfigData currentConfigForHotkey = ConfigurationService.LoadConfiguration();
                    string hotkeyToSave;
                    if (!string.IsNullOrWhiteSpace(hotkeyCliValue))
                    {
                        hotkeyToSave = hotkeyCliValue;
                    }
                    else
                    {
                        hotkeyToSave = currentConfigForHotkey?.Hotkey ?? "Ctrl+D";
                    }

                    ConfigData configToSave = new ConfigData
                    {
                        WindowIdentifier = windowIdentifier,
                        RegexPattern = regexPattern,
                        UrlTemplate = urlTemplate,
                        JsonKey = jsonKey,
                        Hotkey = hotkeyToSave
                    };

                    try
                    {
                        ConfigurationService.SaveConfiguration(configToSave);
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Configuration saved successfully from command-line arguments to the registry.");
                        Console.ResetColor();
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Error.WriteLine($"Error saving configuration: {ex.Message}");
                        Console.ResetColor();
                        context.ExitCode = 1;
                    }
                    return;
                }

                if (showConfig)
                {
                    if (!ElevationHelper.IsAdmin())
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        await Console.Error.WriteLineAsync("Error: Administrator privileges are required to save configuration.");
                        await Console.Error.WriteLineAsync("Please re-run with --config as an administrator.");
                        Console.ResetColor();
                        context.ExitCode = 1;
                        return;
                    }

                    bool createdNew;
                    _configMutex = new Mutex(true, ConfigMutexName, out createdNew);
                    if (!createdNew)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("Another instance of MatchKit console configuration is already running.");
                        Console.WriteLine("Please close the other instance and try again.");
                        Console.ResetColor();
                        context.ExitCode = 1;
                        return;
                    }

                    Console.WriteLine("Interactive Configuration Mode (Administrator):");
                    ConfigData newConfig = new ConfigData();

                    Console.Write("Enter Window Identifier (Process name or window title regex): ");
                    newConfig.WindowIdentifier = Console.ReadLine();

                    Console.Write("Enter Regex Pattern: ");
                    newConfig.RegexPattern = Console.ReadLine();

                    Console.Write("Enter URL Template (optional, e.g., http://api/$1): ");
                    newConfig.UrlTemplate = Console.ReadLine();

                    Console.Write("Enter JSON Key for response (optional, e.g., data.value): ");
                    newConfig.JsonKey = Console.ReadLine();

                    string hotkeyPromptDefault = !string.IsNullOrWhiteSpace(hotkeyCliValue) ? hotkeyCliValue : "Ctrl+D";
                    Console.Write($"Enter Hotkey for Tray App (e.g., Ctrl+D, optional, default: {hotkeyPromptDefault}): ");
                    string hotkeyInput = Console.ReadLine();
                    newConfig.Hotkey = string.IsNullOrWhiteSpace(hotkeyInput) ? hotkeyPromptDefault : hotkeyInput;

                    if (string.IsNullOrWhiteSpace(newConfig.WindowIdentifier) || string.IsNullOrWhiteSpace(newConfig.RegexPattern))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        await Console.Error.WriteLineAsync("Error: Window Identifier and Regex Pattern are required.");
                        Console.ResetColor();
                        context.ExitCode = 1;
                        return;
                    }

                    try
                    {
                        ConfigurationService.SaveConfiguration(newConfig);
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Configuration saved successfully to the registry.");
                        Console.ResetColor();
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        await Console.Error.WriteLineAsync($"Error saving configuration: {ex.Message}");
                        Console.ResetColor();
                        context.ExitCode = 1;
                    }
                    return;
                }

                bool operationalArgsProvided = false;
                if (originalArgs.Length > 0)
                {
                    operationalArgsProvided = originalArgs.Any(arg =>
                        !arg.Equals("--config", StringComparison.OrdinalIgnoreCase) &&
                        !arg.Equals("-c", StringComparison.OrdinalIgnoreCase) &&
                        !arg.Equals("--debug", StringComparison.OrdinalIgnoreCase) &&
                        !arg.Equals("-d", StringComparison.OrdinalIgnoreCase) &&
                        !arg.Equals("--list-windows", StringComparison.OrdinalIgnoreCase) &&
                        !arg.Equals("-l", StringComparison.OrdinalIgnoreCase)
                    );
                     if (!operationalArgsProvided &&
                        (context.ParseResult.GetValueForOption(windowIdentifierOption) != null ||
                         context.ParseResult.GetValueForOption(regexPatternOption) != null ||
                         listWindows ))
                    {
                        operationalArgsProvided = true;
                    }
                }

                if (!operationalArgsProvided && !listWindows)
                {
                    if (debugEnabled) Console.WriteLine("[MatchKit] No command-line arguments. Attempting to load from registry...");
                    ConfigData loadedConfig = ConfigurationService.LoadConfiguration();
                    if (loadedConfig != null)
                    {
                        if (debugEnabled) Console.WriteLine("[MatchKit] Configuration loaded from registry.");
                        windowIdentifier = loadedConfig.WindowIdentifier;
                        regexPattern = loadedConfig.RegexPattern;
                        urlTemplate = loadedConfig.UrlTemplate;
                        jsonKey = loadedConfig.JsonKey;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        await Console.Error.WriteLineAsync("Error: No configuration found in registry and no arguments provided.");
                        await Console.Error.WriteLineAsync("Please run with --config to set defaults, or provide command-line arguments, or use --list-windows.");
                        Console.ResetColor();
                        await rootCommand.InvokeAsync("--help");
                        context.ExitCode = 1;
                        return;
                    }
                }
                else if (operationalArgsProvided)
                {
                    if (debugEnabled) Console.WriteLine("[MatchKit] Command-line arguments provided, overriding registry settings.");
                }

                AutomationOrchestrator orchestrator = new AutomationOrchestrator(debugEnabled);

                if (listWindows)
                {
                    if (debugEnabled) Console.WriteLine("[MatchKit] --list-windows / -l flag active. Listing windows...");
                    orchestrator.ListAvailableWindows();
                }
                else
                {
                    if (string.IsNullOrEmpty(windowIdentifier) || string.IsNullOrEmpty(regexPattern))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        await Console.Error.WriteLineAsync("Error: --window-identifier (-w) and --regex-pattern (-r) are required when --list-windows (-l) is not used and no registry config is found.");
                        Console.ResetColor();
                        await rootCommand.InvokeAsync("--help");
                        context.ExitCode = 1;
                        return;
                    }

                    AutomationOrchestrator.AutomationConfig autoConfig = new AutomationOrchestrator.AutomationConfig
                    {
                        WindowIdentifier = windowIdentifier,
                        RegexPattern = regexPattern,
                        UrlTemplate = urlTemplate,
                        JsonKey = jsonKey,
                        DebugMode = debugEnabled
                    };

                    AutomationOrchestrator.AutomationResult result = await orchestrator.ExecuteAsync(autoConfig);

                    if (!result.Success)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        await Console.Error.WriteLineAsync($"Error: {result.Error}");
                        Console.ResetColor();
                        context.ExitCode = 1;
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
            });

            try
            {
                return await rootCommand.InvokeAsync(args);
            }
            finally
            {
                _configMutex?.ReleaseMutex();
                _configMutex?.Dispose();
            }
        }
    }
}
