using BloodLineAPI.Application.Common.Models;
using MediatR;

namespace BloodLineAPI.Application.Features.Campaigns.Commands.DeleteCampaign;

public record DeleteCampaignCommand(string Id) : IRequest<Result<Unit>>;
