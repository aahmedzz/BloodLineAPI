using MediatR;

namespace BloodLineAPI.Application.Features.Inventory.Queries.ExportOutflowPdf;

public sealed record ExportOutflowPdfQuery(
    string? Search = null,
    string? ActionType = null,
    string? BloodType = null,
    string? PerformedById = null
) : IRequest<byte[]>;
