using FacilitiesMgmt.Application.Abstractions;
using FacilitiesMgmt.Application.Audit;
using FacilitiesMgmt.Application.Auth;
using FacilitiesMgmt.Application.MaintenanceRequests;
using FacilitiesMgmt.Application.Reports;
using FacilitiesMgmt.Application.Sites;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace FacilitiesMgmt.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Scoped, all of them: each depends on IAppDbContext and ICurrentTenant, whose
        // lifetimes are the request. A singleton here would capture one tenant's context
        // and serve it to everyone — the classic captive-dependency cross-tenant bug.
        services.AddScoped<IAuditWriter, AuditWriter>();
        services.AddScoped<AuthService>();
        services.AddScoped<MaintenanceRequestService>();
        services.AddScoped<SpendReportService>();
        services.AddScoped<SiteService>();

        services.AddValidatorsFromAssemblyContaining<LoginValidator>(ServiceLifetime.Singleton);

        return services;
    }
}
