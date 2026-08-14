using System;
using System.Linq;
using BankingApp.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BankingApp.Api.Tests;

public sealed class CustomWebApplicationFactory
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(
        IWebHostBuilder builder
    )
    {
        builder.ConfigureServices(services =>
        {
            ServiceDescriptor? descriptor =
                services.SingleOrDefault(
                    d =>
                        d.ServiceType ==
                        typeof(DbContextOptions<BankingDbContext>)
                );

            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            string connectionString =
                        Environment.GetEnvironmentVariable(
                            "ConnectionStrings__PostgresTest"
                        )
                        ?? throw new InvalidOperationException(
                            "Test Postgres connection string is missing."
                        );

            services.AddDbContext<BankingDbContext>(
                options =>
                    options.UseNpgsql(connectionString)
            );

            using ServiceProvider serviceProvider =
                services.BuildServiceProvider();

            using IServiceScope scope =
                serviceProvider.CreateScope();

            BankingDbContext dbContext =
                scope.ServiceProvider
                    .GetRequiredService<BankingDbContext>();

            dbContext.Database.Migrate();
        });
    }
}
