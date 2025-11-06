using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace PaddleBook.Tests.Integration;

public class AdminCourtsTests : IClassFixture<CustomWebAppFactory>
{
    private readonly HttpClient _client;

    public AdminCourtsTests(CustomWebAppFactory factory)
    {
        _client = factory.CreateClient(); // entorno "Testing" + InMemory + JWT config
    }

    [Fact]
    public async Task Player_Cannot_Create_Court_Should_Return_403()
    {
        // login como Player
        var token = await AuthTokenHelper.LoginAndGetTokenAsync(_client,
            "player@paddlebook.local", "Player123$");
        _client.UseBearer(token);

        var create = await _client.PostAsJsonAsync("/courts", new
        {
            name = "Pista 1",
            surface = "resina"   // usa los campos reales de tu DTO
        });

        create.StatusCode.Should().Be(HttpStatusCode.Forbidden); // 403
    }

    [Fact]
    public async Task Admin_Can_Create_And_List_Courts()
    {
        // login como Admin
        var token = await AuthTokenHelper.LoginAndGetTokenAsync(_client,
            "admin@paddlebook.local", "Admin123$");
        _client.UseBearer(token);

        // Crear court
        var create = await _client.PostAsJsonAsync("/courts", new
        {
            name = "Central",
            surface = "resina"
        });
        create.StatusCode.Should().Be(HttpStatusCode.Created);

        // Listar courts (GET público)
        var list = await _client.GetAsync("/courts");
        list.StatusCode.Should().Be(HttpStatusCode.OK);

        // Leer la respuesta de forma robusta: el endpoint puede devolver un array
        // directamente o un objeto wrapper { "items": [...] } u otra forma.
        var json = await list.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "null" : json);
        var root = doc.RootElement;

        JsonElement arrayElement;
        if (root.ValueKind == JsonValueKind.Array)
        {
            arrayElement = root;
        }
        else if (root.ValueKind == JsonValueKind.Object)
        {
            if (root.TryGetProperty("items", out var it) && it.ValueKind == JsonValueKind.Array)
                arrayElement = it;
            else if (root.TryGetProperty("data", out var dt) && dt.ValueKind == JsonValueKind.Array)
                arrayElement = dt;
            else
                throw new InvalidOperationException("Formato de respuesta inesperado en /courts. Esperado array o { items: [...] }.");
        }
        else
        {
            throw new InvalidOperationException("Respuesta vacía o no JSON válido desde /courts.");
        }

        var found = false;
        foreach (var el in arrayElement.EnumerateArray())
        {
            if (el.ValueKind != JsonValueKind.Object) continue;
            if (el.TryGetProperty("name", out var nameProp))
            {
                var name = nameProp.GetString() ?? "";
                if (name.Equals("Central", StringComparison.OrdinalIgnoreCase))
                {
                    found = true;
                    break;
                }
            }
        }

        found.Should().BeTrue();
    }
}
