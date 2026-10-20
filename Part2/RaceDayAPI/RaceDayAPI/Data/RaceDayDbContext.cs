using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Models;

namespace RaceDayAPI.Data
{
    public class RaceDayDbContext : DbContext
    {
        public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<EventCategory> EventCategories { get; set; }
        public DbSet<Enrolment> Enrolments { get; set; }
        public DbSet<Result> Results { get; set; }
        public DbSet<RaceRoute> Routes { get; set; }
        public DbSet<WeatherForecast> WeatherForecasts { get; set; }

        // The Part 1 SQL script is mirrored here: same tables, columns, unique and check constraints.
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(e =>
            {
                e.ToTable("Users", t =>
                    t.HasCheckConstraint("CK_Users_Role", "[Role] IN (N'Organiser', N'Participant')"));
                e.HasKey(x => x.UserId);
                e.Property(x => x.FirstName).HasMaxLength(60).IsRequired();
                e.Property(x => x.LastName).HasMaxLength(60).IsRequired();
                e.Property(x => x.Email).HasMaxLength(255).IsRequired();
                e.Property(x => x.PasswordHash).HasMaxLength(255).IsRequired();
                e.Property(x => x.PhoneNumber).HasMaxLength(30);
                e.Property(x => x.Role).HasMaxLength(20).IsRequired();
                e.Property(x => x.CreatedAt).HasColumnType("datetime2(0)");
                e.HasIndex(x => x.Email).IsUnique().HasDatabaseName("UQ_Users_Email");
            });

