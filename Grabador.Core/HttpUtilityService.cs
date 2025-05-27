using System.Net.Http;
using System.Threading.Tasks;

namespace Grabador.Core
{
    public static class HttpUtilityService
    {
        public static async Task<(string? responseBody, string? error)> GetUrlContentAsync(string url)
        {
            if (TextAutomationService.DebugMode) System.Console.WriteLine($"[CoreSvc.Http] Requesting URL: {url}");
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();
                        if (TextAutomationService.DebugMode) System.Console.WriteLine($"[CoreSvc.Http] Success: {url}, Body length: {responseBody.Length}");
                        return (responseBody, null);
                    }
                    else
                    {
                        string error = $"HTTP Error: {response.StatusCode} - {response.ReasonPhrase}";
                        if (TextAutomationService.DebugMode) System.Console.WriteLine($"[CoreSvc.Http] Failure: {url}, Error: {error}");
                        return (null, error);
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                 string error = $"Request exception: {ex.Message}";
                 if (TextAutomationService.DebugMode) System.Console.WriteLine($"[CoreSvc.Http] Exception: {url}, Error: {error}");
                return (null, error);
            }
            catch (System.Exception ex) // Catch other potential errors
            {
                string error = $"Generic exception: {ex.Message}";
                if (TextAutomationService.DebugMode) System.Console.WriteLine($"[CoreSvc.Http] Generic Exception: {url}, Error: {error}");
                return (null, error);
            }
        }
    }
}
