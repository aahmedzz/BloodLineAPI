using BloodLineAPI.Domain.Entities;
using BloodLineAPI.Domain.Entities.Users;
using BloodLineAPI.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BloodLineAPI.Infrastructure.Seeding;

public static class AdminAccountSeeder
{
    public static async Task SeedAdminAccountAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        const string adminEmail = "admin@bloodline.com";
        const string adminPassword = "Admin@123456";
        const string adminNationalId = "29001010123456";
        const string adminRole = "Admin";

        try
        {
            // Check if admin already exists by email
            var existingUser = await userManager.FindByEmailAsync(adminEmail);
            if (existingUser is not null)
            {
                logger.LogInformation("Admin account already exists. Skipping seed.");
                return;
            }

            // Also check by NationalId (UserName) to be safe
            var existingByUsername = await userManager.FindByNameAsync(adminNationalId);
            if (existingByUsername is not null)
            {
                logger.LogInformation("Admin account (by NationalId) already exists. Skipping seed.");
                return;
            }

            // Create the Identity User
            var adminUser = new User
            {
                UserName = adminNationalId,
                Email = adminEmail,
                EmailConfirmed = true,
                PhoneNumber = "01012345678",
                PhoneNumberConfirmed = true
            };

            var createResult = await userManager.CreateAsync(adminUser, adminPassword);
            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                logger.LogError("Failed to seed admin user: {Errors}", errors);
                return;
            }

            // Assign Admin role
            var roleResult = await userManager.AddToRoleAsync(adminUser, adminRole);
            if (!roleResult.Succeeded)
            {
                var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                logger.LogError("Failed to assign Admin role to seed user: {Errors}", errors);
                return;
            }

            // Create the linked Staff record
            var staff = new Staff
            {
                Id = adminUser.Id,
                EmployeeIdentifier = "EMP-ADMIN001",
                FirstName = "أحمد",
                SecondName = "محمد",
                ThirdName = "علي",
                PhoneNumber = "01012345678",
                Address = "شارع بورسعيد، وسط البلد",
                City = "بني سويف",
                DepartmentName = adminRole,
                IsActiveEmployee = true
            };

            dbContext.Staff.Add(staff);
            await dbContext.SaveChangesAsync();

            logger.LogInformation("Admin account seeded successfully. Email: {Email}, Password: {Password}", adminEmail, adminPassword);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the admin account.");
        }
    }
}
