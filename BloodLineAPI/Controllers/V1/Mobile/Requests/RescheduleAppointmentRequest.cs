namespace BloodLineAPI.Controllers.V1.Mobile.Requests;

public sealed record RescheduleAppointmentRequest(DateTime NewScheduledDate, TimeSpan NewStartTime);
