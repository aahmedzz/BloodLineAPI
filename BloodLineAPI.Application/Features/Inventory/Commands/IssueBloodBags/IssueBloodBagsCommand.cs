using MediatR;

namespace BloodLineAPI.Application.Features.Inventory.Commands.IssueBloodBags;

public sealed record IssueBloodBagsCommand(
    List<Guid> BagIds,
    string RecipientName,
    string NationalId,
    string? Phone,
    string Reason) : IRequest<IssueBloodBagsResult>;
