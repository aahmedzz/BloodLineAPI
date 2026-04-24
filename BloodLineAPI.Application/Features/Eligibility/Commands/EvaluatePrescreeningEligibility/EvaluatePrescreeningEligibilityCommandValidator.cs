using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace BloodLineAPI.Application.Features.Eligibility.Commands.EvaluatePrescreeningEligibility
{

    public sealed class EvaluatePrescreeningEligibilityCommandValidator
        : AbstractValidator<EvaluatePrescreeningEligibilityCommand>
    {
        public EvaluatePrescreeningEligibilityCommandValidator()
        {
            RuleFor(x => x.DonorId).NotEmpty();
            RuleFor(x => x.Answers).NotNull();
        }
    }
}
