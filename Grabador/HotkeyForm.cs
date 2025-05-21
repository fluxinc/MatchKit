using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using Grabador.Core; // Import the shared library

namespace Grabador
{
    public class HotkeyForm : Form
    {
        // Hotkey P/Invoke
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int HOTKEY_ID_MAIN = 1; // Unique ID for our main hotkey
        private const int WM_HOTKEY = 0x0312;

        // Modifiers for hotkey parsing (can be a shared utility later)
        private const uint MOD_NONE = 0x0000;
        private const uint MOD_ALT = 0x0001;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        // private const uint MOD_WIN = 0x0008; // Not typically used by apps

        // Win32 Error Codes for RegisterHotKey
        private const int ERROR_HOTKEY_ALREADY_REGISTERED = 1409;

        private string _targetWindowIdentifier;
        private string _regexPattern;
        private string _hotkeyString;

        private uint _parsedModifiers;
        private uint _parsedVk;

        private TextAutomationService _automationService; // Instance of the shared service

        public HotkeyForm(string windowIdentifier, string regexPattern, string hotkeyString)
        {
            _targetWindowIdentifier = windowIdentifier;
            _regexPattern = regexPattern;
            _hotkeyString = hotkeyString;
            _automationService = new TextAutomationService(); // Initialize the service

            // Prepare the form to be hidden
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            this.Visible = false;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            this.Visible = false; // Ensure it's hidden before trying to register hotkey

            (_parsedModifiers, _parsedVk) = ParseHotkeyInternal(_hotkeyString);

            if (_parsedVk == 0) // VK_NONE or parse error
            {
                MessageBox.Show($"Invalid hotkey string: {_hotkeyString}", "Hotkey Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit(); // or this.Close();
                return;
            }

            if (!RegisterHotKey(this.Handle, HOTKEY_ID_MAIN, _parsedModifiers, _parsedVk))
            {
                int errorCode = Marshal.GetLastWin32Error();
                string errorMessage = $"Failed to register hotkey '{_hotkeyString}'. Error code: {errorCode}.";
                if (errorCode == ERROR_HOTKEY_ALREADY_REGISTERED)
                {
                    errorMessage += " This hotkey is already in use.";
                }
                MessageBox.Show(errorMessage, "Hotkey Registration Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit(); // or this.Close();
                return;
            }
            Console.WriteLine($"[HotkeyForm] Hotkey '{_hotkeyString}' registered successfully."); // Debug output
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (m.Msg == WM_HOTKEY)
            {
                if (m.WParam.ToInt32() == HOTKEY_ID_MAIN)
                {
                    Console.WriteLine("[HotkeyForm] Hotkey pressed!"); // Debug output
                    PerformTextRetrieval();
                }
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (this.Handle != IntPtr.Zero && _parsedVk != 0) // Ensure hotkey was registered before trying to unregister
            {
                if (!UnregisterHotKey(this.Handle, HOTKEY_ID_MAIN))
                {
                    Console.WriteLine("[HotkeyForm] Failed to unregister hotkey. Error: " + Marshal.GetLastWin32Error());
                }
                else
                {
                    Console.WriteLine("[HotkeyForm] Hotkey unregistered successfully.");
                }
            }
            base.OnFormClosing(e);
        }

        private async void PerformTextRetrieval()
        {
            Console.WriteLine($"[HotkeyForm] Attempting to process window '{_targetWindowIdentifier}' with pattern '{_regexPattern}'.");
            var (matchedText, error) = await _automationService.ExtractAndMatchAsync(_targetWindowIdentifier, _regexPattern);

            if (error != null)
            {
                Console.WriteLine("[HotkeyForm] Error: " + error);
                // Decide on appropriate user feedback, e.g., a sound or a tray notification if implemented
                if (error.Contains("not found")) System.Media.SystemSounds.Hand.Play();
                else if (error.Contains("No text")) System.Media.SystemSounds.Exclamation.Play();
                else if (error.Contains("Invalid regex")) System.Media.SystemSounds.Hand.Play();
                else System.Media.SystemSounds.Question.Play();
                return;
            }

            if (matchedText != null)
            {
                Console.WriteLine("[HotkeyForm] Match found: " + matchedText);
                try
                {
                    Clipboard.SetText(matchedText);
                    System.Media.SystemSounds.Asterisk.Play(); // Audible feedback for success
                }
                catch (Exception ex)
                {
                     Console.WriteLine("[HotkeyForm] Error setting clipboard text: " + ex.Message);
                     System.Media.SystemSounds.Hand.Play();
                }
            }
            // If matchedText is null and error is null, it implies no match was found (handled by service, but good to be aware)
        }

        // --- Hotkey Parsing Logic (Remains in HotkeyForm as it uses System.Windows.Forms.Keys) ---
        private static (uint modifiers, uint vk) ParseHotkeyInternal(string hotkeyString)
        {
            if (string.IsNullOrWhiteSpace(hotkeyString))
                return (MOD_NONE, (uint)Keys.None);

            uint modifiers = MOD_NONE;
            Keys key = Keys.None;
            string[] parts = hotkeyString.ToUpper().Split(new[] { '+', '-', ' ' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string part in parts.Take(parts.Length -1))
            {
                switch (part)
                {
                    case "CTRL": case "CONTROL": case "^": modifiers |= MOD_CONTROL; break;
                    case "ALT": case "!": modifiers |= MOD_ALT; break;
                    case "SHIFT": case "%": modifiers |= MOD_SHIFT; break;
                }
            }

            string keyPart = parts.LastOrDefault();
            if (string.IsNullOrEmpty(keyPart))
                return (modifiers, (uint)key);

            if (Enum.TryParse<Keys>(keyPart, true, out Keys parsedKey))
            {
                key = parsedKey;
            }
            else if (keyPart.Length == 1 && char.IsLetterOrDigit(keyPart[0]))
            { // For single characters not directly in Keys enum like '5' vs 'D5'
                try { key = (Keys)Enum.Parse(typeof(Keys), keyPart.ToUpper()); }
                catch { /* ignored */ }
            }
            return (modifiers, (uint)key);
        }
    }
}
