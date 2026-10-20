namespace RaceDayAPI.Models
{
    public class WeatherForecast
    {
        public int WeatherForecastId { get; set; }
        public int EventId { get; set; }
        public DateOnly ForecastDate { get; set; }
        public string Condition { get; set; } = string.Empty;
        public decimal TemperatureCelsius { get; set; }
        public decimal WindSpeedKph { get; set; }
        public byte PrecipitationChance { get; set; }
        public string Source { get; set; } = string.Empty;
        public DateTime RetrievedAt { get; set; } = DateTime.UtcNow;

        public Event? Event { get; set; }
    }
}
