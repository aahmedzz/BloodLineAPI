using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Features.Appointments.Dtos;
using BloodLineAPI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BloodLineAPI.Application.Features.Appointments.Queries.GetDonationCenters;

public sealed class GetDonationCentersQueryHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<GetDonationCentersQuery, IReadOnlyList<DonationCenterDto>>
{
    public async Task<IReadOnlyList<DonationCenterDto>> Handle(GetDonationCentersQuery request, CancellationToken cancellationToken)
    {
        // 1. Resolve Donor Coordinates and update DB if new values are provided
        var donor = await dbContext.Donors
            .FirstOrDefaultAsync(d => d.Id == request.DonorId, cancellationToken);

        if (request.Latitude.HasValue && request.Longitude.HasValue)
        {
            if (donor != null && (donor.Latitude != request.Latitude || donor.Longitude != request.Longitude))
            {
                donor.Latitude = request.Latitude;
                donor.Longitude = request.Longitude;
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        double? donorLat = request.Latitude ?? donor?.Latitude;
        double? donorLng = request.Longitude ?? donor?.Longitude;

        // 2. Fetch Average Ratings aggregated in the Database
        var averageRatings = await dbContext.DonationRatings
            .GroupBy(r => r.DonationAppointment.DonationCenterId)
            .Select(g => new { CenterId = g.Key, Avg = g.Average(r => r.StarScore) })
            .ToDictionaryAsync(x => x.CenterId, x => (double?)x.Avg, cancellationToken);

        // 3. Resolve base query for active centers
        var query = dbContext.DonationCenters
            .Include(c => c.OpeningHours)
            .Include(c => c.CenterExclusions)
            .AsNoTracking()
            .Where(c => c.Status == CenterStatus.Active)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.Trim();
            query = query.Where(c => c.Name.Contains(search) || c.Location.Contains(search));
        }

        var centers = await query.ToListAsync(cancellationToken);

        // 4. Resolve current week/day variables for slot & operating calculation
        var todayDate = dateTimeProvider.CurrentLocalDate.ToDateTime(TimeOnly.MinValue);
        var nowTime = dateTimeProvider.CurrentLocalTimeOfDay;

        // 5. Query all booked appointments today to perform in-memory slot availability checks
        var bookedAppointmentsCountToday = await dbContext.DonationAppointments
            .Where(a => a.ScheduledDate == todayDate && a.Status != AppointmentStatus.Cancelled)
            .GroupBy(a => new { a.DonationCenterId, a.StartTime })
            .Select(g => new { g.Key.DonationCenterId, g.Key.StartTime, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var bookingsMap = bookedAppointmentsCountToday
            .GroupBy(b => b.DonationCenterId)
            .ToDictionary(g => g.Key, g => g.ToDictionary(x => x.StartTime, x => x.Count));

        // 6. Map and project to DTOs
        var results = new List<DonationCenterDto>();

        foreach (var center in centers)
        {
            // Available slots today calculation
            int availableSlotsToday = 0;
            if (center.IsOperatingOn(todayDate))
            {
                var slots = center.GenerateTimeSlotsForDate(
                    todayDate, 
                    center.CenterExclusions.ToList(), 
                    center.OpeningHours.ToList());

                var remainingSlots = slots.Where(s => s.Start >= nowTime).ToList();
                var centerBookings = bookingsMap.GetValueOrDefault(center.Id, []);

                foreach (var slot in remainingSlots)
                {
                    var bookedCount = centerBookings.GetValueOrDefault(slot.Start, 0);
                    availableSlotsToday += Math.Max(0, slot.MaxPerSlot - bookedCount);
                }
            }

            // Is Open Now calculation
            var operatingHours = center.ResolveOperatingHours(
                todayDate, 
                center.CenterExclusions.ToList(), 
                center.OpeningHours.ToList());

            bool isOpenNow = false;
            if (operatingHours.HasValue)
            {
                var (open, close, _) = operatingHours.Value;
                isOpenNow = nowTime >= open && nowTime <= close;
            }

            // Distance calculation
            double? distanceKm = null;
            if (donorLat.HasValue && donorLng.HasValue)
            {
                distanceKm = CalculateDistance(donorLat.Value, donorLng.Value, center.Latitude, center.Longitude);
            }

            averageRatings.TryGetValue(center.Id, out double? averageRating);

            var availableDonationTypes = center.SupportedDonationTypes
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(static type => type switch
                {
                    "WholeBlood" => "whole blood",
                    "Platelets" => "platelets",
                    "Plasma" => "plasma",
                    _ => type
                })
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            results.Add(new DonationCenterDto(
                center.Id,
                center.Name,
                center.Location,
                center.AddressDetails,
                center.Latitude,
                center.Longitude,
                center.CenterType.ToString(),
                center.Status.ToString(),
                $"{center.StartTime:hh\\:mm} - {center.EndTime:hh\\:mm}",
                availableDonationTypes,
                availableSlotsToday,
                averageRating,
                distanceKm,
                isOpenNow));
        }

        // Return ordered by distance (if location resolved) or center name
        if (donorLat.HasValue && donorLng.HasValue)
        {
            return results.OrderBy(r => r.DistanceKm).ToList();
        }

        return results.OrderBy(r => r.Name).ToList();
    }

    private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        var r = 6371d; // Earth radius in km
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return r * c;
    }

    private static double ToRadians(double val) => (Math.PI / 180) * val;
}
