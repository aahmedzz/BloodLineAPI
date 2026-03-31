using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BloodLineAPI.Infrastructure.Migrations
{
    public partial class SeedBloodTypesAndChangeBloodTypeId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Add temporary byte columns to all affected tables
            migrationBuilder.Sql("ALTER TABLE BloodTypes ADD NewId TINYINT;");
            migrationBuilder.Sql("ALTER TABLE Donors ADD NewBloodTypeId TINYINT;");
            migrationBuilder.Sql("ALTER TABLE BloodTypeUrgentBloodAppeal ADD NewTargetedBloodTypesId TINYINT;");
            migrationBuilder.Sql("ALTER TABLE BloodBags ADD NewBloodTypeId TINYINT;");

            // 2. Map existing Guids to the new Byte IDs based on your Seed Data
            migrationBuilder.Sql(@"
                UPDATE BloodTypes SET NewId = 1 WHERE BloodGroupName = 'A' AND RhFactor = 'Positive';
                UPDATE BloodTypes SET NewId = 2 WHERE BloodGroupName = 'A' AND RhFactor = 'Negative';
                UPDATE BloodTypes SET NewId = 3 WHERE BloodGroupName = 'B' AND RhFactor = 'Positive';
                UPDATE BloodTypes SET NewId = 4 WHERE BloodGroupName = 'B' AND RhFactor = 'Negative';
                UPDATE BloodTypes SET NewId = 5 WHERE BloodGroupName = 'AB' AND RhFactor = 'Positive';
                UPDATE BloodTypes SET NewId = 6 WHERE BloodGroupName = 'AB' AND RhFactor = 'Negative';
                UPDATE BloodTypes SET NewId = 7 WHERE BloodGroupName = 'O' AND RhFactor = 'Positive';
                UPDATE BloodTypes SET NewId = 8 WHERE BloodGroupName = 'O' AND RhFactor = 'Negative';
            ");

            // 3. Migrate Relational Data: Copy the correct new ID to the foreign key tables
            migrationBuilder.Sql(@"
                UPDATE Donors SET NewBloodTypeId = BloodTypes.NewId
                FROM Donors INNER JOIN BloodTypes ON Donors.BloodTypeId = BloodTypes.Id;

                UPDATE BloodTypeUrgentBloodAppeal SET NewTargetedBloodTypesId = BloodTypes.NewId
                FROM BloodTypeUrgentBloodAppeal INNER JOIN BloodTypes ON BloodTypeUrgentBloodAppeal.TargetedBloodTypesId = BloodTypes.Id;

                UPDATE BloodBags SET NewBloodTypeId = BloodTypes.NewId
                FROM BloodBags INNER JOIN BloodTypes ON BloodBags.BloodTypeId = BloodTypes.Id;
            ");

            // 4. Drop Foreign Key constraints AND Indexes Safely
            migrationBuilder.Sql("ALTER TABLE Donors DROP CONSTRAINT FK_Donors_BloodTypes_BloodTypeId;");
            migrationBuilder.Sql("ALTER TABLE BloodTypeUrgentBloodAppeal DROP CONSTRAINT FK_BloodTypeUrgentBloodAppeal_BloodTypes_TargetedBloodTypesId;");
            migrationBuilder.Sql("ALTER TABLE BloodBags DROP CONSTRAINT FK_BloodBags_BloodTypes_BloodTypeId;");

            // Safely drop indexes only if they exist
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_Donors_BloodTypeId ON Donors;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_BloodTypeUrgentBloodAppeal_TargetedBloodTypesId ON BloodTypeUrgentBloodAppeal;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_BloodBags_BloodTypeId ON BloodBags;");

            // 5. Drop the Primary Key constraints (Including the composite key for the join table)
            migrationBuilder.Sql("ALTER TABLE BloodTypeUrgentBloodAppeal DROP CONSTRAINT PK_BloodTypeUrgentBloodAppeal;");
            migrationBuilder.Sql("ALTER TABLE BloodTypes DROP CONSTRAINT PK_BloodTypes;");

            // 6. Drop the old Guid columns completely
            migrationBuilder.Sql("ALTER TABLE Donors DROP COLUMN BloodTypeId;");
            migrationBuilder.Sql("ALTER TABLE BloodTypeUrgentBloodAppeal DROP COLUMN TargetedBloodTypesId;");
            migrationBuilder.Sql("ALTER TABLE BloodBags DROP COLUMN BloodTypeId;");
            migrationBuilder.Sql("ALTER TABLE BloodTypes DROP COLUMN Id;");

            // 7. Rename the new byte columns to match the original names
            migrationBuilder.Sql("EXEC sp_rename 'BloodTypes.NewId', 'Id', 'COLUMN';");
            migrationBuilder.Sql("EXEC sp_rename 'Donors.NewBloodTypeId', 'BloodTypeId', 'COLUMN';");
            migrationBuilder.Sql("EXEC sp_rename 'BloodTypeUrgentBloodAppeal.NewTargetedBloodTypesId', 'TargetedBloodTypesId', 'COLUMN';");
            migrationBuilder.Sql("EXEC sp_rename 'BloodBags.NewBloodTypeId', 'BloodTypeId', 'COLUMN';");

            // 8. Re-apply NOT NULL constraints where appropriate (Donors.BloodTypeId remains nullable)
            migrationBuilder.Sql("ALTER TABLE BloodTypes ALTER COLUMN Id TINYINT NOT NULL;");
            migrationBuilder.Sql("ALTER TABLE BloodTypeUrgentBloodAppeal ALTER COLUMN TargetedBloodTypesId TINYINT NOT NULL;");
            migrationBuilder.Sql("ALTER TABLE BloodBags ALTER COLUMN BloodTypeId TINYINT NOT NULL;");

            // 9. Recreate Primary/Foreign Key Constraints AND Indexes
            migrationBuilder.Sql("ALTER TABLE BloodTypes ADD CONSTRAINT PK_BloodTypes PRIMARY KEY (Id);");

            // Recreate the composite Primary Key using the correct column names
            migrationBuilder.Sql("ALTER TABLE BloodTypeUrgentBloodAppeal ADD CONSTRAINT PK_BloodTypeUrgentBloodAppeal PRIMARY KEY (TargetedBloodTypesId, UrgentBloodAppealsId);");

            migrationBuilder.Sql("ALTER TABLE Donors ADD CONSTRAINT FK_Donors_BloodTypes_BloodTypeId FOREIGN KEY (BloodTypeId) REFERENCES BloodTypes(Id);");
            migrationBuilder.Sql("ALTER TABLE BloodTypeUrgentBloodAppeal ADD CONSTRAINT FK_BloodTypeUrgentBloodAppeal_BloodTypes_TargetedBloodTypesId FOREIGN KEY (TargetedBloodTypesId) REFERENCES BloodTypes(Id);");
            migrationBuilder.Sql("ALTER TABLE BloodBags ADD CONSTRAINT FK_BloodBags_BloodTypes_BloodTypeId FOREIGN KEY (BloodTypeId) REFERENCES BloodTypes(Id);");

            // Recreate the indexes for performance
            migrationBuilder.Sql("CREATE INDEX IX_Donors_BloodTypeId ON Donors (BloodTypeId);");
            migrationBuilder.Sql("CREATE INDEX IX_BloodTypeUrgentBloodAppeal_TargetedBloodTypesId ON BloodTypeUrgentBloodAppeal (TargetedBloodTypesId);");
            migrationBuilder.Sql("CREATE INDEX IX_BloodBags_BloodTypeId ON BloodBags (BloodTypeId);");

            // 10. Safely insert seed data ONLY if it doesn't already exist from step 2
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM BloodTypes WHERE Id = 1) INSERT INTO BloodTypes (Id, BloodGroupName, RhFactor) VALUES (1, 'A', 'Positive');
                IF NOT EXISTS (SELECT 1 FROM BloodTypes WHERE Id = 2) INSERT INTO BloodTypes (Id, BloodGroupName, RhFactor) VALUES (2, 'A', 'Negative');
                IF NOT EXISTS (SELECT 1 FROM BloodTypes WHERE Id = 3) INSERT INTO BloodTypes (Id, BloodGroupName, RhFactor) VALUES (3, 'B', 'Positive');
                IF NOT EXISTS (SELECT 1 FROM BloodTypes WHERE Id = 4) INSERT INTO BloodTypes (Id, BloodGroupName, RhFactor) VALUES (4, 'B', 'Negative');
                IF NOT EXISTS (SELECT 1 FROM BloodTypes WHERE Id = 5) INSERT INTO BloodTypes (Id, BloodGroupName, RhFactor) VALUES (5, 'AB', 'Positive');
                IF NOT EXISTS (SELECT 1 FROM BloodTypes WHERE Id = 6) INSERT INTO BloodTypes (Id, BloodGroupName, RhFactor) VALUES (6, 'AB', 'Negative');
                IF NOT EXISTS (SELECT 1 FROM BloodTypes WHERE Id = 7) INSERT INTO BloodTypes (Id, BloodGroupName, RhFactor) VALUES (7, 'O', 'Positive');
                IF NOT EXISTS (SELECT 1 FROM BloodTypes WHERE Id = 8) INSERT INTO BloodTypes (Id, BloodGroupName, RhFactor) VALUES (8, 'O', 'Negative');
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Rolling back this kind of destructive, data-translating change perfectly is exceptionally dangerous. 
            // We throw a NotSupportedException to prevent accidental data loss from running "Update-Database" downwards.
            throw new NotSupportedException("Rolling back from byte back to Guid requires manually wiping the tables or writing a reverse manual mapping script.");
        }
    }
}
