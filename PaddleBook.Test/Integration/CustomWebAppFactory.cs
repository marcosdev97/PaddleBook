using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PaddleBook.Infrastructure.Persistence;

namespace PaddleBook.Tests.Integration;

public class CustomWebAppFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // 1) Entorno de tests
        builder.UseEnvironment("Testing");

        // 2) Configuración mínima para JWT en tests
        builder.ConfigureAppConfiguration((ctx, config) =>
        {
            var dict = new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "PaddleBook.Tests",
                ["Jwt:Audience"] = "PaddleBook.Tests",
                ["Jwt:Key"] = "test_key_which_is_long_enough_for_hmac256_1234567890",
                ["Jwt:ExpiresMinutes"] = "60",
                ["TestDbName"] = Guid.NewGuid().ToString("N")
            };
            config.AddInMemoryCollection(dict);
        });

        // 3) Crear la DB in-memory (ya registrada por Program.cs al ver "Testing")
        builder.ConfigureServices(services =>
        {
            using var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<PaddleDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();
        });
    }
}
