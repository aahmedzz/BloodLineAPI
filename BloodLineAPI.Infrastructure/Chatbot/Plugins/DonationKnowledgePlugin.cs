using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace BloodLineAPI.Infrastructure.Chatbot.Plugins;

/// <summary>
/// A zero-cost, static-knowledge plugin that provides authoritative answers
/// about blood donation types, eligibility criteria, preparation tips,
/// post-donation care, the donation process, and blood group compatibility.
/// No database queries are made — all content is hardcoded medical/educational information.
/// </summary>
public class DonationKnowledgePlugin
{
    [KernelFunction, Description("Explains the different types of blood donation available (Whole Blood, Plasma, Platelets). Useful when the user asks what types of donation exist or the difference between them.")]
    public Task<string> GetDonationTypesInfoAsync()
    {
        const string info = """
            Types of Blood Donation:

            1. Whole Blood Donation:
               - The most common type of donation.
               - About 450–500 ml of blood is collected.
               - Takes approximately 10–15 minutes for the actual draw.
               - Can be donated every 3–4 months (males) or every 4 months (females).
               - Used for: trauma patients, surgeries, and anemia treatment.

            2. Plasma Donation (Plasmapheresis):
               - Only the plasma (liquid part of blood) is collected; red cells are returned to the donor.
               - Takes approximately 45–60 minutes.
               - Can be donated more frequently than whole blood (every 2–4 weeks).
               - Used for: burn patients, clotting disorders, and immune deficiencies.

            3. Platelet Donation (Plateletpheresis):
               - Only platelets are collected using a special machine; other components are returned.
               - Takes approximately 1.5–2.5 hours.
               - Can be donated every 2 weeks, up to 24 times per year.
               - Used for: cancer patients undergoing chemotherapy, organ transplants, and major surgeries.
            """;

        return Task.FromResult(info);
    }

    [KernelFunction, Description("Provides the blood group compatibility chart showing which blood types can donate to and receive from which other types. Useful when the user asks about blood type compatibility, who can donate to whom, or universal donors/recipients.")]
    public Task<string> GetBloodGroupCompatibilityAsync()
    {
        const string info = """
            Blood Group Compatibility Chart:

            Blood Type  | Can Donate To           | Can Receive From
            ------------|-------------------------|---------------------------
            O-          | All types (Universal)   | O- only
            O+          | O+, A+, B+, AB+         | O-, O+
            A-          | A-, A+, AB-, AB+         | O-, A-
            A+          | A+, AB+                 | O-, O+, A-, A+
            B-          | B-, B+, AB-, AB+         | O-, B-
            B+          | B+, AB+                 | O-, O+, B-, B+
            AB-         | AB-, AB+                | O-, A-, B-, AB-
            AB+         | AB+ only                | All types (Universal)

            Key Facts:
            - O- is the Universal Donor (red blood cells can go to anyone).
            - AB+ is the Universal Recipient (can receive red blood cells from anyone).
            - AB is the Universal Plasma Donor.
            - O is the Universal Plasma Recipient.
            - In emergencies, O- blood is given when the patient's type is unknown.
            - Rh-negative blood can be given to Rh-positive patients, but NOT the reverse.
            """;

        return Task.FromResult(info);
    }

    [KernelFunction, Description("Provides tips and guidelines on how to prepare before donating blood. Useful when the user asks what to do before donation, what to eat or drink, or how to get ready.")]
    public Task<string> GetPreDonationTipsAsync()
    {
        const string info = """
            Before Donating Blood — Preparation Tips:

            ✅ DO:
            - Drink plenty of water (at least 2–3 glasses) in the hours before donation.
            - Eat a healthy, iron-rich meal 2–3 hours before (e.g., red meat, spinach, beans, lentils).
            - Get a good night's sleep (at least 7–8 hours) the night before.
            - Wear comfortable clothing with sleeves that can be rolled up easily.
            - Bring a valid ID (national ID card).
            - Relax and stay calm — the process is safe and quick.

            ❌ DON'T:
            - Do not donate on an empty stomach.
            - Avoid fatty or fried foods before donation (they can affect blood test results).
            - Avoid alcohol for at least 24 hours before donating.
            - Avoid strenuous exercise or heavy lifting on the day of donation.
            - Do not smoke for at least 1 hour before donating.
            - Do not take aspirin or blood thinners within 48 hours of donation (consult your doctor).
            """;

        return Task.FromResult(info);
    }

