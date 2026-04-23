namespace BloodLineAPI.Domain.Entities.DonationEntities;

public class CenterExclusion : BaseEntity
{
    public Guid CenterId { get; private set; }
    public DateTime Date { get; private set; }
    public bool IsClosed { get; private set; }
    public TimeSpan? SpecialOpeningTime { get; private set; }
    public TimeSpan? SpecialClosingTime { get; private set; }
    public string Reason { get; private set; } = string.Empty;

    public DonationCenter Center { get; private set; } = null!;

    private CenterExclusion()
    {
    }

    public static CenterExclusion Create(
        Guid centerId,
        DateTime date,
        bool isClosed,
        string reason,
        TimeSpan? specialOpen = null,
        TimeSpan? specialClose = null)
    {
        if (!isClosed && specialOpen.HasValue && specialClose.HasValue && specialOpen >= specialClose)
        {
            throw new DomainException("Special opening time must be before closing time.");
        }

        return new CenterExclusion
        {
            Id = Guid.NewGuid(),
            CenterId = centerId,
            Date = date.Date,
            IsClosed = isClosed,
            Reason = reason,
            SpecialOpeningTime = specialOpen,
            SpecialClosingTime = specialClose
        };
    }
}
