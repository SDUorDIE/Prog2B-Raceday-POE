namespace RaceDayAPI.Models
{
    public class EventCategory
    {
        public int CategoryId { get; set; }
        public int EventId { get; set; }
        public string Name { get; set; } = string.Empty;        // e.g. "Under 20", "Senior", "10 km Open"
        public decimal DistanceKm { get; set; }
        public decimal EntryFee { get; set; }
        public int Capacity { get; set; }
        public byte? MinimumAge { get; set; }
        public byte? MaximumAge { get; set; }
        public bool IsActive { get; set; } = true;

        public Event? Event { get; set; }
        public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
    }
}
