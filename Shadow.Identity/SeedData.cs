using IdentityModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Shadow.Identity.Data;
using Shadow.Identity.Models;
using System.Security.Claims;

namespace Shadow.Identity;

public class SeedData
{
    public static async Task EnsureSeedDataAsync(WebApplication app)
    {
        await using var scope = app.Services.GetRequiredService<IServiceScopeFactory>().CreateAsyncScope();

            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            // apply pending migrations if it's safe. If there are pending migrations that would
            // change identity/column properties we skip automatic migration to avoid data loss.
            var pending = (await context.Database.GetPendingMigrationsAsync()).ToList();
            if (pending.Count > 0)
            {
                Log.Warning("{Count} pending EF Core migrations detected. Skipping automatic Migrate() to avoid schema changes: {Migrations}", pending.Count, string.Join(',', pending));
            }
            else
            {
                await context.Database.MigrateAsync();
            }

            var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var alice = await userMgr.FindByNameAsync("alice");
            if (alice == null)
            {
                alice = new ApplicationUser
                {
                    UserName = "alice",
                    Email = "AliceSmith@email.com",
                    EmailConfirmed = true,
                };

                var result = await userMgr.CreateAsync(alice, "Pass123$");
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException(result.Errors.First().Description);
                }

                result = await userMgr.AddClaimsAsync(alice, new Claim[] {
                    new Claim(JwtClaimTypes.Name, "Alice Smith"),
                    new Claim(JwtClaimTypes.GivenName, "Alice"),
                    new Claim(JwtClaimTypes.FamilyName, "Smith"),
                    new Claim(JwtClaimTypes.WebSite, "http://alice.com")
                });

                if (!result.Succeeded)
                {
                    throw new InvalidOperationException(result.Errors.First().Description);
                }

                Log.Debug("alice created");
            }
            else
            {
                Log.Debug("alice already exists");
            }

            var bob = await userMgr.FindByNameAsync("bob");
            if (bob == null)
            {
                bob = new ApplicationUser
                {
                    UserName = "bob",
                    Email = "BobSmith@email.com",
                    EmailConfirmed = true
                };

                var result = await userMgr.CreateAsync(bob, "Pass123$");
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException(result.Errors.First().Description);
                }

                result = await userMgr.AddClaimsAsync(bob, new Claim[] {
                    new Claim(JwtClaimTypes.Name, "Bob Smith"),
                    new Claim(JwtClaimTypes.GivenName, "Bob"),
                    new Claim(JwtClaimTypes.FamilyName, "Smith"),
                    new Claim(JwtClaimTypes.WebSite, "http://bob.com"),
                    new Claim("location", "somewhere")
                });

                if (!result.Succeeded)
                {
                    throw new InvalidOperationException(result.Errors.First().Description);
                }

                Log.Debug("bob created");
            }
            else
            {
                Log.Debug("bob already exists");
            }
    }

    // keep a synchronous shim for callers that expect the old API
    public static void EnsureSeedData(WebApplication app)
    {
        EnsureSeedDataAsync(app).GetAwaiter().GetResult();
    }
}
