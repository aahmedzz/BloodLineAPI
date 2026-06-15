namespace BloodLineAPI.Controllers.V1.System.Requests
{
    public sealed class SubmitResultRequest
    {
        public string ConfirmedBloodType { get; set; } = string.Empty;
        public string Hcv { get; set; } = string.Empty;
        public string Hbv { get; set; } = string.Empty;
        public string Syphilis { get; set; } = string.Empty;
        public string Hiv { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }
}
