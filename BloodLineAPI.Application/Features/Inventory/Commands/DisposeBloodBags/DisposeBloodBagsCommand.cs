using MediatR;

namespace BloodLineAPI.Application.Features.Inventory.Commands.DisposeBloodBags;

public sealed record DisposeBloodBagsCommand(
    List<Guid> BagIds,
    string Reason,
    string? Notes) : IRequest<DisposeBloodBagsResult>;
