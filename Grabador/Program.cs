using System;
using System.Windows.Forms;
// Remove System.CommandLine and System.Runtime.InteropServices if no longer needed by Program.cs directly
// Keep System.Threading.Tasks if HotkeyForm or its methods become async again, but Main itself won't be.

namespace Grabador
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main() // Simplified Main method
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Define parameters here. These would ideally come from a config file or a future UI.
            // For now, using placeholder/example values.
            string defaultWindowIdentifier = ".*Notepad"; // Example: Any window with "Notepad" in its title
            string defaultRegexPattern = "^.*$";         // Example: Match an entire line
            string defaultHotkeyString = "^D";          // Example: Ctrl+D

            // The application will run entirely as a hidden form listening for this hotkey.
            // The HotkeyForm will handle its own lifecycle for hotkey registration and unregistration.
            Application.Run(new HotkeyForm(defaultWindowIdentifier, defaultRegexPattern, defaultHotkeyString));
        }
    }
}
