using System;
using System.Collections.Generic;
using System.Text;

namespace BloodLineAPI.Application.Common.Models.PrescreeningEligibility
{
    public sealed record PrescreeningEligibilityResponse(
    bool Eligible,
    string? ReasonCode,
    string? Message);
}
