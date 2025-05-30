using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.IO;
using Microsoft.Win32.SafeHandles;
using MatchKit.Core;
using System.Linq;
using System.Threading; // For Mutex
using System.Diagnostics; // For Process

namespace MatchKit.Tray
{
    static class Program
    {
        private static Mutex _configMutex = null;
        private const string ConfigMutexName = "Global\\MatchKitTrayConfigMutex";
        // The hotkey-specific mutex will be managed in TrayApplicationContext,
        // but its name might be constructed here or passed to it.
        public const string TrayAppMutexPrefix = "Global\\MatchKitTrayAppMutex_";

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool FreeConsole();

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern SafeFileHandle CreateFileW(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile
        );

        private const int STD_OUTPUT_HANDLE = -11;
        private const int STD_ERROR_HANDLE = -12;
        private const int STD_INPUT_HANDLE = -10;
        private const uint GENERIC_WRITE = 0x40000000;
        private const uint GENERIC_READ = 0x80000000;
        private const uint FILE_SHARE_WRITE = 0x00000002;
        private const uint FILE_SHARE_READ = 0x00000001;
        private const uint OPEN_EXISTING = 0x00000003;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            ElevationHelper.ElevateIfConfigAndNotAdmin(args); // Ensure elevation if in config mode

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var configOption = new Option<bool>(
                aliases: new[] { "--config", "-c" },
                description: "Show the configuration panel to set and save default settings to the registry.");

            RootCommand rootCommand = new RootCommand("MatchKit.Tray - A system tray application for hotkey-triggered text extraction and automation.");
            rootCommand.AddOption(configOption);

            Option<string> windowIdentifierOption = new Option<string>(
                aliases: new[] { "--window", "-w" },
                description: "Process name or window title regex of the target application.");

            Option<string> regexPatternOption = new Option<string>(
                aliases: new[] { "--regex", "-r" },
                description: "Regular expression to search for within the window's controls.");

            Option<string> urlTemplateOption = new Option<string?>(
                aliases: new[] { "--url", "-u" },
                description: "URL template with $1 placeholder for the found text (e.g., http://api/endpoint/$1).");

            Option<string> jsonKeyOption = new Option<string?>(
                aliases: new[] { "--json-key", "-j" },
                description: "The key or dot-separated path of the value to extract from the JSON response.");

