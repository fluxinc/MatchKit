using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Automation;
using System.Net.Http; // Added for HttpClient
// Note: System.Windows.Forms should not be referenced here for core logic if it's to be truly shareable outside WinForms contexts.

namespace MatchKit.Core
{
    public class TextAutomationService
    {
        public static bool DebugMode { get; set; } = false;

        private AutomationElement FindWindow(string identifier)
        {
            if (DebugMode) Console.WriteLine($"[CoreSvc] Attempting to find window with identifier: `{identifier}`");

            AutomationElementCollection windows = AutomationElement.RootElement.FindAll(TreeScope.Children, Condition.TrueCondition);
            foreach (AutomationElement windowElement in windows)
            {
                try
                {
                    if (windowElement == null || string.IsNullOrEmpty(windowElement.Current.Name) ||
                        windowElement.Current.IsOffscreen)
                        continue;
                    if (!Regex.IsMatch(windowElement.Current.Name, identifier, RegexOptions.IgnoreCase)) 
                        continue;
                    if (DebugMode) Console.WriteLine($"[CoreSvc] Found window by title regex: {windowElement.Current.Name}");
                    return windowElement;
                }
                catch (ElementNotAvailableException)
                {
                    continue;
                }
            }

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
                        if (p.MainWindowHandle == IntPtr.Zero) continue;
                        
                        AutomationElement window = AutomationElement.FromHandle(p.MainWindowHandle);
                        if (window == null || string.IsNullOrEmpty(window.Current.Name)) continue;
                        
                        if (DebugMode) Console.WriteLine($"[CoreSvc] Found window by process name: {window.Current.Name} (PID: {p.Id})");
                        return window;
                    }
                }
            }
            catch (ArgumentException ex)
            {
                if (DebugMode) Console.WriteLine($"[CoreSvc] Error finding process by name '{identifier}': {ex.Message}. This identifier may be invalid for process name matching.");
            }
            if (DebugMode) Console.WriteLine($"[CoreSvc] Window '{identifier}' not found.");
            return null;
        }

        private static string GetTextFromElement(AutomationElement element)
        {
            if (element == null)
            {
                if (DebugMode) Console.WriteLine("[CoreSvc.GetText] Element is null, returning empty string.");
                return string.Empty;
            }

            if (DebugMode) Console.WriteLine($"[CoreSvc.GetText] Processing element: {element.Current.Name} (ClassName: {element.Current.ClassName}, ControlType: {element.Current.LocalizedControlType})");
            string accumulatedText = "";
            try
            {
                if (element.TryGetCurrentPattern(TextPattern.Pattern, out object textPatternObj))
                {
                    TextPattern textPattern = (TextPattern)textPatternObj;
                    string text = textPattern.DocumentRange.GetText(-1).Trim();
                    if (DebugMode) Console.WriteLine($"[CoreSvc.GetText] Main element TextPattern: '{text}'");
                    if (!string.IsNullOrEmpty(text)) accumulatedText += text + "\n";
                }
                else if (element.TryGetCurrentPattern(ValuePattern.Pattern, out object valuePatternObj))
                {
                    ValuePattern valuePattern = (ValuePattern)valuePatternObj;
                    string value = valuePattern.Current.Value.Trim();
                    if (DebugMode) Console.WriteLine($"[CoreSvc.GetText] Main element ValuePattern: '{value}'");
                    if (!string.IsNullOrEmpty(value)) accumulatedText += value + "\n";
                }
                else if (!string.IsNullOrEmpty(element.Current.Name))
                {
                    string name = element.Current.Name.Trim();
                    if (DebugMode) Console.WriteLine($"[CoreSvc.GetText] Main element Name property: '{name}'");
                    if (!string.IsNullOrEmpty(name)) accumulatedText += name + "\n";
                }
                else
                {
                    if (DebugMode) Console.WriteLine("[CoreSvc.GetText] Main element: No TextPattern, ValuePattern, or Name found.");
                }

                if (DebugMode) Console.WriteLine("[CoreSvc.GetText] Attempting to get text from descendants...");
                AutomationElementCollection children = element.FindAll(TreeScope.Descendants, Condition.TrueCondition);
                if (DebugMode) Console.WriteLine($"[CoreSvc.GetText] Found {children.Count} descendants.");
                foreach (AutomationElement child in children)
                {
                    string childTextValue = "";
                    try
                    {
                        if (child.TryGetCurrentPattern(TextPattern.Pattern, out object childTextPatternObj))
                        {
                            childTextValue = ((TextPattern)childTextPatternObj).DocumentRange.GetText(-1).Trim();
                            if (DebugMode && !string.IsNullOrEmpty(childTextValue)) Console.WriteLine($"[CoreSvc.GetText] Child TextPattern ({child.Current.Name}): '{childTextValue}'");
                        }
                        else if (child.TryGetCurrentPattern(ValuePattern.Pattern, out object childValuePatternObj))
                        {
                            childTextValue = ((ValuePattern)childValuePatternObj).Current.Value.Trim();
                            if (DebugMode && !string.IsNullOrEmpty(childTextValue)) Console.WriteLine($"[CoreSvc.GetText] Child ValuePattern ({child.Current.Name}): '{childTextValue}'");
                        }
                        else if (!string.IsNullOrEmpty(child.Current.Name))
                        {
                           childTextValue = child.Current.Name.Trim();
                           if (DebugMode && !string.IsNullOrEmpty(childTextValue)) Console.WriteLine($"[CoreSvc.GetText] Child Name property ({child.Current.Name}): '{childTextValue}'");
                        }

                        if (!string.IsNullOrEmpty(childTextValue) && !accumulatedText.Contains(childTextValue))
                        {
                            accumulatedText += childTextValue + "\n";
                        }
                    }
                    catch (ElementNotAvailableException)
                    {
                        if (DebugMode) Console.WriteLine($"[CoreSvc.GetText] Child element became unavailable: {child?.Current.Name ?? "N/A"}");
                        continue;
                    }
                    catch (Exception ex)
                    {
                        if (DebugMode) Console.WriteLine($"[CoreSvc.GetText] Error processing child element ({child?.Current.Name ?? "N/A"}): {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                if (DebugMode) Console.WriteLine($"[CoreSvc.GetText] Error in GetTextFromElement for element {element?.Current.Name ?? "N/A"}: {ex.Message}");
            }
            if (DebugMode) Console.WriteLine($"[CoreSvc.GetText] Accumulated text before final trim for {element?.Current.Name ?? "N/A"}:\n{accumulatedText}");
            return accumulatedText.Trim();
        }

        public async Task<(string? matchedText, string? error)> ExtractAndMatchAsync(string windowIdentifier, string regexPattern)
        {
            if (DebugMode) Console.WriteLine($"[CoreSvc] Attempting extraction: Window '{windowIdentifier}', Regex: '{regexPattern}'");
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

            try
            {
                Match m = Regex.Match(allText, regexPattern, RegexOptions.Multiline | RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    string result = m.Groups.Count > 1 && !string.IsNullOrEmpty(m.Groups[1].Value) ? m.Groups[1].Value : m.Value;
                    if (DebugMode) Console.WriteLine("[CoreSvc] Match found: " + result);
                    return (result, null);
                }

                if (DebugMode) Console.WriteLine("[CoreSvc] No match found.");
                return (null, "No match found.");
            }
            catch (Exception ex)
            {
                if (DebugMode) Console.WriteLine($"[CoreSvc] Error during regex matching: {ex.Message}");
                return (null, $"Error during regex matching: {ex.Message}");
            }
        }

        public void ListAvailableWindows()
        {
            if (DebugMode) Console.WriteLine("Available Windows (Title - Process Name - Process ID):");
            AutomationElementCollection windows = AutomationElement.RootElement.FindAll(TreeScope.Children, Condition.TrueCondition);

            foreach (AutomationElement window in windows)
            {
                try
                {
                    if (string.IsNullOrEmpty(window.Current.Name) || window.Current.IsOffscreen) 
                        continue;
                    
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
                    if (DebugMode) Console.WriteLine($"Title: {windowTitle} - Process: {processName} (ID: {processId})");
                }
                catch (ElementNotAvailableException) { continue; }
                catch (Exception ex)
                {
                    if (DebugMode) Console.WriteLine($"[CoreSvc] Error listing window: {ex.Message}");
                }
            }
        }
    }
}