    [KernelFunction, Description("Provides post-donation care instructions and recovery tips after giving blood. Useful when the user asks what to do after donating, recovery time, or side effects.")]
    public Task<string> GetPostDonationCareAsync()
    {
        const string info = """
            After Donating Blood — Recovery & Care Tips:

            Immediately After:
            - Rest at the donation center for 10–15 minutes.
            - Drink the juice or water offered to you — stay hydrated.
            - Keep the bandage on for at least 4–5 hours.
            - If you feel dizzy, sit or lie down with your legs elevated until it passes.

            For the Next 24 Hours:
            - Drink extra fluids (water, juice) — at least 4 extra glasses.
            - Eat iron-rich foods (meat, eggs, leafy greens, dried fruits) to replenish stores.
            - Avoid heavy exercise, lifting weights, or strenuous physical activity.
            - Avoid standing for long periods.
            - Do not smoke for at least 2 hours after donation.
            - Avoid alcohol for 24 hours.

            Normal Side Effects (usually resolve within hours):
            - Mild dizziness or lightheadedness
            - Slight bruising at the needle site
            - Minor fatigue

            ⚠️ Seek medical attention if you experience:
            - Persistent bleeding from the needle site
            - Prolonged dizziness or fainting
            - Numbness or tingling in the arm
            - Signs of infection (redness, swelling, warmth at the site)

            Your body replaces the donated plasma within 24 hours and red blood cells within 4–6 weeks.
            """;

        return Task.FromResult(info);
    }

    [KernelFunction, Description("Explains the step-by-step blood donation process from registration to post-donation. Useful when the user asks what happens during donation, the donation procedure, or what to expect.")]
    public Task<string> GetDonationProcessStepsAsync()
    {
        const string info = """
            The Blood Donation Process — Step by Step:

            Step 1: Registration
            - Present your ID and fill out a brief health questionnaire.
            - Your basic information is recorded.

            Step 2: Health Pre-Screening
            - A staff member checks your vital signs: blood pressure, pulse, temperature, and hemoglobin level.
            - You will be asked about your medical history, recent travel, and medications.
            - This determines if you are eligible to donate today.

            Step 3: The Donation
            - You will sit or lie on a comfortable donation chair.
            - The inside of your elbow is cleaned with antiseptic.
            - A sterile, single-use needle is inserted — you may feel a brief pinch.
            - About 450–500 ml of blood is collected into a sterile bag.
            - The actual blood draw takes only 8–15 minutes.
            - You can relax, listen to music, or chat during the process.

            Step 4: Post-Donation Rest
            - The needle is removed and a bandage is applied.
            - You rest for 10–15 minutes at the refreshment area.
            - You are given fluids and a light snack.

            Step 5: Blood Processing
            - Your donated blood is tested for infectious diseases (HIV, Hepatitis B & C, Syphilis, etc.).
            - It is then separated into components (red cells, plasma, platelets) to help multiple patients.

            Total time at the center: approximately 30–45 minutes.
            """;

        return Task.FromResult(info);
    }

    [KernelFunction, Description("Provides the general eligibility criteria and requirements for donating blood. Useful when the user asks if they can donate, who is eligible, age or weight requirements, or medical conditions that prevent donation.")]
    public Task<string> GetEligibilityCriteriaAsync()
    {
        const string info = """
            Blood Donation Eligibility Criteria:

            ✅ You CAN donate if:
            - You are between 18 and 65 years old.
            - You weigh at least 50 kg (110 lbs).
            - Your hemoglobin level is adequate (≥12.5 g/dL for women, ≥13.5 g/dL for men).
            - You are in good general health and feeling well on the day of donation.
            - It has been at least 3–4 months since your last whole blood donation.

            ❌ You CANNOT donate if:
            - You have a chronic blood disease (e.g., sickle cell disease, hemophilia).
            - You are currently taking blood thinners or critical medication.
            - You had surgery in the past 6 months.
            - You got a tattoo or piercing in the past 6 months.
            - You had a dental procedure in the past week.
            - You currently have a fever, active infection, or severe cold.
            - You have a chronic illness that affects blood donation (e.g., uncontrolled diabetes, heart disease).
            - You are pregnant or breastfeeding.
            - You have tested positive for HIV, Hepatitis B, or Hepatitis C.

            Temporary Deferrals:
            - Recent vaccination: wait 2–4 weeks depending on the vaccine type.
            - Travel to malaria-endemic areas: wait 3–12 months.
            - Minor illness (cold/flu): wait until fully recovered.

            When in doubt, our medical staff at the center will evaluate your eligibility.
            """;

        return Task.FromResult(info);
    }
}
