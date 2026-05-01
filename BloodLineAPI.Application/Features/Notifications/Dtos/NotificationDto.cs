namespace BloodLineAPI.Application.Features.Notifications.Dtos;

public sealed record NotificationDto(
    Guid Id,
    string Title,
    string Message,
    string Type,
    Dictionary<string, string>? ActionPayload,
    bool IsRead,
    DateTime SentDate);
