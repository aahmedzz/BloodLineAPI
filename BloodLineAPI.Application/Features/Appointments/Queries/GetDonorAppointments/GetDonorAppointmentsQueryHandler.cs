using BloodLineAPI.Application.Common.Interfaces;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Application.Common.Models.MobileAppointment;
using BloodLineAPI.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;


namespace BloodLineAPI.Application.Features.Appointments.Queries.DonorAppointments
{
    public sealed class GetDonorAppointmentsQueryHandler(IApplicationDbContext db)
            : IRequestHandler<GetDonorAppointmentsQuery, Result<IReadOnlyList<AppointmentListItem>>>
        {
            public async Task<Result<IReadOnlyList<AppointmentListItem>>> Handle(
                GetDonorAppointmentsQuery request,
                CancellationToken cancellationToken)
            {
                var utcNow = DateTime.UtcNow;
                var today = utcNow.Date;
                var nowTime = utcNow.TimeOfDay;
                var wantUpcoming = request.Status.Equals("upcoming", StringComparison.OrdinalIgnoreCase);
                var query = db.DonationAppointments
                    .AsNoTracking()
                    .Where(a => a.DonorId == request.DonorId)
                    .AsQueryable();
                if (wantUpcoming)
                {
                    query = query.Where(a =>
                        a.Status != AppointmentStatus.Cancelled && a.Status != AppointmentStatus.Completed && (a.ScheduledDate.Date > today|| (a.ScheduledDate.Date == today && a.BookTime >= nowTime)));
                }
                else
                {
                    query = query.Where(a =>
                        a.ScheduledDate.Date < today|| a.Status == AppointmentStatus.Completed|| a.Status == AppointmentStatus.Cancelled|| (a.ScheduledDate.Date == today && a.BookTime < nowTime));
                }
                var list = await query
                    .OrderByDescending(a => a.ScheduledDate)
                    .ThenByDescending(a => a.BookTime)
                    .Select(a => new AppointmentListItem(
                        a.Id,
                        a.DonationType.ToString(),
                        a.ScheduledDate,
                        a.BookTime,
                        a.Status,
                        a.DonationCenterId,
                        a.DonationCenter.Name,
                        a.DonationCenter.Location))
                    .ToListAsync(cancellationToken);
                return Result<IReadOnlyList<AppointmentListItem>>.Success(list);
            }
        }
    }

