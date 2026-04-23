namespace BloodLineAPI.Domain.Common;

public class AppointmentSettings
{
    public int GracePeriodMinutes { get; set; } = 30;
    public int DefaultScreeningLockoutDays { get; set; } = 7;
}
