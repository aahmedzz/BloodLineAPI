using System.Collections.Generic;

namespace BloodLineAPI.Domain.Entities;

public class BloodStockThreshold : BaseEntity
{
    public byte? BloodTypeId { get; set; }
    public BloodType? BloodType { get; set; }
    public int LowThreshold { get; set; }
    public int CriticalThreshold { get; set; }

    public static readonly IReadOnlyDictionary<byte, (int Low, int Critical)> DefaultThresholds = new Dictionary<byte, (int Low, int Critical)>
    {
        { 1, (10, 5) },  // A+
        { 2, (8, 4) },   // A-
        { 3, (12, 6) },  // B+
        { 4, (10, 5) },  // B-
        { 5, (5, 2) },   // AB+
        { 6, (5, 2) },   // AB-
        { 7, (15, 7) },  // O+
        { 8, (10, 5) }   // O-
    };
}
