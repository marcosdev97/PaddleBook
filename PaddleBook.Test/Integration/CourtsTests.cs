using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace PaddleBook.Tests.Integration;

public class CourtsTests : IClassFixture<CustomWebAppFactory>
{
    private readonly HttpClient _client;

    public CourtsTests(CustomWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetCourts_Should_Be_Public()
    {
        var response = await _client.GetAsync("/courts");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PostCourt_Should_Be_Protected()
    {
        var response = await _client.PostAsJsonAsync("/courts",
            new { name = "Central", surface = "resina" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
