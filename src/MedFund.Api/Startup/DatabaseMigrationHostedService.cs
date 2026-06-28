using MedFund.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MedFund.Api.Startup;

public sealed class DatabaseMigrationHostedService : IHostedService
{
    private readonly IServiceProvider serviceProvider;
    private readonly IConfiguration configuration;
    private readonly IHostEnvironment environment;
    private readonly ILogger<DatabaseMigrationHostedService> logger;

    public DatabaseMigrationHostedService(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger<DatabaseMigrationHostedService> logger)
    {
        this.serviceProvider = serviceProvider;
        this.configuration = configuration;
        this.environment = environment;
        this.logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
        {
            logger.LogInformation("Database migration on startup is disabled.");
            return;
        }

        await using var scope = serviceProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<MedFundDbContext>();

        var pendingMigrations = await db.Database.GetPendingMigrationsAsync(cancellationToken);
        var pendingMigrationList = pendingMigrations.ToArray();

        if (pendingMigrationList.Length == 0)
        {
            logger.LogInformation("Database is already up to date. No pending migrations found.");
        }
        else
        {
            logger.LogInformation(
                "Applying {MigrationCount} pending database migration(s): {Migrations}",
                pendingMigrationList.Length,
                string.Join(", ", pendingMigrationList));

            await db.Database.MigrateAsync(cancellationToken);

            logger.LogInformation("Database migrations applied successfully.");
        }

        if (ShouldSeedDevelopmentData())
        {
            await scope.ServiceProvider.GetRequiredService<IDevelopmentDataSeeder>().SeedAsync(cancellationToken);
            logger.LogInformation("Development seed data is ready.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        return Task.CompletedTask;
    }

    private bool ShouldSeedDevelopmentData()
    {
        if (configuration.GetValue<bool?>("Database:SeedDevelopmentDataOnStartup") is { } configured)
        {
            return configured;
        }

        return environment.IsDevelopment();
    }
}
