using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace MatchKit.Core
{
    /// <summary>
    /// Orchestrates the complete automation workflow: text extraction, URL calling, and JSON parsing.
    /// This class provides a unified interface that can be used by both console and tray applications.
    /// </summary>
    public class AutomationOrchestrator
    {
        private readonly TextAutomationService _automationService;
        public TextAutomationService AutomationService => _automationService;

        public AutomationOrchestrator(bool debugMode = false)
        {
            TextAutomationService.DebugMode = debugMode;
            _automationService = new TextAutomationService();
        }

        /// <summary>
        /// Configuration for an automation run
        /// </summary>
        public class AutomationConfig
        {
            public string WindowIdentifier { get; set; }
            public string RegexPattern { get; set; }
            public string UrlTemplate { get; set; }
            public string JsonKey { get; set; }
            public bool DebugMode { get; set; }
        }

        /// <summary>
        /// Result of an automation run
        /// </summary>
        public class AutomationResult
        {
            public bool Success { get; set; }
            public string Value { get; set; }
            public string Error { get; set; }
            public string ExtractedText { get; set; }
            public string RawResponse { get; set; }
        }

        /// <summary>
        /// Execute the complete automation workflow
        /// </summary>
        public async Task<AutomationResult> ExecuteAsync(AutomationConfig config)
        {
            AutomationResult result = new AutomationResult();

            try
            {
                // Step 1: Extract text using regex
                if (config.DebugMode) Console.WriteLine($"[AutomationOrchestrator] Processing window: {config.WindowIdentifier} with regex: {config.RegexPattern}");

                (string? matchedText, string? extractError) = await _automationService.ExtractAndMatchAsync(config.WindowIdentifier, config.RegexPattern);

                if (extractError != null)
                {
                    result.Success = false;
                    result.Error = extractError;
                    return result;
                }

                if (matchedText == null)
                {
                    result.Success = false;
                    result.Error = "No match found";
                    return result;
                }

                result.ExtractedText = matchedText;
                result.Value = matchedText; // Default to extracted text

                // Step 2: Call URL if provided
                if (!string.IsNullOrEmpty(config.UrlTemplate))
                {
                    if (config.DebugMode) Console.WriteLine($"[AutomationOrchestrator] URL template provided: {config.UrlTemplate}");

                    string actualUrl = config.UrlTemplate.Replace("$1", Uri.EscapeDataString(matchedText));
                    if (config.DebugMode) Console.WriteLine($"[AutomationOrchestrator] Calling URL: {actualUrl}");

                    (string? responseBody, string? httpError) = await HttpUtilityService.GetUrlContentAsync(actualUrl);

                    if (httpError != null)
                    {
                        result.Success = false;
                        result.Error = $"Error calling URL: {httpError}";
                        return result;
                    }

                    result.RawResponse = responseBody;
                    result.Value = responseBody; // Update to response body

                    // Step 3: Extract JSON value if key provided
                    if (!string.IsNullOrEmpty(config.JsonKey) && !string.IsNullOrEmpty(responseBody))
                    {
                        try
                        {
                            JObject jObject = JObject.Parse(responseBody);
                            JToken token = jObject.SelectToken(config.JsonKey);

                            if (token != null)
                            {
                                result.Value = token.ToString();
                            }
                            else
                            {
                                result.Success = false;
                                result.Error = $"JSON key/path '{config.JsonKey}' not found in the response";
                                return result;
                            }
                        }
                        catch (Exception ex)
                        {
                            result.Success = false;
                            result.Error = $"Error parsing JSON response: {ex.Message}";
                            return result;
                        }
                    }
                }

                result.Success = true;
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = $"Unexpected error: {ex.Message}";
                if (config.DebugMode) Console.WriteLine($"[AutomationOrchestrator] Exception caught: {ex}");
                return result;
            }
        }

        /// <summary>
        /// List available windows (wrapper for TextAutomationService)
        /// </summary>
        public void ListAvailableWindows()
        {
            _automationService.ListAvailableWindows();
        }
    }
}
