using System;
using System.Windows.Forms;

namespace MatchKit.Tray
{
    /// <summary>
    /// Utility class to parse hotkey strings into Keys enum values
    /// </summary>
    public static class HotkeyParser
    {
        /// <summary>
        /// Parse a hotkey string like "Ctrl+R" or "Alt+F1" into a Keys value
        /// </summary>
        public static Keys Parse(string hotkeyString)
        {
            if (string.IsNullOrWhiteSpace(hotkeyString))
                throw new ArgumentException("Hotkey string cannot be empty");

            Keys result = Keys.None;
            string[] parts = hotkeyString.Split('+');

            foreach (string part in parts)
            {
                string trimmed = part.Trim();

                // Handle modifiers
                if (string.Equals(trimmed, "Ctrl", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(trimmed, "Control", StringComparison.OrdinalIgnoreCase))
                {
                    result |= Keys.Control;
                }
                else if (string.Equals(trimmed, "Alt", StringComparison.OrdinalIgnoreCase))
                {
                    result |= Keys.Alt;
                }
                else if (string.Equals(trimmed, "Shift", StringComparison.OrdinalIgnoreCase))
                {
                    result |= Keys.Shift;
                }
                else if (string.Equals(trimmed, "Win", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(trimmed, "Windows", StringComparison.OrdinalIgnoreCase))
                {
                    result |= Keys.LWin;
                }
                else
                {
                    // Try to parse as a key
                    if (Enum.TryParse<Keys>(trimmed, true, out Keys key))
                    {
                        result |= key;
                    }
                    else
                    {
                        // Special cases
                        if (trimmed.Length == 1 && char.IsLetter(trimmed[0]))
                        {
                            // Single letter
                            if (Enum.TryParse<Keys>(trimmed.ToUpper(), out key))
                            {
                                result |= key;
                            }
                            else
                            {
                                throw new ArgumentException($"Invalid key: {trimmed}");
                            }
                        }
                        else if (trimmed.Length == 1 && char.IsDigit(trimmed[0]))
                        {
                            // Single digit
                            if (Enum.TryParse<Keys>("D" + trimmed, out key))
                            {
                                result |= key;
                            }
                            else
                            {
                                throw new ArgumentException($"Invalid key: {trimmed}");
                            }
                        }
                        else
                        {
                            throw new ArgumentException($"Invalid key: {trimmed}");
                        }
                    }
                }
            }

            // Validate that we have at least one non-modifier key
            var nonModifierKeys = result & ~(Keys.Control | Keys.Alt | Keys.Shift | Keys.LWin);
            if (nonModifierKeys == Keys.None)
            {
                throw new ArgumentException("Hotkey must include at least one non-modifier key");
            }

            return result;
        }
    }
}
