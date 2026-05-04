using BloodLineAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BloodLineAPI.Infrastructure.Persistence.Configurations
{
    public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
    {
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
    }
}
