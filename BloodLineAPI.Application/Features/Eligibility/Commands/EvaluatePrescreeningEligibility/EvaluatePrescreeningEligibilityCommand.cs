using BloodLineAPI.Application.Common.Models;
using BloodLineAPI.Application.Common.Models.PrescreeningEligibility;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace BloodLineAPI.Application.Features.Eligibility.Commands.EvaluatePrescreeningEligibility
{
    public sealed record EvaluatePrescreeningEligibilityCommand(
    Guid DonorId,
    PrescreeningAnswers Answers)
    : IRequest<Result<PrescreeningEligibilityResponse>>;
}
