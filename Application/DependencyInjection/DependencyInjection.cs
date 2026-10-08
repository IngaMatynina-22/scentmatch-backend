using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using ScentMatch.Application.Auth.Validators;

namespace ScentMatch.Application.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();
        return services;
    }
}
