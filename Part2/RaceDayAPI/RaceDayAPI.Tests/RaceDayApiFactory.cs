using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Configuration;
using RaceDayAPI.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace RaceDayAPI.Tests;

public class RaceDayApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"RaceDayTests-{Guid.NewGuid()}";
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?> { ["OrganiserRegistrationCode"] = "TEST-ONLY-ORGANISER-CODE" }));

        builder.ConfigureServices(services =>
        {
            services.AddDataProtection().UseEphemeralDataProtectionProvider();
            services.RemoveAll<DbContextOptions<RaceDayDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<RaceDayDbContext>>();

            services.AddDbContext<RaceDayDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
            });
        });
    }
}
