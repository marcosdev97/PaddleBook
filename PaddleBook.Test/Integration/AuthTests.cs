using FluentAssertions;
using System.Net.Http.Json;
using System.Text.Json;

namespace PaddleBook.Tests.Integration;

public class AuthTests : IClassFixture<CustomWebAppFactory>
{
    private readonly HttpClient _client;

    public AuthTests(CustomWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_Then_Login_Should_Return_Token()
    {
        // Register
        var registerResponse = await _client.PostAsJsonAsync("/auth/register",
            new { email = "test@paddle.com", password = "secret123" });

        registerResponse.IsSuccessStatusCode.Should().BeTrue();

        // Login
        var loginResponse = await _client.PostAsJsonAsync("/auth/login",
            new { email = "test@paddle.com", password = "secret123" });

        loginResponse.IsSuccessStatusCode.Should().BeTrue();

        var content = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        content.TryGetProperty("accessToken", out _).Should().BeTrue();
    }
}
