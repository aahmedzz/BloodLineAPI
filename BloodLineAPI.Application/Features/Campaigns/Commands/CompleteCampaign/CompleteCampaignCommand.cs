using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Application.Features.Campaigns.Dtos;
using MediatR;

namespace BloodLineAPI.Application.Features.Campaigns.Commands.CompleteCampaign;

public record CompleteCampaignCommand(string Id) : IRequest<Result<CampaignDto>>;
