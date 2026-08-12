using System;
using BankingApp.Api.Data;
using BankingApp.Api.Middleware;
using BankingApp.Api.Services;
using DotNetEnv;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

Env.TraversePath().Load();
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

string connectionString =
    builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException(
        "Postgres connection string is missing."
    );

builder.Services.AddDbContext<BankingDbContext>(
    options => options.UseNpgsql(connectionString)
);
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IBankAccountService, BankAccountService>();
var app = builder.Build();

using (IServiceScope scope = app.Services.CreateScope())
{
    BankingDbContext dbContext =
        scope.ServiceProvider
            .GetRequiredService<BankingDbContext>();

    dbContext.Database.Migrate();
}

app.MapControllers();
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.Run();

public partial class Program
{
}
