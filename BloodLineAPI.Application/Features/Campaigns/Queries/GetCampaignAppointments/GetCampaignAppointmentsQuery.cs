using System.Collections.Generic;
using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Application.Features.Campaigns.Dtos;
using MediatR;

namespace BloodLineAPI.Application.Features.Campaigns.Queries.GetCampaignAppointments;

public record GetCampaignAppointmentsQuery(string Id) : IRequest<Result<IReadOnlyList<CampaignAppointmentSlotDto>>>;
