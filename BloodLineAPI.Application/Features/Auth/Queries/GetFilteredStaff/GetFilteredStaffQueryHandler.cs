using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BloodLineAPI.Application.Features.Auth.Queries.GetFilteredStaff;

public sealed class GetFilteredStaffQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetFilteredStaffQuery, Result<PaginatedStaffResult>>
{
    public async Task<Result<PaginatedStaffResult>> Handle(
        GetFilteredStaffQuery request,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Staff
            .Include(s => s.User)
                .ThenInclude(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
            .Where(s => !s.User.IsDeleted && !s.User.UserRoles.Any(ur => ur.Role.Name == "Admin"));

        // 1. Apply Search Filter
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(s =>
                s.FirstName.Contains(search) ||
                s.SecondName.Contains(search) ||
                s.ThirdName.Contains(search) ||
                (s.FourthName != null && s.FourthName.Contains(search)) ||
                (s.FirstName + " " + s.SecondName + " " + s.ThirdName + " " + (s.FourthName ?? "")).Contains(search) ||
                (s.User.Email != null && s.User.Email.Contains(search)) ||
                (s.PhoneNumber != null && s.PhoneNumber.Contains(search)));
        }

        // 2. Apply Role Filter (map frontend format to database format)
        if (!string.IsNullOrWhiteSpace(request.Role))
        {
            var dbRole = request.Role.Trim().ToLowerInvariant() switch
            {
                "admin" => "Admin",
                "doctor" => "Doctor",
                "lab" => "LabDoctor",
                "inventory" => "InventoryManager",
                _ => request.Role.Trim()
            };

            query = query.Where(s => s.User.UserRoles.Any(ur => ur.Role.Name == dbRole));
        }

        // 3. Apply Status Filter
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (request.Status.Equals("active", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(s => s.IsActiveEmployee);
            }
            else if (request.Status.Equals("inactive", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(s => !s.IsActiveEmployee);
            }
        }

        // 4. Calculate pagination bounds
        var total = await query.CountAsync(cancellationToken);
        var page = request.Page < 1 ? 1 : request.Page;
        var limit = request.Limit < 1 ? 10 : request.Limit;

        // 5. Materialize and paginated staff records
        var staffList = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync(cancellationToken);

        // 6. Map to DTOs in memory
        var mappedData = new List<StaffDto>();
        foreach (var s in staffList)
        {
            var userRoles = s.User.UserRoles.Select(ur => ur.Role.Name).ToList();
            var primaryRole = userRoles.FirstOrDefault() ?? string.Empty;
            var mappedRole = primaryRole switch
            {
                "Admin" => "admin",
                "Doctor" => "doctor",
                "LabDoctor" => "lab",
                "InventoryManager" => "inventory",
                _ => primaryRole.ToLowerInvariant()
            };

            mappedData.Add(new StaffDto(
                Id: s.Id,
                Name: s.FullName,
                Email: s.User.Email ?? string.Empty,
                Role: mappedRole,
                NationalId: s.User.UserName ?? string.Empty,
                Phone: s.PhoneNumber ?? string.Empty,
                Address: s.Address ?? string.Empty,
                City: s.City ?? string.Empty,
                Status: s.IsActiveEmployee ? "active" : "inactive",
                CreatedAt: s.CreatedAt.ToString("yyyy-MM-dd")
            ));
        }

        var result = new PaginatedStaffResult(mappedData, total, page, limit);
        return Result<PaginatedStaffResult>.Success(result);
    }
}
