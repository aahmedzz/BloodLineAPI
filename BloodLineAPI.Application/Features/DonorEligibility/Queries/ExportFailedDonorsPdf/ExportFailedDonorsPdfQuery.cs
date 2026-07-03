using MediatR;
using System;

namespace BloodLineAPI.Application.Features.DonorEligibility.Queries.ExportFailedDonorsPdf;

public sealed record ExportFailedDonorsPdfQuery(
    Guid AppealId
) : IRequest<byte[]>;
