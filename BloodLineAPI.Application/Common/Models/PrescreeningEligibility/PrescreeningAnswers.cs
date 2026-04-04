using System;
using System.Collections.Generic;
using System.Text;

namespace BloodLineAPI.Application.Common.Models.PrescreeningEligibility
{
    public sealed record PrescreeningAnswers(
    bool OkLastDonationIntervalThreeToFourMonths,
    bool OkNoListedSeriousConditions,
    bool OkNotOnBloodThinnersOrCriticalLongTermMeds,
    bool OkNoSurgeryPastSixMonths,
    bool OkNoTattooOrPiercingPastSixMonths,
    bool OkNoBloodTransfusionPastYear,
    bool OkNoFeverInfectionOrSevereCold,
    bool OkNoChronicIllnessAffectingDonation);
}
