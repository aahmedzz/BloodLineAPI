namespace BloodLineAPI.Domain.Entities;

public class BloodStockThreshold : BaseEntity
{
    public byte? BloodTypeId { get; set; }
    public BloodType? BloodType { get; set; }
    public int LowThreshold { get; set; }
    public int CriticalThreshold { get; set; }
}
