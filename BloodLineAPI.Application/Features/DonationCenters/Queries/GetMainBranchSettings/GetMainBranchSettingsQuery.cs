using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Application.Features.DonationCenters.Dtos;
using MediatR;

namespace BloodLineAPI.Application.Features.DonationCenters.Queries.GetMainBranchSettings;

public sealed record GetMainBranchSettingsQuery : IRequest<Result<MainBranchSettingsResult>>;
