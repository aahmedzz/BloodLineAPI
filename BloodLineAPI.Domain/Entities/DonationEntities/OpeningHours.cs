namespace BloodLineAPI.Domain.Entities.DonationEntities;

public class OpeningHours : BaseEntity
{
    public Guid CenterId { get; private set; }
    public DayOfWeek DayOfWeek { get; private set; }
    public bool IsClosed { get; private set; }
    public TimeSpan OpeningTime { get; private set; }
    public TimeSpan ClosingTime { get; private set; }
    public int? MaxDonorsPerSlot { get; private set; }

    public DonationCenter Center { get; private set; } = null!;

    private OpeningHours()
    {
    }

    public static OpeningHours Create(
        Guid centerId,
        DayOfWeek dayOfWeek,
        bool isClosed,
        TimeSpan openingTime,
        TimeSpan closingTime,
        int? maxDonorsPerSlot = null)
    {
        if (!isClosed && openingTime >= closingTime)
        {
            throw new DomainException("Opening time must be before closing time.");
        }

        return new OpeningHours
        {
            Id = Guid.NewGuid(),
            CenterId = centerId,
            DayOfWeek = dayOfWeek,
            IsClosed = isClosed,
            OpeningTime = openingTime,
            ClosingTime = closingTime,
            MaxDonorsPerSlot = maxDonorsPerSlot
        };
    }
}
