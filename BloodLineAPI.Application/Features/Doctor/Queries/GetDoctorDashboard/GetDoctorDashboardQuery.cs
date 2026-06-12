using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Application.Features.Doctor.Dtos;
using MediatR;

namespace BloodLineAPI.Application.Features.Doctor.Queries.GetDoctorDashboard;

public sealed record GetDoctorDashboardQuery : IRequest<Result<DoctorDashboardDto>>;
