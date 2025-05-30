using Microsoft.Win32;
using System;
using System.Security.Permissions;

namespace MatchKit.Core
{
    public class ConfigData
    {
        public string WindowIdentifier { get; set; }
        public string RegexPattern { get; set; }
        public string UrlTemplate { get; set; }
        public string JsonKey { get; set; }
        public string Hotkey { get; set; } // Stored as string, parsed by client
    }

    public static class ConfigurationService
    {
        private const string RegistryPath = "SOFTWARE\\MatchKit"; // For HKEY_LOCAL_MACHINE

        public static void SaveConfiguration(ConfigData data)
        {
            try
            {
                // Explicitly use the 32-bit view of HKLM to ensure Wow6432Node is used on 64-bit systems
                using (RegistryKey hklmBase = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
                using (RegistryKey key = hklmBase.CreateSubKey(RegistryPath))
                {
                    if (key != null)
                    {
                        key.SetValue("WindowIdentifier", data.WindowIdentifier ?? "");
                        key.SetValue("RegexPattern", data.RegexPattern ?? "");
                        key.SetValue("UrlTemplate", data.UrlTemplate ?? "");
                        key.SetValue("JsonKey", data.JsonKey ?? "");
                        key.SetValue("Hotkey", data.Hotkey ?? "Ctrl+D");
                    }
                    else
                    {
                        throw new Exception("Failed to create or open registry key under HKLM (32-bit view). Ensure the application has administrator privileges if using HKEY_LOCAL_MACHINE.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error saving configuration: {ex.Message}");
                throw; // Re-throw to allow calling code to handle it
            }
        }

        public static ConfigData LoadConfiguration()
        {
            try
            {
                // Explicitly use the 32-bit view of HKLM to ensure Wow6432Node is used on 64-bit systems
                using (RegistryKey hklmBase = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
                using (RegistryKey key = hklmBase.OpenSubKey(RegistryPath, writable: false)) // Open for read-only
                {
                    if (key != null)
                    {
                        return new ConfigData
                        {
                            WindowIdentifier = key.GetValue("WindowIdentifier") as string,
                            RegexPattern = key.GetValue("RegexPattern") as string,
                            UrlTemplate = key.GetValue("UrlTemplate") as string,
                            JsonKey = key.GetValue("JsonKey") as string,
                            Hotkey = key.GetValue("Hotkey") as string ?? "Ctrl+D"
                        };
                    }
                }
            }
            catch (System.Security.SecurityException secEx)
            {
                Console.Error.WriteLine($"Security error loading configuration from HKLM (32-bit view): {secEx.Message}. This usually indicates a permissions issue.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error loading configuration from HKLM (32-bit view): {ex.Message}");
            }
            return null;
        }
    }
}
