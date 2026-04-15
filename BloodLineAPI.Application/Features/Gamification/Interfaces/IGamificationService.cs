using BloodLineAPI.Application.Features.Gamification.Models;

namespace BloodLineAPI.Application.Features.Gamification.Interfaces;

public interface IGamificationService
{
    Task ProcessAsync(GamificationContext context, CancellationToken cancellationToken);
}
