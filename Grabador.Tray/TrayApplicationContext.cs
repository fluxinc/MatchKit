using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Grabador.Core;

namespace Grabador.Tray
{
    public class TrayApplicationContext : ApplicationContext
    {
        private NotifyIcon _trayIcon;
        private readonly AutomationOrchestrator _orchestrator;
        private readonly AutomationOrchestrator.AutomationConfig _config;
        private readonly Keys _hotkey;
        private Form _hiddenForm;

        public TrayApplicationContext(
            string windowIdentifier,
            string regexPattern,
            string urlTemplate,
            string jsonKey,
            Keys hotkey,
            bool debugMode)
        {
            _orchestrator = new AutomationOrchestrator(debugMode);
            _config = new AutomationOrchestrator.AutomationConfig
            {
                WindowIdentifier = windowIdentifier,
                RegexPattern = regexPattern,
                UrlTemplate = urlTemplate,
                JsonKey = jsonKey,
                DebugMode = debugMode
            };
            _hotkey = hotkey;

            InitializeTrayIcon();
            RegisterHotkey();
        }

        private void InitializeTrayIcon()
        {
            _trayIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application, // You can replace with a custom icon
                Visible = true,
                Text = "Grabador.Tray - Press " + GetHotkeyDisplay()
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
            // Create a hidden form to receive hotkey messages
            _hiddenForm = new Form
            {
                WindowState = FormWindowState.Minimized,
                ShowInTaskbar = false,
                Visible = false
            };

            _hiddenForm.Load += (s, e) =>
            {
                Form1.RegisterHotKey(_hiddenForm, _hotkey);
            };

            _hiddenForm.FormClosing += (s, e) =>
            {
                Form1.UnregisterHotKey(_hiddenForm);
            };

            // Override WndProc to handle hotkey
            _hiddenForm.Shown += (s, e) => _hiddenForm.Hide();

            // Create custom form class to handle WndProc
            var hotkeyForm = new HotkeyForm(async () => await ExecuteAutomation());
            hotkeyForm.RegisterKey(_hotkey);
            hotkeyForm.Show();
            hotkeyForm.Hide();
            _hiddenForm = hotkeyForm;
        }

        private string GetHotkeyDisplay()
        {
            var display = "";
            if ((_hotkey & Keys.Control) == Keys.Control) display += "Ctrl+";
            if ((_hotkey & Keys.Alt) == Keys.Alt) display += "Alt+";
            if ((_hotkey & Keys.Shift) == Keys.Shift) display += "Shift+";

            var key = _hotkey & ~Keys.Control & ~Keys.Alt & ~Keys.Shift;
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
            try
            {
                var result = await _orchestrator.ExecuteAsync(_config);

                if (!result.Success)
                {
                    ShowBalloonTip("Unable to complete operation",
                        GetUserFriendlyError(result.Error), ToolTipIcon.Error);
                    return;
                }

                // Use the hidden form to ensure we're on the UI/STA thread
                _hiddenForm.Invoke(new Action(() =>
                {
                    // Copy to clipboard
                    Clipboard.SetText(result.Value);

                    // Small delay to ensure clipboard is ready
                    System.Threading.Thread.Sleep(100);

                    // Paste using SendKeys
                    SendKeys.SendWait("^v");
                }));

                ShowBalloonTip("Success", "Information pasted successfully", ToolTipIcon.Info);
            }
            catch (Exception ex)
            {
                if (_config.DebugMode)
                {
                    ShowBalloonTip("Error", ex.Message, ToolTipIcon.Error);
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
            }
            base.Dispose(disposing);
        }
    }

    // Custom form to handle hotkey messages
    internal class HotkeyForm : Form
    {
        private readonly Func<Task> _hotkeyAction;

        public HotkeyForm(Func<Task> hotkeyAction)
        {
            _hotkeyAction = hotkeyAction;
            WindowState = FormWindowState.Minimized;
            ShowInTaskbar = false;
        }

        public void RegisterKey(Keys key)
        {
            Form1.RegisterHotKey(this, key);
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (m.Msg == Form1.WM_HOTKEY)
            {
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
