using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Grabador.Core;
using System.Threading;
using System.Text;

namespace Grabador.Tray
{
    public class TrayApplicationContext : ApplicationContext
    {
        private NotifyIcon _trayIcon;
        private readonly AutomationOrchestrator _orchestrator;
        private readonly AutomationOrchestrator.AutomationConfig _config;
        private readonly Keys _hotkeyEnumInternal;
        private Form _hiddenForm;
        private StringBuilder _threadLog;

        private Mutex _hotkeyMutex;
        private string _hotkeyMutexName;
        private bool _debugModeInternal;

        public TrayApplicationContext(
            string windowIdentifier,
            string regexPattern,
            string urlTemplate,
            string jsonKey,
            Keys hotkeyEnum,
            bool debugMode,
            string hotkeyStringLiteral
            )
        {
            _threadLog = new StringBuilder();
            _debugModeInternal = debugMode;
            LogThreadInfo("[TrayApplicationContext] Constructor entered");

            string sanitizedHotkeyString = hotkeyStringLiteral
                .Replace("+", "_Plus_")
                .Replace("^", "Ctrl_")
                .Replace("%", "Alt_")
                .Replace("#", "Win_")
                .Replace("!", "Shift_")
                .Replace(" ", "_Space_")
                .Replace("\\", "_Backslash_")
                .Replace("/", "_Slash_")
                .Replace(":", "_Colon_")
                .Replace("*", "_Asterisk_")
                .Replace("?", "_Question_")
                .Replace("\"", "_Quote_")
                .Replace("<", "_LessThan_")
                .Replace(">", "_GreaterThan_")
                .Replace("|", "_Pipe_")
                .Replace(",", "_Comma_")
                .Replace(".", "_Period_");

            _hotkeyMutexName = Program.TrayAppMutexPrefix + sanitizedHotkeyString;
            bool createdNewMutex;

            try
            {
                _hotkeyMutex = new Mutex(true, _hotkeyMutexName, out createdNewMutex);
            }
            catch (Exception ex)
            {
                LogThreadInfo($"[TrayApplicationContext] CRITICAL: Failed to create or acquire mutex '{_hotkeyMutexName}'. Exception: {ex}");
                MessageBox.Show($"A critical error occurred while trying to initialize the hotkey exclusivity lock for '{hotkeyStringLiteral}'.\n\nError: {ex.Message}", "Mutex Creation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(1);
                return;
            }

            if (!createdNewMutex)
            {
                LogThreadInfo($"[TrayApplicationContext] Mutex '{_hotkeyMutexName}' already exists. Another instance with hotkey '{hotkeyStringLiteral}' is likely running.");
                MessageBox.Show($"An instance of Grabador.Tray with the hotkey '{hotkeyStringLiteral}' is already running.\nOnly one instance per hotkey is allowed.", "Instance Already Running", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Environment.Exit(1);
                return;
            }
            LogThreadInfo($"[TrayApplicationContext] Successfully acquired mutex '{_hotkeyMutexName}' for hotkey '{hotkeyStringLiteral}'.");

            _orchestrator = new AutomationOrchestrator(debugMode);
            _config = new AutomationOrchestrator.AutomationConfig
            {
                WindowIdentifier = windowIdentifier,
                RegexPattern = regexPattern,
                UrlTemplate = urlTemplate,
                JsonKey = jsonKey,
                DebugMode = debugMode
            };
            _hotkeyEnumInternal = hotkeyEnum;

            InitializeTrayIcon(hotkeyStringLiteral);
            RegisterHotkey();
            LogThreadInfo("[TrayApplicationContext] Constructor finished successfully");
        }

        private void InitializeTrayIcon(string hotkeyStringForDisplay)
        {
            _trayIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Visible = true,
                Text = "Grabador.Tray - Press " + hotkeyStringForDisplay
            };

            var contextMenu = new ContextMenuStrip();

            contextMenu.Items.Add("Show Configuration", null, ShowConfiguration);
            contextMenu.Items.Add("Test Automation", null, async (s, e) => await ExecuteAutomation());
            contextMenu.Items.Add("-");
            contextMenu.Items.Add("Exit", null, Exit);

            _trayIcon.ContextMenuStrip = contextMenu;
            _trayIcon.DoubleClick += ShowConfiguration;
        }

        private void RegisterHotkey()
        {
            var hotkeyForm = new HotkeyForm(async () => await ExecuteAutomation(), _hotkeyEnumInternal, LogThreadInfo);
            hotkeyForm.Show();
            hotkeyForm.Hide();
            _hiddenForm = hotkeyForm;
        }

        private string GetHotkeyDisplay()
        {
            var display = "";
            if ((_hotkeyEnumInternal & Keys.Control) == Keys.Control) display += "Ctrl+";
            if ((_hotkeyEnumInternal & Keys.Alt) == Keys.Alt) display += "Alt+";
            if ((_hotkeyEnumInternal & Keys.Shift) == Keys.Shift) display += "Shift+";

            var key = _hotkeyEnumInternal & ~Keys.Control & ~Keys.Alt & ~Keys.Shift;
            display += key.ToString();

            return display;
        }

        private void ShowConfiguration(object sender, EventArgs e)
        {
            var message = $"Grabador.Tray Configuration:\n\n" +
                          $"Window: {_config.WindowIdentifier}\n" +
                          $"Regex: {_config.RegexPattern}\n" +
                          $"URL: {_config.UrlTemplate ?? "(none)"}\n" +
                          $"JSON Key: {_config.JsonKey ?? "(none)"}\n" +
                          $"Hotkey: {GetHotkeyDisplay()}\n" +
                          $"Debug Mode: {(_config.DebugMode ? "Enabled" : "Disabled")}";

            MessageBox.Show(message, "Grabador.Tray Configuration",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private async Task ExecuteAutomation()
        {
            LogThreadInfo("[ExecuteAutomation] Method entered");

            try
            {
                var result = await _orchestrator.ExecuteAsync(_config);

                LogThreadInfo("[ExecuteAutomation] After await _orchestrator.ExecuteAsync");

                if (!result.Success)
                {
                    if (_config.DebugMode)
                    {
                        result.Error += $"\n\nThread Log:\n{_threadLog.ToString()}";
                    }
                    ShowBalloonTip("Unable to complete operation",
                        GetUserFriendlyError(result.Error), ToolTipIcon.Error);
                    return;
                }

                _hiddenForm.Invoke(new Action(() =>
                {
                    LogThreadInfo("[ExecuteAutomation] Inside Invoke block");

                    LogThreadInfo("[ExecuteAutomation] Before Clipboard.SetText");
                    Clipboard.SetText(result.Value);
                    LogThreadInfo("[ExecuteAutomation] After Clipboard.SetText");

                    LogThreadInfo("[ExecuteAutomation] Before Thread.Sleep(100)");
                    System.Threading.Thread.Sleep(100);
                    LogThreadInfo("[ExecuteAutomation] After Thread.Sleep(100)");

                    LogThreadInfo("[ExecuteAutomation] Before SendKeys.SendWait");
                    SendKeys.SendWait("^v");
                    LogThreadInfo("[ExecuteAutomation] After SendKeys.SendWait");
                }));

                ShowBalloonTip("Success", "Information pasted successfully", ToolTipIcon.Info);
            }
            catch (Exception ex)
            {
                if (_config.DebugMode)
                {
                    string errorWithLog = $"Error: {ex.Message}\n\nThread Log:\n{_threadLog.ToString()}";
                    MessageBox.Show(errorWithLog, "Grabador.Tray Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    ShowBalloonTip("Error", "An unexpected error occurred", ToolTipIcon.Error);
                }
            }
        }

        private string GetUserFriendlyError(string technicalError)
        {
            if (technicalError.Contains("Could not find window"))
                return "Window not found - please check the application is running";

            if (technicalError.Contains("No match found"))
                return "Unable to find the requested information";

            if (technicalError.Contains("Error calling URL"))
                return "Unable to retrieve data from server";

            if (technicalError.Contains("Error parsing JSON"))
                return "Server returned unexpected data format";

            if (technicalError.Contains("JSON key/path") && technicalError.Contains("not found"))
                return "Information not found in server response";

            return "Unable to complete the operation";
        }

        private void ShowBalloonTip(string title, string text, ToolTipIcon icon)
        {
            _trayIcon.ShowBalloonTip(3000, title, text, icon);
        }

        private void Exit(object sender, EventArgs e)
        {
            _trayIcon.Visible = false;
            _hiddenForm?.Close();
            Application.Exit();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _trayIcon?.Dispose();
                _hiddenForm?.Dispose();

                try
                {
                    _hotkeyMutex?.ReleaseMutex();
                }
                catch (ApplicationException ex)
                {
                    LogThreadInfo($"[TrayApplicationContext] Warning: Exception when trying to release mutex '{_hotkeyMutexName}': {ex.Message}");
                }
                _hotkeyMutex?.Dispose();
                LogThreadInfo($"[TrayApplicationContext] Mutex '{_hotkeyMutexName}' released and disposed.");
            }
            base.Dispose(disposing);
        }

        private void LogThreadInfo(string message)
        {
            string logEntry = $"{message} - Thread: {Thread.CurrentThread.ManagedThreadId}, Apartment: {Thread.CurrentThread.GetApartmentState()}";
            _threadLog.AppendLine(logEntry);
            Console.WriteLine(logEntry);
        }
    }

    internal class HotkeyForm : Form
    {
        private readonly Func<Task> _hotkeyAction;
        private Keys _keyToRegister;
        private Action<string> _logThreadInfoAction;

        public HotkeyForm(Func<Task> hotkeyAction, Keys keyToRegister, Action<string> logThreadInfoAction)
        {
            _hotkeyAction = hotkeyAction;
            _keyToRegister = keyToRegister;
            _logThreadInfoAction = logThreadInfoAction;
            WindowState = FormWindowState.Minimized;
            ShowInTaskbar = false;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            _logThreadInfoAction("[HotkeyForm] OnLoad entered");
            Form1.RegisterHotKey(this, _keyToRegister);
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (m.Msg == Form1.WM_HOTKEY)
            {
                _logThreadInfoAction("[HotkeyForm] WndProc entered");
                Task.Run(_hotkeyAction);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            Form1.UnregisterHotKey(this);
            base.OnFormClosing(e);
        }
    }
}
