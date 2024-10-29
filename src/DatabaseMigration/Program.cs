using DatabaseMigration;
using Microsoft.EntityFrameworkCore;
using Server.Database;
using ServiceDefaults;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<DatabaseInitializer>();

builder.AddServiceDefaults();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource(DatabaseInitializer.ActivitySourceName));

builder.AddNpgsqlDbContext<WalletDbContext>(
    "finance-wallet", 
    configureDbContextOptions: optionsBuilder => 
        optionsBuilder.UseSnakeCaseNamingConvention());


IHost host = builder.Build();
await host.RunAsync();
