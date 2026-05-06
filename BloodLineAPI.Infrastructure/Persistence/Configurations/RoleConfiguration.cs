using BloodLineAPI.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BloodLineAPI.Infrastructure.Persistence.Configurations
{
    public class RoleConfiguration : IEntityTypeConfiguration<Role>
    {
        public void Configure(EntityTypeBuilder<Role> builder)
        {
            builder.HasMany(r => r.UserRoles)
                .WithOne(ur => ur.Role)
                .HasForeignKey(ur => ur.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasData(
                new Role { Id = Guid.Parse("d40d9d40-4252-4a7f-ae68-3e9862d512a8"), Name = "Admin", NormalizedName = "ADMIN" },
                new Role { Id = Guid.Parse("e35b7194-22b6-4b2a-8924-f7b5fae5f75e"), Name = "Doctor", NormalizedName = "DOCTOR" },
                new Role { Id = Guid.Parse("7b9a5f78-1cf5-4e3a-967b-1a938c23ab7c"), Name = "LabDoctor", NormalizedName = "LABDOCTOR" },
                new Role { Id = Guid.Parse("1f7e02df-8a39-44d5-ab85-9b87f252601c"), Name = "InventoryManager", NormalizedName = "INVENTORYMANAGER" },
                new Role { Id = Guid.Parse("a0f7b11c-2f92-494b-a7e6-8c437190f898"), Name = "Donor", NormalizedName = "DONOR" }
            );
        }
    }
}
