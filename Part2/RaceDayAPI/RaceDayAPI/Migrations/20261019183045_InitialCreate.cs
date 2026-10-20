using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaceDayAPI.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstName = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                    table.CheckConstraint("CK_Users_Role", "[Role] IN (N'Organiser', N'Participant')");
                });

            migrationBuilder.CreateTable(
                name: "Events",
                columns: table => new
                {
                    EventId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganiserId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    EventDate = table.Column<DateOnly>(type: "date", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time(0)", nullable: false),
                    Venue = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Province = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    DistanceKm = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    RegistrationCloseDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.EventId);
                    table.CheckConstraint("CK_Events_Distance", "[DistanceKm] > 0");
                    table.CheckConstraint("CK_Events_EventType", "[EventType] IN (N'Running', N'Walking', N'Cycling')");
                    table.CheckConstraint("CK_Events_RegistrationCloseDate", "[RegistrationCloseDate] <= [EventDate]");
                    table.CheckConstraint("CK_Events_Status", "[Status] IN (N'Draft', N'Published', N'Cancelled', N'Completed')");
                    table.ForeignKey(
                        name: "FK_Events_Users_OrganiserId",
                        column: x => x.OrganiserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EventCategories",
                columns: table => new
                {
                    CategoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DistanceKm = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    EntryFee = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    Capacity = table.Column<int>(type: "int", nullable: false),
                    MinimumAge = table.Column<byte>(type: "tinyint", nullable: true),
                    MaximumAge = table.Column<byte>(type: "tinyint", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventCategories", x => x.CategoryId);
                    table.CheckConstraint("CK_EventCategories_AgeRange", "[MaximumAge] IS NULL OR [MinimumAge] IS NULL OR [MaximumAge] >= [MinimumAge]");
                    table.CheckConstraint("CK_EventCategories_Capacity", "[Capacity] > 0");
                    table.CheckConstraint("CK_EventCategories_Distance", "[DistanceKm] > 0");
                    table.CheckConstraint("CK_EventCategories_EntryFee", "[EntryFee] >= 0");
                    table.ForeignKey(
                        name: "FK_EventCategories_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "EventId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Routes",
                columns: table => new
                {
                    RouteId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventId = table.Column<int>(type: "int", nullable: false),
                    RouteName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DistanceKm = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    ElevationGainM = table.Column<int>(type: "int", nullable: false),
                    StartLocation = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FinishLocation = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RouteMapUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Routes", x => x.RouteId);
                    table.CheckConstraint("CK_Routes_Distance", "[DistanceKm] > 0");
                    table.CheckConstraint("CK_Routes_Elevation", "[ElevationGainM] >= 0");
                    table.ForeignKey(
                        name: "FK_Routes_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "EventId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WeatherForecasts",
                columns: table => new
                {
                    WeatherForecastId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventId = table.Column<int>(type: "int", nullable: false),
                    ForecastDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Condition = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TemperatureCelsius = table.Column<decimal>(type: "decimal(4,1)", precision: 4, scale: 1, nullable: false),
                    WindSpeedKph = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: false),
                    PrecipitationChance = table.Column<byte>(type: "tinyint", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    RetrievedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeatherForecasts", x => x.WeatherForecastId);
                    table.CheckConstraint("CK_WeatherForecasts_Precipitation", "[PrecipitationChance] BETWEEN 0 AND 100");
                    table.CheckConstraint("CK_WeatherForecasts_Wind", "[WindSpeedKph] >= 0");
                    table.ForeignKey(
                        name: "FK_WeatherForecasts_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "EventId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Enrolments",
                columns: table => new
                {
                    EnrolmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParticipantId = table.Column<int>(type: "int", nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    EnrolmentDate = table.Column<DateTime>(type: "datetime2(0)", nullable: false),
                    EmergencyContactName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    EmergencyContactPhone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Enrolments", x => x.EnrolmentId);
                    table.CheckConstraint("CK_Enrolments_Status", "[Status] IN (N'Pending', N'Confirmed', N'Cancelled')");
                    table.ForeignKey(
                        name: "FK_Enrolments_EventCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "EventCategories",
                        principalColumn: "CategoryId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Enrolments_Users_ParticipantId",
                        column: x => x.ParticipantId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Results",
                columns: table => new
                {
                    ResultId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EnrolmentId = table.Column<int>(type: "int", nullable: false),
                    FinishTime = table.Column<TimeOnly>(type: "time(0)", nullable: true),
                    OverallPosition = table.Column<int>(type: "int", nullable: true),
                    CategoryPosition = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Results", x => x.ResultId);
                    table.CheckConstraint("CK_Results_FinishTime", "([Status] = N'Finished' AND [FinishTime] IS NOT NULL) OR ([Status] <> N'Finished' AND [FinishTime] IS NULL)");
                    table.CheckConstraint("CK_Results_Positions", "([OverallPosition] IS NULL OR [OverallPosition] > 0) AND ([CategoryPosition] IS NULL OR [CategoryPosition] > 0)");
                    table.CheckConstraint("CK_Results_Status", "[Status] IN (N'Finished', N'DNF', N'DNS', N'DSQ')");
                    table.ForeignKey(
                        name: "FK_Results_Enrolments_EnrolmentId",
                        column: x => x.EnrolmentId,
                        principalTable: "Enrolments",
                        principalColumn: "EnrolmentId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Enrolments_CategoryId",
                table: "Enrolments",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "UQ_Enrolments_Participant_Category",
                table: "Enrolments",
                columns: new[] { "ParticipantId", "CategoryId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_EventCategories_Event_Name",
                table: "EventCategories",
                columns: new[] { "EventId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Events_OrganiserId",
                table: "Events",
                column: "OrganiserId");

            migrationBuilder.CreateIndex(
                name: "UQ_Results_Enrolment",
                table: "Results",
                column: "EnrolmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Routes_Event",
                table: "Routes",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_WeatherForecasts_Event_Date",
                table: "WeatherForecasts",
                columns: new[] { "EventId", "ForecastDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Results");

            migrationBuilder.DropTable(
                name: "Routes");

            migrationBuilder.DropTable(
                name: "WeatherForecasts");

            migrationBuilder.DropTable(
                name: "Enrolments");

            migrationBuilder.DropTable(
                name: "EventCategories");

            migrationBuilder.DropTable(
                name: "Events");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
