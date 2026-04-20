using BloodLineAPI.Application.Features.Gamification.Models;
using BloodLineAPI.Domain.Enums;

namespace BloodLineAPI.Application.Features.Gamification.Interfaces;

public interface IPointRule
{
    PointActionType ActionType { get; }
    Task<PointRuleResult?> EvaluateAsync(GamificationContext context, CancellationToken cancellationToken);
}