            Option<string> hotkeyOption = new Option<string>(
                aliases: new[] { "--hotkey", "-k" },
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

            string[] originalArgs = (string[])args.Clone();

            rootCommand.SetHandler((InvocationContext context) =>
            {
                bool showConfig = context.ParseResult.GetValueForOption(configOption);
                string windowIdentifier = context.ParseResult.GetValueForOption(windowIdentifierOption);
                string regexPattern = context.ParseResult.GetValueForOption(regexPatternOption);
                string urlTemplate = context.ParseResult.GetValueForOption(urlTemplateOption);
                string jsonKey = context.ParseResult.GetValueForOption(jsonKeyOption);
                string hotkeyString = context.ParseResult.GetValueForOption(hotkeyOption);
                bool debugMode = context.ParseResult.GetValueForOption(debugOption);

                if (showConfig)
                {
                    if (!ElevationHelper.IsAdmin())
                    {
                        MessageBox.Show("Administrator privileges are required for configuration. Please re-run with --config as an administrator.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        Environment.Exit(1);
                        return;
                    }

                    bool createdNewConfigMutex;
                    _configMutex = new Mutex(true, ConfigMutexName, out createdNewConfigMutex);
                    if (!createdNewConfigMutex)
                    {
                        MessageBox.Show("Another instance of MatchKit Tray configuration is already running. Please close it and try again.", "Configuration Active", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        Environment.Exit(1);
                        return;
                    }

                    if (debugMode)
                    {
                        AllocateDebugConsole();
                    }
                    Application.Run(new ConfigurationForm());
                    _configMutex.ReleaseMutex();
                    _configMutex.Dispose();

                    // After configuration, ask to kill other instances
                    DialogResult killResult = MessageBox.Show(
                        "Configuration saved. Do you want to close any existing MatchKit.Tray instances to apply changes?",
                        "Restart Instances?",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (killResult == DialogResult.Yes)
                    {
                        KillOtherMatchKitTrayInstances();
                    }

                    Environment.Exit(0);
                    return;
                }

                ConfigData loadedConfig = null;
                bool operationalArgsProvided = false;

                if (originalArgs.Length > 0)
                {
                    operationalArgsProvided = originalArgs.Any(arg =>
                        !arg.Equals("--config", StringComparison.OrdinalIgnoreCase) &&
                        !arg.Equals("-c", StringComparison.OrdinalIgnoreCase) &&
                        !arg.Equals("--debug", StringComparison.OrdinalIgnoreCase) &&
                        !arg.Equals("-d", StringComparison.OrdinalIgnoreCase)
                    );
                    if (!operationalArgsProvided &&
                        (context.ParseResult.GetValueForOption(windowIdentifierOption) != null ||
                         context.ParseResult.GetValueForOption(regexPatternOption) != null))
                    {
                        operationalArgsProvided = true;
                    }
                }

                if (!operationalArgsProvided)
                {
                    if (debugMode) AllocateDebugConsole();
                    if (debugMode) Console.WriteLine("No command-line arguments provided. Attempting to load from registry...");
                    loadedConfig = ConfigurationService.LoadConfiguration();
                    if (loadedConfig != null)
                    {
                        if (debugMode) Console.WriteLine("Configuration loaded from registry.");
                        windowIdentifier = loadedConfig.WindowIdentifier;
                        regexPattern = loadedConfig.RegexPattern;
                        urlTemplate = loadedConfig.UrlTemplate;
                        jsonKey = loadedConfig.JsonKey;
                        hotkeyString = loadedConfig.Hotkey;
                    }
                    else
                    {
                        // Ensure a message is visible even if not in debug mode / no console
                        string errorMessage = "Failed to load configuration from registry and no arguments provided.\nPlease run with --config to set defaults or provide arguments directly.";
                        Console.Error.WriteLine(errorMessage); // Keep for debug/console cases
                        MessageBox.Show(errorMessage, "Configuration Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        if (debugMode) Console.ReadKey(); // Keep for debug behavior if console is allocated
                        Environment.Exit(1);
                        return;
                    }
                }
                else
                {
                    if (debugMode) AllocateDebugConsole();
                    if (debugMode) Console.WriteLine("Command-line arguments provided, overriding registry settings.");
                }

                if (string.IsNullOrEmpty(windowIdentifier) || string.IsNullOrEmpty(regexPattern) || string.IsNullOrEmpty(hotkeyString))
                {
                    Console.Error.WriteLine("Error: Window identifier, regex pattern, and hotkey are required.");
                    Console.Error.WriteLine("Please provide them as command-line arguments or run with --config to set defaults.");
                    if (debugMode) Console.ReadKey();
                    Environment.Exit(1);
                    return;
                }

                try
                {
                    Keys hotkey = HotkeyParser.Parse(hotkeyString);
                    var appContext = new TrayApplicationContext(
                        windowIdentifier,
                        regexPattern,
                        urlTemplate,
                        jsonKey,
                        hotkey,
                        debugMode,
                        hotkeyString); // Pass hotkeyString for mutex name generation

                    Application.Run(appContext);
                }
                catch (ArgumentException ex)
                {
                    Console.Error.WriteLine($"Invalid configuration: {ex.Message}");
                    if (debugMode) Console.ReadKey();
                    Environment.Exit(1);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Failed to start: {ex.Message}");
                    if (debugMode) Console.ReadKey();
                    Environment.Exit(1);
                }
            });

            rootCommand.InvokeAsync(args).GetAwaiter().GetResult();
        }

        private static void KillOtherMatchKitTrayInstances()
        {
            Process currentProcess = Process.GetCurrentProcess();
            Process[] processes = Process.GetProcessesByName(currentProcess.ProcessName);

            foreach (Process process in processes)
            {
                if (process.Id != currentProcess.Id)
                {
                    try
                    {
                        // Check if it's a tray app and not a config window
                        // This is a heuristic. A more robust way would be to check mutex ownership or command line args if possible.
                        if (process.MainWindowHandle == IntPtr.Zero) // Tray apps often don't have a main window handle visible this way
                        {
                            if (debugMode) Console.WriteLine($"Attempting to close process ID: {process.Id}");
                            process.Kill();
                            process.WaitForExit(5000); // Wait up to 5 seconds
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Failed to kill process {process.Id}: {ex.Message}");
                    }
                }
            }
        }

        private static void AllocateDebugConsole()
        {
            if (AllocConsole())
            {
                try
                {
                    var stdOutHandle = CreateFileW("CONOUT$", GENERIC_WRITE, FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
                    if (!stdOutHandle.IsInvalid)
                    {
                        var writer = new StreamWriter(new FileStream(stdOutHandle, FileAccess.Write)) { AutoFlush = true };
                        Console.SetOut(writer);
                    }

                    var stdErrHandle = CreateFileW("CONOUT$", GENERIC_WRITE, FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
                    if (!stdErrHandle.IsInvalid)
                    {
                        var errorWriter = new StreamWriter(new FileStream(stdErrHandle, FileAccess.Write)) { AutoFlush = true };
                        Console.SetError(errorWriter);
                    }

                    var stdInHandle = CreateFileW("CONIN$", GENERIC_READ, FILE_SHARE_READ, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
                    if (!stdInHandle.IsInvalid)
                    {
                        var reader = new StreamReader(new FileStream(stdInHandle, FileAccess.Read));
                        Console.SetIn(reader);
                    }
                    Console.WriteLine("Debug console allocated and I/O redirected.");
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Failed to redirect console I/O: {ex.Message}");
                }
            }
            else
            {
                 Console.Error.WriteLine($"Failed to allocate new console. Error: {Marshal.GetLastWin32Error()} - this may happen if a console is already allocated by a parent process.");
            }
        }
    }
}
