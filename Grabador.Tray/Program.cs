using System;
using System.CommandLine;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Grabador.Tray
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static async Task<int> Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            RootCommand rootCommand = new RootCommand("Grabador.Tray - A system tray application for hotkey-triggered text extraction and automation.");

            Option<string> windowIdentifierOption = new Option<string>(
                aliases: new[] { "--window", "-w" },
                description: "Process name or window title regex of the target application.")
            {
                IsRequired = true
            };

            Option<string> regexPatternOption = new Option<string>(
                aliases: new[] { "--regex", "-r" },
                description: "Regular expression to search for within the window's controls.")
            {
                IsRequired = true
            };

            Option<string> urlTemplateOption = new Option<string?>(
                aliases: new[] { "--url", "-u" },
                description: "URL template with $1 placeholder for the found text (e.g., http://api/endpoint/$1).");

            Option<string> jsonKeyOption = new Option<string?>(
                aliases: new[] { "--json-key", "-j" },
                description: "The key or dot-separated path of the value to extract from the JSON response.");

            Option<string> hotkeyOption = new Option<string>(
                aliases: new[] { "--hotkey", "-h" },
                description: "Hotkey combination (e.g., 'Ctrl+R', 'Alt+F1').",
                getDefaultValue: () => "Ctrl+D");

            Option<bool> debugOption = new Option<bool>(
                aliases: new[] { "--debug", "-d" },
                description: "Enable detailed debug logging.",
                getDefaultValue: () => false);

            rootCommand.AddOption(windowIdentifierOption);
            rootCommand.AddOption(regexPatternOption);
            rootCommand.AddOption(urlTemplateOption);
            rootCommand.AddOption(jsonKeyOption);
            rootCommand.AddOption(hotkeyOption);
            rootCommand.AddOption(debugOption);

            int returnCode = 0;

            rootCommand.SetHandler((windowIdentifier, regexPattern, urlTemplate, jsonKey, hotkeyString, debugMode) =>
            {
                try
                {
                    Keys hotkey = HotkeyParser.Parse(hotkeyString);

                    var context = new TrayApplicationContext(
                        windowIdentifier,
                        regexPattern,
                        urlTemplate,
                        jsonKey,
                        hotkey,
                        debugMode);

                    Application.Run(context);
                }
                catch (ArgumentException ex)
                {
                    MessageBox.Show($"Invalid configuration: {ex.Message}", "Grabador.Tray Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    returnCode = 1;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to start: {ex.Message}", "Grabador.Tray Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    returnCode = 1;
                }
            }, windowIdentifierOption, regexPatternOption, urlTemplateOption, jsonKeyOption, hotkeyOption, debugOption);

            await rootCommand.InvokeAsync(args);
            return returnCode;
        }
    }
}
