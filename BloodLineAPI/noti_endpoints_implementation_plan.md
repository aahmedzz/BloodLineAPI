# Notification System — Updated Implementation Plan (v3)

## Background

Revision of the previous plan incorporating:
- **Decoupled ActionPayload** — entity-only context, no Flutter routes
- **Removed `NotificationActionType` enum** — Flutter owns all routing decisions
- **Removed `Priority` column** — not used in the app
- **Dedicated unread-count endpoint** instead of embedding in pagination envelope
- **Bulk mark-all-as-read** endpoint
- **No deletion** — notifications are permanent
- **No SignalR** — FCM + pull-to-refresh only
- **No migrations or database updates** will be run during implementation

---

## Proposed Changes

### 1. Domain Layer

#### [NEW] [NotificationType.cs](file:///d:/AHMED/Graduation%20Project/BloodLineAPI/BloodLineAPI.Domain/Enums/NotificationType.cs)

```csharp
namespace BloodLineAPI.Domain.Enums;

public enum NotificationType
{
    Unknown = 0,
    AppointmentReminder,
    AppointmentConfirmed,
    AppointmentCancelled,
    BadgeEarned,
    RateDonationCenter,
    DonationCompleted,
    UrgentBloodAppeal,
    General
}
```

#### [MODIFY] [Notification.cs](file:///d:/AHMED/Graduation%20Project/BloodLineAPI/BloodLineAPI.Domain/Entities/Notification.cs)

```diff
+ using BloodLineAPI.Domain.Enums;
+
  public class Notification : BaseEntity
  {
      public string Title { get; set; } = string.Empty;
      public string Message { get; set; } = string.Empty;
-     public string Type { get; set; } = string.Empty;
+     public NotificationType Type { get; set; }
-     public int Priority { get; set; }
      public bool IsRead { get; set; } = false;
      public bool IsSent { get; set; } = false;
      public string? SentVia { get; set; }
      public DateTime SentDate { get; set; }
+
+     /// <summary>
+     /// JSON blob with entity-only context for frontend routing.
+     /// Example: {"targetEntity":"DonationCenter","targetId":"d4e5f6a7-...","appointmentId":"e5f6a7b8-..."}
+     /// The backend never specifies routes — Flutter decides navigation from this context.
+     /// </summary>
+     public string? ActionPayload { get; set; }

      public Guid UserId { get; set; }
      public User User { get; set; } = null!;
  }
```

---

### 2. Application Layer — DTOs

#### [NEW] [NotificationDto.cs](file:///d:/AHMED/Graduation%20Project/BloodLineAPI/BloodLineAPI.Application/Features/Notifications/Dtos/NotificationDto.cs)

```csharp
namespace BloodLineAPI.Application.Features.Notifications.Dtos;

/// <summary>
/// Notification item returned to the mobile client.
/// ActionPayload carries entity context only — no routes.
/// </summary>
public sealed record NotificationDto(
    Guid Id,
    string Title,
    string Message,
    string Type,                                   // enum name, e.g. "AppointmentConfirmed"
    Dictionary<string, string>? ActionPayload,     // e.g. {"targetEntity":"DonationCenter","targetId":"..."}
    bool IsRead,
    DateTime SentDate);
```

#### [NEW] [CursorPagedResult.cs](file:///d:/AHMED/Graduation%20Project/BloodLineAPI/BloodLineAPI.Application/Common/Models/Api/CursorPagedResult.cs)

```csharp
namespace BloodLineAPI.Application.Common.Models;

/// <summary>
/// Reusable cursor-based (keyset) pagination envelope.
/// </summary>
public sealed record CursorPagedResult<T>(
    IReadOnlyList<T> Items,
    string? NextCursor,       // null = no more pages
    bool HasMore);
```

---

### 3. Application Layer — CQRS Queries

#### [NEW] GetNotifications Query

**Files:**
- `Features/Notifications/Queries/GetNotifications/GetNotificationsQuery.cs`
- `Features/Notifications/Queries/GetNotifications/GetNotificationsQueryHandler.cs`

**Query:**
```csharp
public sealed record GetNotificationsQuery : IRequest<CursorPagedResult<NotificationDto>>
{
    [JsonIgnore] public Guid UserId { get; init; }
    public string? Cursor { get; init; }
    public int PageSize { get; init; } = 20;
}
```

**Handler** — keyset pagination with Base64-encoded `{SentDate:O}|{Id}` cursor, `Take(pageSize + 1)` to detect `HasMore`. No inline unread count query.