            modelBuilder.Entity<Event>(e =>
            {
                e.ToTable("Events", t =>
                {
                    t.HasCheckConstraint("CK_Events_EventType", "[EventType] IN (N'Running', N'Walking', N'Cycling')");
                    t.HasCheckConstraint("CK_Events_Status", "[Status] IN (N'Draft', N'Published', N'Cancelled', N'Completed')");
                    t.HasCheckConstraint("CK_Events_RegistrationCloseDate", "[RegistrationCloseDate] <= [EventDate]");
                    t.HasCheckConstraint("CK_Events_Distance", "[DistanceKm] > 0");
                });
                e.HasKey(x => x.EventId);
                e.Property(x => x.Name).HasMaxLength(150).IsRequired();
                e.Property(x => x.EventType).HasMaxLength(20).IsRequired();
                e.Property(x => x.StartTime).HasColumnType("time(0)");
                e.Property(x => x.Venue).HasMaxLength(150).IsRequired();
                e.Property(x => x.City).HasMaxLength(100).IsRequired();
                e.Property(x => x.Province).HasMaxLength(100).IsRequired();
                e.Property(x => x.Description).HasMaxLength(1000).IsRequired();
                e.Property(x => x.DistanceKm).HasPrecision(6, 2);
                e.Property(x => x.Status).HasMaxLength(20).IsRequired();
                e.Property(x => x.CreatedAt).HasColumnType("datetime2(0)");
                e.HasOne(x => x.Organiser).WithMany(u => u.OrganisedEvents)
                    .HasForeignKey(x => x.OrganiserId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<EventCategory>(e =>
            {
                e.ToTable("EventCategories", t =>
                {
                    t.HasCheckConstraint("CK_EventCategories_Distance", "[DistanceKm] > 0");
                    t.HasCheckConstraint("CK_EventCategories_EntryFee", "[EntryFee] >= 0");
                    t.HasCheckConstraint("CK_EventCategories_Capacity", "[Capacity] > 0");
                    t.HasCheckConstraint("CK_EventCategories_AgeRange",
                        "[MaximumAge] IS NULL OR [MinimumAge] IS NULL OR [MaximumAge] >= [MinimumAge]");
                });
                e.HasKey(x => x.CategoryId);
                e.Property(x => x.Name).HasMaxLength(100).IsRequired();
                e.Property(x => x.DistanceKm).HasPrecision(6, 2);
                e.Property(x => x.EntryFee).HasPrecision(10, 2);
                e.HasIndex(x => new { x.EventId, x.Name }).IsUnique().HasDatabaseName("UQ_EventCategories_Event_Name");
                e.HasOne(x => x.Event).WithMany(ev => ev.Categories)
                    .HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Enrolment>(e =>
            {
                e.ToTable("Enrolments", t =>
                    t.HasCheckConstraint("CK_Enrolments_Status", "[Status] IN (N'Pending', N'Confirmed', N'Cancelled')"));
                e.HasKey(x => x.EnrolmentId);
                e.Property(x => x.EnrolmentDate).HasColumnType("datetime2(0)");
                e.Property(x => x.EmergencyContactName).HasMaxLength(120).IsRequired();
                e.Property(x => x.EmergencyContactPhone).HasMaxLength(30).IsRequired();
                e.Property(x => x.Status).HasMaxLength(20).IsRequired();
                e.HasIndex(x => new { x.ParticipantId, x.CategoryId }).IsUnique()
                    .HasDatabaseName("UQ_Enrolments_Participant_Category");
                e.HasOne(x => x.Participant).WithMany(u => u.Enrolments)
                    .HasForeignKey(x => x.ParticipantId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.Category).WithMany(c => c.Enrolments)
                    .HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Result>(e =>
            {
                e.ToTable("Results", t =>
                {
                    t.HasCheckConstraint("CK_Results_Status", "[Status] IN (N'Finished', N'DNF', N'DNS', N'DSQ')");
                    t.HasCheckConstraint("CK_Results_Positions",
                        "([OverallPosition] IS NULL OR [OverallPosition] > 0) AND ([CategoryPosition] IS NULL OR [CategoryPosition] > 0)");
                    t.HasCheckConstraint("CK_Results_FinishTime",
                        "([Status] = N'Finished' AND [FinishTime] IS NOT NULL) OR ([Status] <> N'Finished' AND [FinishTime] IS NULL)");
                });
                e.HasKey(x => x.ResultId);
                e.Property(x => x.FinishTime).HasColumnType("time(0)");
                e.Property(x => x.Status).HasMaxLength(20).IsRequired();
                e.Property(x => x.RecordedAt).HasColumnType("datetime2(0)");
                e.HasIndex(x => x.EnrolmentId).IsUnique().HasDatabaseName("UQ_Results_Enrolment");
                e.HasOne(x => x.Enrolment).WithOne(en => en.Result)
                    .HasForeignKey<Result>(x => x.EnrolmentId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<RaceRoute>(e =>
            {
                e.ToTable("Routes", t =>
                {
                    t.HasCheckConstraint("CK_Routes_Distance", "[DistanceKm] > 0");
                    t.HasCheckConstraint("CK_Routes_Elevation", "[ElevationGainM] >= 0");
                });
                e.HasKey(x => x.RouteId);
                e.Property(x => x.RouteName).HasMaxLength(150).IsRequired();
                e.Property(x => x.DistanceKm).HasPrecision(6, 2);
                e.Property(x => x.StartLocation).HasMaxLength(200).IsRequired();
                e.Property(x => x.FinishLocation).HasMaxLength(200).IsRequired();
                e.Property(x => x.RouteMapUrl).HasMaxLength(500);
                e.Property(x => x.Notes).HasMaxLength(1000);
                e.HasIndex(x => x.EventId).IsUnique().HasDatabaseName("UQ_Routes_Event");
                e.HasOne(x => x.Event).WithOne(ev => ev.Route)
                    .HasForeignKey<RaceRoute>(x => x.EventId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<WeatherForecast>(e =>
            {
                e.ToTable("WeatherForecasts", t =>
                {
                    t.HasCheckConstraint("CK_WeatherForecasts_Wind", "[WindSpeedKph] >= 0");
                    t.HasCheckConstraint("CK_WeatherForecasts_Precipitation", "[PrecipitationChance] BETWEEN 0 AND 100");
                });
                e.HasKey(x => x.WeatherForecastId);
                e.Property(x => x.Condition).HasMaxLength(100).IsRequired();
                e.Property(x => x.TemperatureCelsius).HasPrecision(4, 1);
                e.Property(x => x.WindSpeedKph).HasPrecision(5, 1);
                e.Property(x => x.Source).HasMaxLength(150).IsRequired();
                e.Property(x => x.RetrievedAt).HasColumnType("datetime2(0)");
                e.HasIndex(x => new { x.EventId, x.ForecastDate }).IsUnique()
                    .HasDatabaseName("UQ_WeatherForecasts_Event_Date");
                e.HasOne(x => x.Event).WithMany(ev => ev.WeatherForecasts)
                    .HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
