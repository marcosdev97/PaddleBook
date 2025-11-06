using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PaddleBook.Infrastructure.Identity;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PaddleBook.Infrastructure.Persistence;

public static class DataSeeder
{
    /// <summary>
    /// Crea roles básicos y un usuario admin por defecto (idempotente).
    /// </summary>
    public static async Task SeedAsync(
        UserManager<AppUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager)
    {
        // 1) Roles
        var roles = new[] { "Admin", "Player" };
        foreach (var role in roles)
        {

            var normalized = role.ToUpperInvariant();
            var exists = await roleManager.Roles
                .AnyAsync(r => r.NormalizedName == normalized);

            if (!exists)
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
        }

        // 2) Usuario Admin
        const string adminEmail = "admin@paddlebook.local";
        const string adminPassword = "Admin123$"; // SOLO dev/testing

        var admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin is null)
        {
            admin = new AppUser
            {
                Id = Guid.NewGuid(),
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FullName = "Admin"
            };

            var create = await userManager.CreateAsync(admin, adminPassword);
            if (!create.Succeeded)
                throw new Exception("Admin creation failed: " +
                    string.Join(", ", create.Errors.Select(e => e.Description)));
        }

        // asignación al rol (idempotente)
        if (!await userManager.IsInRoleAsync(admin, "Admin"))
            await userManager.AddToRoleAsync(admin, "Admin");


        // 3) (Opcional) Usuario normal
        const string userEmail = "player@paddlebook.local";
        const string userPassword = "Player123$";

        var player = await userManager.FindByEmailAsync(userEmail);
        if (player is null)
        {
            player = new AppUser
            {
                Id = Guid.NewGuid(),
                UserName = userEmail,
                Email = userEmail,
                EmailConfirmed = true,
                FullName = "Player One"
            };

            var create = await userManager.CreateAsync(player, userPassword);
            if (!create.Succeeded)
                throw new Exception("Player creation failed: " +
                    string.Join(", ", create.Errors.Select(e => e.Description)));

            await userManager.AddToRoleAsync(player, "Player");
        }
    }
}