#### [NEW] GetUnreadCount Query

**Files:**
- `Features/Notifications/Queries/GetUnreadCount/GetUnreadCountQuery.cs`
- `Features/Notifications/Queries/GetUnreadCount/GetUnreadCountQueryHandler.cs`

```csharp
public sealed record GetUnreadCountQuery(Guid UserId) : IRequest<int>;

public sealed class GetUnreadCountQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetUnreadCountQuery, int>
{
    public async Task<int> Handle(GetUnreadCountQuery request, CancellationToken ct)
    {
        return await dbContext.Notifications
            .Where(n => n.UserId == request.UserId && !n.IsRead)
            .CountAsync(ct);
    }
}
```

---

### 4. Application Layer — CQRS Commands

#### [NEW] MarkNotificationReadCommand

**File:** `Features/Notifications/Commands/MarkNotificationRead/MarkNotificationReadCommand.cs`

```csharp
public sealed record MarkNotificationReadCommand(Guid UserId, Guid NotificationId) : IRequest;
```

Handler finds notification by `Id + UserId`, sets `IsRead = true` if not already, saves.

#### [NEW] MarkAllNotificationsReadCommand

**File:** `Features/Notifications/Commands/MarkAllNotificationsRead/MarkAllNotificationsReadCommand.cs`

```csharp
public sealed record MarkAllNotificationsReadCommand(Guid UserId) : IRequest<int>;
```

Handler uses `ExecuteUpdateAsync` for a single bulk SQL update:

```csharp
var count = await dbContext.Notifications
    .Where(n => n.UserId == request.UserId && !n.IsRead)
    .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), ct);
return count;
```

#### [MODIFY] [SendTestNotificationCommand.cs](file:///d:/AHMED/Graduation%20Project/BloodLineAPI/BloodLineAPI.Application/Features/Notifications/Commands/SendTestNotificationCommand.cs)

- `Type = "test"` → `Type = NotificationType.General`
- Remove `Priority = 0`
- Add `ActionPayload = null`

#### [MODIFY] [AppointmentReminderJob.cs](file:///d:/AHMED/Graduation%20Project/BloodLineAPI/BloodLineAPI.Infrastructure/BackgroundJobs/AppointmentReminderJob.cs)

- `Type = "appointment_reminder"` → `Type = NotificationType.AppointmentReminder`
- Remove `Priority = 1`
- Add entity-context payload:

```csharp
ActionPayload = JsonSerializer.Serialize(new Dictionary<string, string>
{
    ["targetEntity"] = "DonationAppointment",
    ["targetId"] = appt.Id.ToString()
})
```

---

### 5. Infrastructure Layer — EF Core Configuration & Indexes

#### [MODIFY] [NotificationConfiguration.cs](file:///d:/AHMED/Graduation%20Project/BloodLineAPI/BloodLineAPI.Infrastructure/Persistence/Configurations/NotificationConfiguration.cs)

```csharp
public void Configure(EntityTypeBuilder<Notification> builder)
{
    builder.HasKey(n => n.Id);
    builder.Property(n => n.Title).IsRequired().HasMaxLength(200);
    builder.Property(n => n.Message).IsRequired();

    // Enum stored as string
    builder.Property(n => n.Type)
        .HasMaxLength(50)
        .HasConversion<string>();

    // JSON payload — generous but bounded
    builder.Property(n => n.ActionPayload)
        .HasMaxLength(2000);

    // Relationships
    builder.HasOne(n => n.User)
        .WithMany(u => u.Notifications)
        .HasForeignKey(n => n.UserId)
        .OnDelete(DeleteBehavior.Cascade);

    // ═══ INDEXES ═══

    // 1. Keyset pagination: WHERE UserId = @p ORDER BY SentDate DESC, Id DESC
    builder.HasIndex(n => new { n.UserId, n.SentDate, n.Id })
        .HasDatabaseName("IX_Notifications_UserId_SentDate_Id")
        .IsDescending(false, true, true);

    // 2. Unread count: WHERE UserId = @p AND IsRead = 0 (filtered index)
    builder.HasIndex(n => new { n.UserId, n.IsRead })
        .HasDatabaseName("IX_Notifications_UserId_IsRead")
        .HasFilter("[IsRead] = 0");
}
```

---

### 6. WebAPI Layer — Controller Endpoints

#### [MODIFY] [NotificationsController.cs](file:///d:/AHMED/Graduation%20Project/BloodLineAPI/BloodLineAPI/Controllers/V1/Mobile/NotificationsController.cs)

