namespace BloodLineAPI.Controllers.V1.System.Requests;

public record IssueBloodBagsRequest(
    List<Guid> BagIds,
    string RecipientName,
    string NationalId,
    string? Phone,
    string Reason,
    Guid? BloodDemandId = null
);
