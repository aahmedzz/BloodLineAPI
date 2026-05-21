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
                new Role { Id = Guid.Parse("d40d9d40-4252-4a7f-ae68-3e9862d512a8"), Name = "Admin", NormalizedName = "ADMIN", ConcurrencyStamp = "1c6958c7-2806-4cec-941d-b63689dc93e3" },
                new Role { Id = Guid.Parse("e35b7194-22b6-4b2a-8924-f7b5fae5f75e"), Name = "Doctor", NormalizedName = "DOCTOR", ConcurrencyStamp = "91b5145a-e3db-412e-b95a-cd6d29e00fbb" },
                new Role { Id = Guid.Parse("7b9a5f78-1cf5-4e3a-967b-1a938c23ab7c"), Name = "LabDoctor", NormalizedName = "LABDOCTOR", ConcurrencyStamp = "eb09d12f-1e4a-4aee-8fca-caac79eb4d9b" },
                new Role { Id = Guid.Parse("1f7e02df-8a39-44d5-ab85-9b87f252601c"), Name = "InventoryManager", NormalizedName = "INVENTORYMANAGER", ConcurrencyStamp = "20a435e9-1e42-4e7c-8d4f-0181c52d5845" },
                new Role { Id = Guid.Parse("c29aac7b-bb6e-4ba0-a8c3-08dea15c2736"), Name = "Donor", NormalizedName = "DONOR", ConcurrencyStamp = "e28a94a9-1774-4d82-985b-f7f4903897c5" }
            );
        }
    }
}
