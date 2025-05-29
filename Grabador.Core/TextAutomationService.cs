using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Automation;
using System.Net.Http; // Added for HttpClient
// Note: System.Windows.Forms should not be referenced here for core logic if it's to be truly shareable outside WinForms contexts.

namespace Grabador.Core
{
    public class TextAutomationService
    {
        public static bool DebugMode { get; set; } = false;

        public AutomationElement FindWindow(string identifier)
        {
            // This is the migrated FindWindowInternal logic
            Console.WriteLine($"[CoreSvc] Attempting to find window with identifier: `{identifier}`"); // Conditional logging

            AutomationElementCollection windows = AutomationElement.RootElement.FindAll(TreeScope.Children, Condition.TrueCondition);
            foreach (AutomationElement windowElement in windows)
            {
                try
                {
                    if (windowElement != null && !string.IsNullOrEmpty(windowElement.Current.Name) && !windowElement.Current.IsOffscreen)
                    {
                        if (Regex.IsMatch(windowElement.Current.Name, identifier, RegexOptions.IgnoreCase))
                        {
                            Console.WriteLine($"[CoreSvc] Found window by title regex: {windowElement.Current.Name}");
                            return windowElement;
                        }
                    }
                }
                catch (ElementNotAvailableException)
                {
                    // Ignore windows that become unavailable during enumeration
                    continue;
                }
            }

            // If not found by title regex, try finding by process name (exact match)
            try
            {
                // Remove .exe if present, as GetProcessesByName expects the name without extension
                string processNameForSearch = identifier;
                if (identifier.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    processNameForSearch = identifier.Substring(0, identifier.Length - 4);
                }

                Process[] processes = Process.GetProcessesByName(processNameForSearch);
                if (processes.Length > 0)
                {
                    foreach (Process p in processes)
                    {
                        if (p.MainWindowHandle != IntPtr.Zero)
                        {
                            AutomationElement window = AutomationElement.FromHandle(p.MainWindowHandle);
                            // Additional check to ensure the window is valid and has a name (sometimes FromHandle can return unusable elements)
                            if (window != null && !string.IsNullOrEmpty(window.Current.Name))
                            {
                                Console.WriteLine($"[CoreSvc] Found window by process name: {window.Current.Name} (PID: {p.Id})");
                                return window;
                            }
                        }
                    }
                }
            }
            catch (ArgumentException ex) // Catches invalid process names for GetProcessesByName
            {
                Console.WriteLine($"[CoreSvc] Error finding process by name '{identifier}': {ex.Message}. This identifier may be invalid for process name matching.");
            }
            Console.WriteLine($"[CoreSvc] Window '{identifier}' not found.");
            return null;
        }

