namespace BloodLineAPI.Controllers.V1.System.Requests;

public record DisposeBloodBagsRequest(
    List<Guid> BagIds,
    string Reason,
    string? Notes
);
