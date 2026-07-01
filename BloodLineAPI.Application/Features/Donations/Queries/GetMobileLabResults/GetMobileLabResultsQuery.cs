using BloodLineAPI.Application.Common.Models;
using MediatR;
using System;

namespace BloodLineAPI.Application.Features.Donations.Queries.GetMobileLabResults;

public sealed record GetMobileLabResultsQuery(Guid DonationId, Guid DonorId) : IRequest<Result<MobileLabResultResponse>>;
