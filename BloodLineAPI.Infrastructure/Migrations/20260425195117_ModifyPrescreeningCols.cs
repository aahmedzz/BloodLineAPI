using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BloodLineAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ModifyPrescreeningCols : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsTakingMedication",
                table: "HealthPreScreenings",
                newName: "IsTakingBloodThinnersOrCriticalMedication");

            migrationBuilder.RenameColumn(
                name: "IsPregnantOrBreastfeeding",
                table: "HealthPreScreenings",
                newName: "HasRecentTattooOrPiercingInPast6Months");

            migrationBuilder.RenameColumn(
                name: "HasRecentVaccination",
                table: "HealthPreScreenings",
                newName: "HasRecentSurgeryInPast6Months");

            migrationBuilder.RenameColumn(
                name: "HasRecentTattooOrPiercing",
                table: "HealthPreScreenings",
                newName: "HasReceivedBloodTransfusionWithinPastYear");

            migrationBuilder.RenameColumn(
                name: "HasRecentSurgery",
                table: "HealthPreScreenings",
                newName: "HasCurrentFeverInfectionOrSevereCold");

            migrationBuilder.RenameColumn(
                name: "HasRecentInfection",
                table: "HealthPreScreenings",
                newName: "HasChronicIllnessAffectingBloodDonation");

            migrationBuilder.RenameColumn(
                name: "HasChronicDisease",
                table: "HealthPreScreenings",
                newName: "HasBeenThreeToFourMonthsSinceLastDonation");

            migrationBuilder.RenameColumn(
                name: "HasBleedingDisorder",
                table: "HealthPreScreenings",
                newName: "HasAnyDisqualifyingCondition");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsTakingBloodThinnersOrCriticalMedication",
                table: "HealthPreScreenings",
                newName: "IsTakingMedication");

            migrationBuilder.RenameColumn(
                name: "HasRecentTattooOrPiercingInPast6Months",
                table: "HealthPreScreenings",
                newName: "IsPregnantOrBreastfeeding");

            migrationBuilder.RenameColumn(
                name: "HasRecentSurgeryInPast6Months",
                table: "HealthPreScreenings",
                newName: "HasRecentVaccination");

            migrationBuilder.RenameColumn(
                name: "HasReceivedBloodTransfusionWithinPastYear",
                table: "HealthPreScreenings",
                newName: "HasRecentTattooOrPiercing");

            migrationBuilder.RenameColumn(
                name: "HasCurrentFeverInfectionOrSevereCold",
                table: "HealthPreScreenings",
                newName: "HasRecentSurgery");

            migrationBuilder.RenameColumn(
                name: "HasChronicIllnessAffectingBloodDonation",
                table: "HealthPreScreenings",
                newName: "HasRecentInfection");

            migrationBuilder.RenameColumn(
                name: "HasBeenThreeToFourMonthsSinceLastDonation",
                table: "HealthPreScreenings",
                newName: "HasChronicDisease");

            migrationBuilder.RenameColumn(
                name: "HasAnyDisqualifyingCondition",
                table: "HealthPreScreenings",
                newName: "HasBleedingDisorder");
        }
    }
}
