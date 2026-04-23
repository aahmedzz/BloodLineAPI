using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BloodLineAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedBeniSuefMainBranch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[CenterExclusions]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [CenterExclusions] (
                        [Id] uniqueidentifier NOT NULL,
                        [CenterId] uniqueidentifier NOT NULL,
                        [Date] datetime2 NOT NULL,
                        [IsClosed] bit NOT NULL,
                        [SpecialOpeningTime] time NULL,
                        [SpecialClosingTime] time NULL,
                        [Reason] nvarchar(300) NOT NULL,
                        CONSTRAINT [PK_CenterExclusions] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_CenterExclusions_DonationCenters_CenterId] FOREIGN KEY ([CenterId]) REFERENCES [DonationCenters] ([Id]) ON DELETE CASCADE
                    );

                    CREATE UNIQUE INDEX [IX_CenterExclusions_CenterId_Date] ON [CenterExclusions] ([CenterId], [Date]);
                END
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[OpeningHours]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [OpeningHours] (
                        [Id] uniqueidentifier NOT NULL,
                        [CenterId] uniqueidentifier NOT NULL,
                        [DayOfWeek] int NOT NULL,
                        [IsClosed] bit NOT NULL,
                        [OpeningTime] time NOT NULL,
                        [ClosingTime] time NOT NULL,
                        [MaxDonorsPerSlot] int NULL,
                        CONSTRAINT [PK_OpeningHours] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_OpeningHours_DonationCenters_CenterId] FOREIGN KEY ([CenterId]) REFERENCES [DonationCenters] ([Id]) ON DELETE CASCADE
                    );

                    CREATE UNIQUE INDEX [IX_OpeningHours_CenterId_DayOfWeek] ON [OpeningHours] ([CenterId], [DayOfWeek]);
                END
                """);

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "HealthPreScreenings");

            migrationBuilder.InsertData(
                table: "DonationCenters",
                columns: new[] { "Id", "AddressDetails", "CenterType", "CreatedAt", "CreatedBy", "DescriptionText", "EndDate", "EndTime", "LastModifiedAt", "LastModifiedBy", "Latitude", "Location", "Longitude", "MaxDonorsPerSlot", "Name", "SlotDurationMinutes", "StartDate", "StartTime", "Status" },
                values: new object[] { new Guid("b5b4d5b7-eaf8-4a92-8b0a-2fc73f6cc3d1"), "Beni Suef Main Branch", "MainBranch", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Main branch in Beni Suef.", null, new TimeSpan(0, 21, 0, 0, 0), null, null, 29.042005899999999, "Beni Suef", 31.118414000000001, 10, "Beni Suef Main Branch", 15, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 7, 0, 0, 0), "Active" });

            migrationBuilder.InsertData(
                table: "OpeningHours",
                columns: new[] { "Id", "CenterId", "ClosingTime", "DayOfWeek", "IsClosed", "MaxDonorsPerSlot", "OpeningTime" },
                values: new object[,]
                {
                    { new Guid("0b14eefa-72d9-4f83-aad4-6d4e90ca8e10"), new Guid("b5b4d5b7-eaf8-4a92-8b0a-2fc73f6cc3d1"), new TimeSpan(0, 21, 0, 0, 0), 0, false, null, new TimeSpan(0, 7, 0, 0, 0) },
                    { new Guid("0eb9fceb-4f4f-48b5-80c5-63f9da73b763"), new Guid("b5b4d5b7-eaf8-4a92-8b0a-2fc73f6cc3d1"), new TimeSpan(0, 21, 0, 0, 0), 3, false, null, new TimeSpan(0, 7, 0, 0, 0) },
                    { new Guid("2dc3fd35-a567-437f-a53f-c833fddfa0f7"), new Guid("b5b4d5b7-eaf8-4a92-8b0a-2fc73f6cc3d1"), new TimeSpan(0, 21, 0, 0, 0), 5, false, null, new TimeSpan(0, 7, 0, 0, 0) },
                    { new Guid("3f9ef858-2962-47d5-95f9-c7e3f2eaeb58"), new Guid("b5b4d5b7-eaf8-4a92-8b0a-2fc73f6cc3d1"), new TimeSpan(0, 21, 0, 0, 0), 6, false, null, new TimeSpan(0, 7, 0, 0, 0) },
                    { new Guid("7f98f6f7-9556-4fbb-a457-e15457d0656f"), new Guid("b5b4d5b7-eaf8-4a92-8b0a-2fc73f6cc3d1"), new TimeSpan(0, 21, 0, 0, 0), 1, false, null, new TimeSpan(0, 7, 0, 0, 0) },
                    { new Guid("9da3ab38-757b-4b96-80a5-3e84f917f4fe"), new Guid("b5b4d5b7-eaf8-4a92-8b0a-2fc73f6cc3d1"), new TimeSpan(0, 21, 0, 0, 0), 2, false, null, new TimeSpan(0, 7, 0, 0, 0) },
                    { new Guid("f7ff2d1c-b95a-4f08-806d-9bf8f5f1a036"), new Guid("b5b4d5b7-eaf8-4a92-8b0a-2fc73f6cc3d1"), new TimeSpan(0, 21, 0, 0, 0), 4, false, null, new TimeSpan(0, 7, 0, 0, 0) }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "OpeningHours",
                keyColumn: "Id",
                keyValue: new Guid("0b14eefa-72d9-4f83-aad4-6d4e90ca8e10"));

            migrationBuilder.DeleteData(
                table: "OpeningHours",
                keyColumn: "Id",
                keyValue: new Guid("0eb9fceb-4f4f-48b5-80c5-63f9da73b763"));

            migrationBuilder.DeleteData(
                table: "OpeningHours",
                keyColumn: "Id",
                keyValue: new Guid("2dc3fd35-a567-437f-a53f-c833fddfa0f7"));

            migrationBuilder.DeleteData(
                table: "OpeningHours",
                keyColumn: "Id",
                keyValue: new Guid("3f9ef858-2962-47d5-95f9-c7e3f2eaeb58"));

            migrationBuilder.DeleteData(
                table: "OpeningHours",
                keyColumn: "Id",
                keyValue: new Guid("7f98f6f7-9556-4fbb-a457-e15457d0656f"));

            migrationBuilder.DeleteData(
                table: "OpeningHours",
                keyColumn: "Id",
                keyValue: new Guid("9da3ab38-757b-4b96-80a5-3e84f917f4fe"));

            migrationBuilder.DeleteData(
                table: "OpeningHours",
                keyColumn: "Id",
                keyValue: new Guid("f7ff2d1c-b95a-4f08-806d-9bf8f5f1a036"));

            migrationBuilder.DeleteData(
                table: "DonationCenters",
                keyColumn: "Id",
                keyValue: new Guid("b5b4d5b7-eaf8-4a92-8b0a-2fc73f6cc3d1"));

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                table: "HealthPreScreenings",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }
    }
}
