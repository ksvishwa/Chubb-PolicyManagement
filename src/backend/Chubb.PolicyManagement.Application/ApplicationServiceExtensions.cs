using Chubb.PolicyManagement.Application.Interfaces;
using Chubb.PolicyManagement.Application.Services;
using Chubb.PolicyManagement.Application.Validators;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Chubb.PolicyManagement.Application;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IPolicyService, PolicyService>();
        
        // Register FluentValidation validators
        services.AddValidatorsFromAssemblyContaining<PolicyFilterQueryValidator>();

        return services;
    }
}
