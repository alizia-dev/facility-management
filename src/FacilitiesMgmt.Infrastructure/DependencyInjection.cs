using FacilitiesMgmt.Application.Abstractions;
using FacilitiesMgmt.Infrastructure.Persistence;
using FacilitiesMgmt.Infrastructure.Security;
using Microsoft.Extensions.DependencyInjection;

namespace FacilitiesMgmt.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        // Scoped, because it depends on the scoped ICurrentTenant. Registering the
        // interceptor as a singleton would capture the first request's tenant and pin
        // every subsequent connection to it — a captive dependency, and the worst
        // possible one to get wrong here.
        
        // Same instance behind both, so a service taking IAppDbContext and one taking
        // AppDbContext share a change tracker and therefore share a transaction.
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IPasswordHasher, IdentityPasswordHasher>();

        return services;
    }
}
