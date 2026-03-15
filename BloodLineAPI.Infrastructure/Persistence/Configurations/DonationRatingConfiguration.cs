using BloodBankSystem.Domain.Entities.DonationEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BloodBankSystem.Infrastructure.Persistence.Configurations
{
    public class DonationRatingConfiguration : IEntityTypeConfiguration<DonationRating>
    {
        public void Configure(EntityTypeBuilder<DonationRating> builder)
        {
            builder.HasKey(dr => dr.Id);
            builder.Property(dr => dr.FeedbackText).HasMaxLength(1000);
            builder.HasOne(dr => dr.Donor)
                .WithMany(d => d.DonationRatings)
                .HasForeignKey(dr => dr.DonorId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(dr => dr.DonationAppointment)
                .WithOne(da => da.DonationRating)
                .HasForeignKey<DonationRating>(dr => dr.DonationAppointmentId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
