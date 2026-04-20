using System.Reflection;
using BloodLineAPI.Application.Common.Behaviors;
using BloodLineAPI.Application.Features.Gamification.Interfaces;
using BloodLineAPI.Application.Features.Gamification.Services;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace BloodLineAPI.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        services.AddTransient<IGamificationService, GamificationService>();

        var pointRuleType = typeof(IPointRule);
        var badgeRuleType = typeof(IBadgeRule);

        foreach (var ruleType in assembly.GetTypes().Where(t => !t.IsAbstract && !t.IsInterface && pointRuleType.IsAssignableFrom(t)))
        {
            services.AddTransient(pointRuleType, ruleType);
        }

        foreach (var ruleType in assembly.GetTypes().Where(t => !t.IsAbstract && !t.IsInterface && badgeRuleType.IsAssignableFrom(t)))
        {
            services.AddTransient(badgeRuleType, ruleType);
        }

        return services;
    }
}
