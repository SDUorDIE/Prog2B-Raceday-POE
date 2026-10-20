namespace RaceDayAPI.Models
{
    public class Event
    {
        public int EventId { get; set; }
        public int OrganiserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;   // Running, Walking, Cycling
        public DateOnly EventDate { get; set; }
        public TimeOnly StartTime { get; set; }
        public string Venue { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal DistanceKm { get; set; }                  // NEW: required by the Part 2 brief
        public DateOnly RegistrationCloseDate { get; set; }
        public string Status { get; set; } = "Published";        // Draft, Published, Cancelled, Completed
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User? Organiser { get; set; }
        public ICollection<EventCategory> Categories { get; set; } = new List<EventCategory>();
        public RaceRoute? Route { get; set; }
        public ICollection<WeatherForecast> WeatherForecasts { get; set; } = new List<WeatherForecast>();
    }
}
