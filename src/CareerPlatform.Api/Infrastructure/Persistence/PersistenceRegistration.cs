using CareerPlatform.Api.Infrastructure.Persistence.Interceptors;
using CareerPlatform.Api.Infrastructure.Persistence.Seed;
using CareerPlatform.Api.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace CareerPlatform.Api.Infrastructure.Persistence;

/// <summary>
/// Registers the persistence baseline: the <see cref="AppDbContext"/> backed by Npgsql plus the
/// <see cref="ICurrentUser"/> accessor consumed by handlers and (later) the audit interceptor
/// (Req 17.7, 18.1, 18.2, 19.3, 19.4).
/// </summary>
public static class PersistenceRegistration
{
    /// <summary>
    /// Wires <see cref="AppDbContext"/> (PostgreSQL) and <see cref="ICurrentUser"/>. Uses
    /// <c>ConnectionStrings:DefaultConnection</c>. <c>AddDbContext</c> does not open a connection
    /// at startup, so the host starts even without a reachable database; the connection is
    /// established lazily on first query.
    /// </summary>
    public static IServiceCollection AddPersistence(
        this IServiceCollection services, IConfiguration configuration)
    {
        // Ambient principal accessor for handlers and the AuditableEntityInterceptor (Req 19.3).
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpCurrentUser>();

        // SaveChanges interceptors and their in-process dispatcher, resolved from DI per scope
        // so they can depend on the scoped ICurrentUser and any per-request handlers (Req 10, 11).
        services.AddScoped<IDomainEventDispatcher, InProcessDomainEventDispatcher>();
        services.AddScoped<AuditableEntityInterceptor>();
        services.AddScoped<DomainEventInterceptor>();

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        // Use the IServiceProvider-aware overload so the interceptors can be resolved from the
        // request scope and attached to the context options (Req 10, 11).
        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString);
            options.AddInterceptors(
                sp.GetRequiredService<AuditableEntityInterceptor>(),
                sp.GetRequiredService<DomainEventInterceptor>());
        });

        // Idempotent seeders + orchestrator. Registration only — seeding is NOT run at startup;
        // a caller resolves DatabaseSeeder from a scope and invokes SeedAsync explicitly (Req 17.8).
        services.AddDatabaseSeeders(configuration);

        return services;
    }
}
