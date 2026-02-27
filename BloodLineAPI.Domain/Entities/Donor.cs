using BloodLineAPI.Domain.Common;
using BloodLineAPI.Domain.Enums;

namespace BloodLineAPI.Domain.Entities;

public sealed class Donor : AuditableEntity
{
    public string FullName { get; set; } = default!;
    public DateOnly DateOfBirth { get; set; }
    public BloodType BloodType { get; set; }
    public string PhoneNumber { get; set; } = default!;
}
