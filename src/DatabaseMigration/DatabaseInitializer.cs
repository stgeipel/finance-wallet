using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using OpenTelemetry.Trace;
using Server.Database;


namespace DatabaseMigration;


public class DatabaseInitializer(
    IServiceProvider serviceProvider,
    IHostApplicationLifetime hostApplicationLifetime) : BackgroundService
{
    public const string ActivitySourceName = "Migrations";
    private static readonly ActivitySource s_activitySource = new(ActivitySourceName);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using Activity? activity = s_activitySource.StartActivity("Migrating database", ActivityKind.Client);

        try
        {
            using IServiceScope scope = serviceProvider.CreateScope();
            WalletDbContext dbDbContext = scope.ServiceProvider.GetRequiredService<WalletDbContext>();

            await EnsureDatabaseAsync(dbDbContext, stoppingToken);
            await RunMigrationAsync(dbDbContext, stoppingToken);
        }
        catch (Exception ex)
        {
            activity?.RecordException(ex);
            throw;
        }

        hostApplicationLifetime.StopApplication();
    }

    private static async Task EnsureDatabaseAsync(WalletDbContext dbDbContext, CancellationToken cancellationToken)
    {
        IRelationalDatabaseCreator dbCreator = dbDbContext.GetService<IRelationalDatabaseCreator>();

        IExecutionStrategy strategy = dbDbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            // Create the database if it does not exist.
            // Do this first so there is then a database to start a transaction against.
            if (!await dbCreator.ExistsAsync(cancellationToken))
            {
                await dbCreator.CreateAsync(cancellationToken);
            }
        });
    }

    private static async Task RunMigrationAsync(WalletDbContext dbDbContext, CancellationToken cancellationToken)
    {
        IExecutionStrategy strategy = dbDbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            // Run migration in a transaction to avoid partial migration if it fails.
            await using IDbContextTransaction transaction = await dbDbContext.Database.BeginTransactionAsync(cancellationToken);
            await dbDbContext.Database.MigrateAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });
    }
}