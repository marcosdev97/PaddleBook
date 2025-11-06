using System.Net.Http.Json;
using System.Text.Json;

namespace PaddleBook.Tests.Integration;

public static class AuthTokenHelper
{
    public static async Task<string> LoginAndGetTokenAsync(HttpClient client, string email, string password)
    {
        var resp = await client.PostAsJsonAsync("/auth/login", new { email, password });
        var body = await resp.Content.ReadAsStringAsync();
        resp.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.GetProperty("accessToken").GetString()!;
    }

    public static void UseBearer(this HttpClient client, string token)
        => client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
}
