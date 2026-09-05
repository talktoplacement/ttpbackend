namespace CareerPlatform.Api.Infrastructure.Persistence.Seed;

/// <summary>
/// Registers the idempotent seeders and the <see cref="DatabaseSeeder"/> orchestrator. Registering
/// here does NOT run seeding at startup; a caller resolves <see cref="DatabaseSeeder"/> from a
/// scope and invokes <see cref="DatabaseSeeder.SeedAsync"/> explicitly (Req 17.8).
/// </summary>
public static class SeedRegistration
{
    public static IServiceCollection AddDatabaseSeeders(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AdminSeedOptions>(
            configuration.GetSection(AdminSeedOptions.SectionName));

        // Each concrete seeder is registered against the shared ISeeder contract so the
        // orchestrator discovers them and runs them in Order.
        services.AddScoped<ISeeder, RoleSeeder>();
        services.AddScoped<ISeeder, AdminSeeder>();
        services.AddScoped<ISeeder, SubscriptionPlanSeeder>();
        services.AddScoped<ISeeder, PlatformSettingSeeder>();
        services.AddScoped<DatabaseSeeder>();

        return services;
    }
}
