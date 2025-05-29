using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
// System.Windows.Forms is not used here to keep it console-friendly.
// If GUI dialogs are strictly needed, it can be added, but Console.Error is preferred.

namespace Grabador.Core
{
    public static class ElevationHelper
    {
        public static bool IsAdmin()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        /// <summary>
        /// Checks if the application is in configuration mode and not running as admin.
        /// If so, attempts to elevate and exits the current process.
        /// This should be called at the very beginning of the Main method.
        /// </summary>
        /// <param name="args">The command-line arguments.</param>
        public static void ElevateIfConfigAndNotAdmin(string[] args)
        {
            bool isConfigMode = args.Any(a =>
                a.Equals("--config", StringComparison.OrdinalIgnoreCase) || a.Equals("-c", StringComparison.OrdinalIgnoreCase) ||
                a.Equals("--save", StringComparison.OrdinalIgnoreCase) || a.Equals("-s", StringComparison.OrdinalIgnoreCase)
            );

            if (isConfigMode && !IsAdmin())
            {
                try
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        // Use Location, which gives the path to the DLL/EXE.
                        FileName = Assembly.GetEntryAssembly().Location,
                        UseShellExecute = true,
                        Verb = "runas",
                        Arguments = string.Join(" ", args)
                    };
                    Process.Start(startInfo);
                    Environment.Exit(0); // Exit current non-elevated process
                }
                catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223) // ERROR_CANCELLED
                {
                    Console.Error.WriteLine("Administrator privileges are required for configuration. Elevation was cancelled by the user.");
                    Environment.Exit(1); // Exit if elevation was cancelled
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Unable to restart with administrator rights for configuration: {ex.Message}");
                    Environment.Exit(1); // Exit if elevation failed
                }
            }
        }
    }
}
