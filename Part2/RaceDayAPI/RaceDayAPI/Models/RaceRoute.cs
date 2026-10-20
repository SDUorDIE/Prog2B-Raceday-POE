namespace RaceDayAPI.Models
{
    // Named RaceRoute (not Route) so it never clashes with the [Route] attribute.
    // It is still mapped to the "Routes" table from the Part 1 script.
    public class RaceRoute
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

        public Event? Event { get; set; }
    }
}