        public string GetTextFromElement(AutomationElement element)
        {
            // This is the migrated GetTextFromElementInternal logic
            if (element == null)
            {
                Console.WriteLine("[CoreSvc.GetText] Element is null, returning empty string."); // Conditional
                return string.Empty;
            }

            Console.WriteLine($"[CoreSvc.GetText] Processing element: {element.Current.Name} (ClassName: {element.Current.ClassName}, ControlType: {element.Current.LocalizedControlType})"); // Conditional
            string accumulatedText = "";
            try
            {
                // Attempt to get text from the main element itself first
                if (element.TryGetCurrentPattern(TextPattern.Pattern, out object textPatternObj))
                {
                    TextPattern textPattern = (TextPattern)textPatternObj;
                    string text = textPattern.DocumentRange.GetText(-1).Trim();
                    Console.WriteLine($"[CoreSvc.GetText] Main element TextPattern: '{text}'"); // Conditional
                    if (!string.IsNullOrEmpty(text)) accumulatedText += text + "\n";
                }
                else if (element.TryGetCurrentPattern(ValuePattern.Pattern, out object valuePatternObj))
                {
                    ValuePattern valuePattern = (ValuePattern)valuePatternObj;
                    string value = valuePattern.Current.Value.Trim();
                    Console.WriteLine($"[CoreSvc.GetText] Main element ValuePattern: '{value}'"); // Conditional
                    if (!string.IsNullOrEmpty(value)) accumulatedText += value + "\n";
                }
                else if (!string.IsNullOrEmpty(element.Current.Name))
                {
                    // Fallback to Name property if patterns are not supported
                    string name = element.Current.Name.Trim();
                    Console.WriteLine($"[CoreSvc.GetText] Main element Name property: '{name}'"); // Conditional
                    if (!string.IsNullOrEmpty(name)) accumulatedText += name + "\n";
                }
                else
                {
                    Console.WriteLine("[CoreSvc.GetText] Main element: No TextPattern, ValuePattern, or Name found."); // Conditional
                }

                // Recursively (or iteratively) get text from children - using FindAll with TreeScope.Descendants
                // It's important to avoid adding duplicate text if parent already provided it or if children text is part of parent's text pattern.
                // The original logic re-added child text. A more refined approach might be needed for complex UIs.
                // For now, keeping a similar logic but being mindful of potential duplicates from descendants.
                Console.WriteLine("[CoreSvc.GetText] Attempting to get text from descendants..."); // Conditional
                AutomationElementCollection children = element.FindAll(TreeScope.Descendants, Condition.TrueCondition);
                Console.WriteLine($"[CoreSvc.GetText] Found {children.Count} descendants."); // Conditional
                foreach (AutomationElement child in children)
                {
                    string childTextValue = "";
                    try
                    {
                        if (child.TryGetCurrentPattern(TextPattern.Pattern, out object childTextPatternObj))
                        {
                            childTextValue = ((TextPattern)childTextPatternObj).DocumentRange.GetText(-1).Trim();
                            if (!string.IsNullOrEmpty(childTextValue)) Console.WriteLine($"[CoreSvc.GetText] Child TextPattern ({child.Current.Name}): '{childTextValue}'"); // Conditional
                        }
                        else if (child.TryGetCurrentPattern(ValuePattern.Pattern, out object childValuePatternObj))
                        {
                            childTextValue = ((ValuePattern)childValuePatternObj).Current.Value.Trim();
                            if (!string.IsNullOrEmpty(childTextValue)) Console.WriteLine($"[CoreSvc.GetText] Child ValuePattern ({child.Current.Name}): '{childTextValue}'"); // Conditional
                        }
                        else if (!string.IsNullOrEmpty(child.Current.Name))
                        {
                           childTextValue = child.Current.Name.Trim();
                           if (!string.IsNullOrEmpty(childTextValue)) Console.WriteLine($"[CoreSvc.GetText] Child Name property ({child.Current.Name}): '{childTextValue}'"); // Conditional
                        }


                        if (!string.IsNullOrEmpty(childTextValue) && !accumulatedText.Contains(childTextValue))
                        {
                            accumulatedText += childTextValue + "\n";
                        }
                    }
                    catch (ElementNotAvailableException)
                    {
                        Console.WriteLine($"[CoreSvc.GetText] Child element became unavailable: {child?.Current.Name ?? "N/A"}"); // Conditional
                        continue;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[CoreSvc.GetText] Error processing child element ({child?.Current.Name ?? "N/A"}): {ex.Message}"); // Conditional
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CoreSvc.GetText] Error in GetTextFromElement for element {element?.Current.Name ?? "N/A"}: {ex.Message}"); // Conditional
            }
            Console.WriteLine($"[CoreSvc.GetText] Accumulated text before final trim for {element?.Current.Name ?? "N/A"}:\n{accumulatedText}"); // Conditional
            return accumulatedText.Trim();
        }

        public async Task<(string? matchedText, string? error)> ExtractAndMatchAsync(string windowIdentifier, string regexPattern)
        {
            Console.WriteLine($"[CoreSvc] Attempting extraction: Window '{windowIdentifier}', Regex: '{regexPattern}'"); // Conditional
            AutomationElement? targetWindow = await Task.Run(() => FindWindow(windowIdentifier));

            if (targetWindow == null)
            {
                return (null, $"Window '{windowIdentifier}' not found.");
            }
            if (DebugMode) Console.WriteLine($"[CoreSvc] Found window: {targetWindow.Current.Name}");

            string allText = await Task.Run(() => GetTextFromElement(targetWindow));
            if (string.IsNullOrWhiteSpace(allText))
            {
                return (null, "No text extracted from the window.");
            }
            // Console.WriteLine($"[CoreSvc] Extracted text:\n{allText}"); // For debugging

            try
            {
                Match m = Regex.Match(allText, regexPattern, RegexOptions.Multiline | RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    string result = m.Groups.Count > 1 && !string.IsNullOrEmpty(m.Groups[1].Value) ? m.Groups[1].Value : m.Value;
                    Console.WriteLine("[CoreSvc] Match found: " + result);
                    return (result, null);
                }
                else
                {
                    Console.WriteLine("[CoreSvc] No match found.");
                    return (null, "No match found.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CoreSvc] Error during regex matching: {ex.Message}");
                return (null, $"Error during regex matching: {ex.Message}");
            }
        }

        // Method for listing windows (can be called by console app)
        public void ListAvailableWindows()
        {
            Console.WriteLine("Available Windows (Title - Process Name - Process ID):");
            AutomationElementCollection windows = AutomationElement.RootElement.FindAll(TreeScope.Children, Condition.TrueCondition);

            foreach (AutomationElement window in windows)
            {
                try
                {
                    if (!string.IsNullOrEmpty(window.Current.Name) && !window.Current.IsOffscreen)
                    {
                        int processId = window.Current.ProcessId;
                        string windowTitle = window.Current.Name;
                        string processName = "N/A";
                        try
                        {
                            Process process = Process.GetProcessById(processId);
                            processName = process.ProcessName;
                        }
                        catch (ArgumentException)
                        {
                            processName = "Process Exited";
                        }
                        Console.WriteLine($"Title: {windowTitle} - Process: {processName} (ID: {processId})");
                    }
                }
                catch (ElementNotAvailableException) { continue; }
                catch (Exception ex)
                {
                    Console.WriteLine($"[CoreSvc] Error listing window: {ex.Message}");
                    continue;
                }
            }
        }

        // Helper method for UI Automation.
        // This might be better placed in a utility class or as part of a more specific service if Grabador.Core grows more features.
    }
}
