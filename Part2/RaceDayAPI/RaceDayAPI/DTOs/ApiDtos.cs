using System.ComponentModel.DataAnnotations;

namespace RaceDayAPI.DTOs
{
    // ---------- Events ----------
    /// <summary>Body for creating or updating an event.</summary>
    public class EventRequestDto
    {
        [Required, StringLength(150)] public string Name { get; set; } = string.Empty;

        /// <summary>Running, Walking or Cycling.</summary>
        [Required, RegularExpression("^(Running|Walking|Cycling)$", ErrorMessage = "EventType must be Running, Walking or Cycling.")]
        public string EventType { get; set; } = string.Empty;

        [Required] public DateOnly? EventDate { get; set; }
        [Required] public TimeOnly? StartTime { get; set; }
        [Required, StringLength(150)] public string Venue { get; set; } = string.Empty;
        [Required, StringLength(100)] public string City { get; set; } = string.Empty;
        [Required, StringLength(100)] public string Province { get; set; } = string.Empty;
        [Required, StringLength(1000)] public string Description { get; set; } = string.Empty;

        /// <summary>Overall event distance in kilometres.</summary>
        [Range(0.01, 1000)] public decimal DistanceKm { get; set; }

        /// <summary>Last day participants can enrol. Must be on or before the event date.</summary>
        [Required] public DateOnly? RegistrationCloseDate { get; set; }

        /// <summary>Draft, Published, Cancelled or Completed. Defaults to Published on create.</summary>
        [RegularExpression("^(Draft|Published|Cancelled|Completed)$", ErrorMessage = "Status must be Draft, Published, Cancelled or Completed.")]
        public string? Status { get; set; }
    }

    public class EventSummaryDto
    {
        public int EventId { get; set; }
        public int OrganiserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public DateOnly EventDate { get; set; }
        public TimeOnly StartTime { get; set; }
        public string Venue { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal DistanceKm { get; set; }
        public DateOnly RegistrationCloseDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class EventDetailDto : EventSummaryDto
    {
        public string OrganiserName { get; set; } = string.Empty;
        public List<CategoryDto> Categories { get; set; } = new();
        public RouteDto? Route { get; set; }
        public WeatherDto? CurrentForecast { get; set; }
    }

    // ---------- Categories ----------
    /// <summary>Body for creating a category. Use age fields, distance, or both.</summary>
    public class CategoryRequestDto
    {
        /// <summary>For example "Under 20", "Senior", "10 km" or "21 km".</summary>
        [Required, StringLength(100)] public string Name { get; set; } = string.Empty;
        [Range(0.01, 1000)] public decimal DistanceKm { get; set; }
        [Range(0, 100000)] public decimal EntryFee { get; set; }
        [Range(1, 100000)] public int Capacity { get; set; }
        [Range(0, 120)] public byte? MinimumAge { get; set; }
        [Range(0, 120)] public byte? MaximumAge { get; set; }
    }

    /// <summary>Body for updating a category (adds IsActive).</summary>
    public class CategoryUpdateDto : CategoryRequestDto
    {
        public bool IsActive { get; set; } = true;
    }

    public class CategoryDto
    {
        public int CategoryId { get; set; }
        public int EventId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal DistanceKm { get; set; }
        public decimal EntryFee { get; set; }
        public int Capacity { get; set; }
        public byte? MinimumAge { get; set; }
        public byte? MaximumAge { get; set; }
        public bool IsActive { get; set; }
    }

    // ---------- Enrolments ----------
    public class EnrolmentRequestDto
    {
        [Range(1, int.MaxValue)] public int CategoryId { get; set; }
        [Required, StringLength(120)] public string EmergencyContactName { get; set; } = string.Empty;
        [Required, StringLength(30)] public string EmergencyContactPhone { get; set; } = string.Empty;
    }

    public class EnrolmentDto
    {
        public int EnrolmentId { get; set; }
        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int ParticipantId { get; set; }
        public string ParticipantName { get; set; } = string.Empty;
        public DateTime EnrolmentDate { get; set; }
        public string EmergencyContactName { get; set; } = string.Empty;
        public string EmergencyContactPhone { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    // ---------- Results ----------
    public class ResultRequestDto
    {
        /// <summary>Required when Status is Finished (format HH:mm:ss); must be empty otherwise.</summary>
        public TimeOnly? FinishTime { get; set; }
        [Range(1, 100000)] public int? OverallPosition { get; set; }
        [Range(1, 100000)] public int? CategoryPosition { get; set; }

        /// <summary>Finished, DNF, DNS or DSQ.</summary>
        [Required, RegularExpression("^(Finished|DNF|DNS|DSQ)$", ErrorMessage = "Status must be Finished, DNF, DNS or DSQ.")]
        public string Status { get; set; } = string.Empty;
    }

    public class ResultDto
    {
        public int ResultId { get; set; }
        public int EnrolmentId { get; set; }
        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public string ParticipantName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public TimeOnly? FinishTime { get; set; }
        public int? OverallPosition { get; set; }
        public int? CategoryPosition { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime RecordedAt { get; set; }
    }

    // ---------- Routes ----------
    public class RouteRequestDto
    {
        [Required, StringLength(150)] public string RouteName { get; set; } = string.Empty;
        [Range(0.01, 1000)] public decimal DistanceKm { get; set; }
        [Range(0, 10000)] public int ElevationGainM { get; set; }
        [Required, StringLength(200)] public string StartLocation { get; set; } = string.Empty;
        [Required, StringLength(200)] public string FinishLocation { get; set; } = string.Empty;
        [StringLength(500)] public string? RouteMapUrl { get; set; }
        [StringLength(1000)] public string? Notes { get; set; }
    }

    public class RouteDto
    {
        public int RouteId { get; set; }
        public int EventId { get; set; }
        public string RouteName { get; set; } = string.Empty;
        public decimal DistanceKm { get; set; }
        public int ElevationGainM { get; set; }
        public string StartLocation { get; set; } = string.Empty;
        public string FinishLocation { get; set; } = string.Empty;
        public string? RouteMapUrl { get; set; }
        public string? Notes { get; set; }
    }

    // ---------- Weather ----------
    public class WeatherRequestDto
    {
        [Required] public DateOnly? ForecastDate { get; set; }
        [Required, StringLength(100)] public string Condition { get; set; } = string.Empty;
        [Range(-60, 60)] public decimal TemperatureCelsius { get; set; }
        [Range(0, 400)] public decimal WindSpeedKph { get; set; }
        [Range(0, 100)] public byte PrecipitationChance { get; set; }
        [Required, StringLength(150)] public string Source { get; set; } = string.Empty;
    }

    public class WeatherDto
    {
        public int WeatherForecastId { get; set; }
        public int EventId { get; set; }
        public DateOnly ForecastDate { get; set; }
        public string Condition { get; set; } = string.Empty;
        public decimal TemperatureCelsius { get; set; }
        public decimal WindSpeedKph { get; set; }
        public byte PrecipitationChance { get; set; }
        public string Source { get; set; } = string.Empty;
        public DateTime RetrievedAt { get; set; }
    }
}
