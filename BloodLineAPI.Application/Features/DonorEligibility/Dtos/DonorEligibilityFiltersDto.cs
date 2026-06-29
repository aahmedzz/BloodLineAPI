namespace BloodLineAPI.Application.Features.DonorEligibility.Dtos;

public class DonorEligibilityFiltersDto
{
    public string? Search { get; set; }
    public string? BloodType { get; set; }
    public string? Status { get; set; }
    public string? District { get; set; }
    public string? Gender { get; set; }
    public bool? HasMobileApp { get; set; }
}