Add 4 new endpoints to the existing controller:

| Method | Route | Purpose |
|---|---|---|
| `GET` | `/notifications` | Cursor-paginated notification list |
| `GET` | `/notifications/unread-count` | Returns `{ data: 5 }` integer count |
| `PATCH` | `/notifications/{id}/read` | Mark single notification as read |
| `PATCH` | `/notifications/read-all` | Bulk mark all as read |

---

### 7. JSON Response Contract (for Flutter)

#### GET /notifications
```json
{
  "success": true,
  "message": "Operation completed successfully.",
  "data": {
    "items": [
      {
        "id": "a1b2c3d4-...",
        "title": "Rate Your Experience",
        "message": "Please rate your experience at Beni Suef Blood Bank.",
        "type": "RateDonationCenter",
        "actionPayload": {
          "targetEntity": "DonationCenter",
          "targetId": "d4e5f6a7-...",
          "appointmentId": "e5f6a7b8-..."
        },
        "isRead": false,
        "sentDate": "2026-04-30T21:30:00Z"
      },
      {
        "id": "b2c3d4e5-...",
        "title": "Appointment Confirmed",
        "message": "Your appointment on Mar 20 is confirmed.",
        "type": "AppointmentConfirmed",
        "actionPayload": {
          "targetEntity": "DonationAppointment",
          "targetId": "c3d4e5f6-..."
        },
        "isRead": true,
        "sentDate": "2026-04-30T18:00:00Z"
      }
    ],
    "nextCursor": "MjAyNi0wNC0yOVQxMDowMDowMFp8...",
    "hasMore": true
  }
}
```

#### GET /notifications/unread-count
```json
{ "success": true, "message": "...", "data": 5 }
```

#### PATCH /notifications/read-all
```json
{ "success": true, "message": "3 notifications marked as read.", "data": 3 }
```

> [!TIP]
> **Flutter routing pseudo-code** — the frontend owns all routing logic:
> ```dart
> void onNotificationTap(NotificationDto n) {
>   if (n.actionPayload == null) return; // informational only
>   final entity = n.actionPayload!['targetEntity'];
>   final id = n.actionPayload!['targetId'];
>   switch (entity) {
>     case 'DonationCenter':      context.push('/center/$id', extra: n.actionPayload);
>     case 'DonationAppointment': context.push('/appointments/$id');
>     case 'Badge':               context.push('/badges/$id');
>   }
> }
> ```

---

## Summary of New/Modified Files

| Layer | File | Action |
|---|---|---|
| **Domain** | `Enums/NotificationType.cs` | NEW |
| **Domain** | `Entities/Notification.cs` | MODIFY — `Type` → enum, remove `Priority`, add `ActionPayload` |
| **Application** | `Common/Models/Api/CursorPagedResult.cs` | NEW |
| **Application** | `Features/Notifications/Dtos/NotificationDto.cs` | NEW |
| **Application** | `Features/Notifications/Queries/GetNotifications/` | NEW (2 files) |
| **Application** | `Features/Notifications/Queries/GetUnreadCount/` | NEW (2 files) |
| **Application** | `Features/Notifications/Commands/MarkNotificationRead/` | NEW |
| **Application** | `Features/Notifications/Commands/MarkAllNotificationsRead/` | NEW |
| **Application** | `Features/Notifications/Commands/SendTestNotificationCommand.cs` | MODIFY |
| **Infrastructure** | `Persistence/Configurations/NotificationConfiguration.cs` | MODIFY |
| **Infrastructure** | `BackgroundJobs/AppointmentReminderJob.cs` | MODIFY |
| **WebAPI** | `Controllers/V1/Mobile/NotificationsController.cs` | MODIFY — 4 new endpoints |

---

## Verification Plan

### Build Verification
- `dotnet build` — all 4 projects compile cleanly.

> [!IMPORTANT]
> **No migrations or database updates will be executed.** The user will handle migrations manually after reviewing the changes.

### Manual Verification (post-migration, by user)
1. `POST /notifications/test-notification` → verify new `Type` enum and `ActionPayload` columns persist.
2. `GET /notifications` → verify cursor pagination returns correct JSON shape.
3. `GET /notifications?cursor={nextCursor}` → verify second page.
4. `GET /notifications/unread-count` → returns integer count.
5. `PATCH /notifications/{id}/read` → verify `IsRead` flips.
6. `PATCH /notifications/read-all` → verify bulk update and returned count.
