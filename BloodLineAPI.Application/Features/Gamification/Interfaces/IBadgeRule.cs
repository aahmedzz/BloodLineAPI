using BloodLineAPI.Application.Features.Gamification.Models;

namespace BloodLineAPI.Application.Features.Gamification.Interfaces;

public interface IBadgeRule
{
    string BadgeKey { get; }
    Task<bool> IsEarnedAsync(GamificationContext context, CancellationToken cancellationToken);
}
