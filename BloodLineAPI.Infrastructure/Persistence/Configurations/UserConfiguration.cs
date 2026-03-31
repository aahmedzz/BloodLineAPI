using BloodLineAPI.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace BloodLineAPI.Infrastructure.Persistence.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.HasKey(u => u.Id);
            builder.Property(u => u.UserName).IsRequired().HasMaxLength(100);
            builder.Property(u => u.Email).IsRequired(false).HasMaxLength(200);
            builder.HasIndex(u => u.Email).IsUnique(false);
            builder.Property(u => u.PhoneNumber).HasMaxLength(20);
            builder.Property(u => u.UserName).HasMaxLength(100);
        }

    }
}
