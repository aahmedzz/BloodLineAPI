using BloodLineAPI.Application.Common.Models.PrescreeningEligibility;
using BloodLineAPI.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace BloodLineAPI.Application.Common.Extentions
{
    public static class DonorEligibilityExtensions
    {
        public static PrescreeningEligibilityResponse IsEligibleForDonation(
            this Donor donor,
            PrescreeningAnswers answers)
        {
            _ = donor;
            if (!answers.OkLastDonationIntervalThreeToFourMonths)
                return new PrescreeningEligibilityResponse(false, "INTERVAL_TOO_SHORT",
                    "Has it been 3–4 months since your last blood donation? Based on your answer, you are not eligible yet.");
            if (!answers.OkNoListedSeriousConditions)
                return new PrescreeningEligibilityResponse(false, "HIGH_RISK_CONDITIONS",
                    "Based on your answers, you may not be eligible due to a listed medical condition.");
            if (!answers.OkNotOnBloodThinnersOrCriticalLongTermMeds)
                return new PrescreeningEligibilityResponse(false, "BLOOD_THINNERS_OR_CRITICAL_MEDS",
                    "Based on your answer about medications, you are not eligible right now.");
            if (!answers.OkNoSurgeryPastSixMonths)
                return new PrescreeningEligibilityResponse(false, "RECENT_SURGERY",
                    "Based on your answer about recent surgery, you are not eligible right now.");
            if (!answers.OkNoTattooOrPiercingPastSixMonths)
                return new PrescreeningEligibilityResponse(false, "RECENT_TATTOO_OR_PIERCING",
                    "Based on your answer about tattoo or piercing, you are not eligible right now.");
            if (!answers.OkNoBloodTransfusionPastYear)
                return new PrescreeningEligibilityResponse(false, "RECENT_TRANSFUSION",
                    "Based on your answer about blood transfusion, you are not eligible right now.");
            if (!answers.OkNoFeverInfectionOrSevereCold)
                return new PrescreeningEligibilityResponse(false, "ACUTE_ILLNESS",
                    "Based on your answer about fever or infection, you are not eligible right now.");
            if (!answers.OkNoChronicIllnessAffectingDonation)
                return new PrescreeningEligibilityResponse(false, "CHRONIC_ILLNESS",
                    "Based on your answer about chronic illness, you are not eligible right now.");
            return new PrescreeningEligibilityResponse(true, null, null);
        }
    }
}
