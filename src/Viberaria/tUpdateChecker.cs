using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Viberaria;

public static class tUpdateChecker
{
    private static readonly HttpClient client = new HttpClient();

    public static async Task<string> CheckForUpdateAsync(string version)
    {
        try
        {
            var url = "https://api.github.com/repos/notasuka/Viberaria/releases/latest";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("User-Agent", "Viberaria-Mod");
            
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var latestTag = doc.RootElement.GetProperty("tag_name").GetString();

            if (string.IsNullOrEmpty(latestTag))
                return null;

            latestTag = latestTag.TrimStart('v'); // Remove 'v' prefix if present

            if (IsNewerVersion(version, latestTag))
                return latestTag;

            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Update check failed: {ex.Message}");
            return null;
        }
    }
    
    private static bool IsNewerVersion(string v1, string v2)
    {
        var a = Array.ConvertAll(v1.Split('.'), int.Parse);
        var b = Array.ConvertAll(v2.Split('.'), int.Parse);

        for (int i = 0; i < Math.Max(a.Length, b.Length); i++)
        {
            int x = i < a.Length ? a[i] : 0;
            int y = i < b.Length ? b[i] : 0;

            if (y > x) return true;
            if (y < x) return false;
        }
        return false;
    }
}