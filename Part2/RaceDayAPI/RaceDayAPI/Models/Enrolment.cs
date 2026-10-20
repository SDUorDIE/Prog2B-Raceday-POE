namespace RaceDayAPI.Models
{
    public class Enrolment
    {
        public int EnrolmentId { get; set; }
        public int ParticipantId { get; set; }
        public int CategoryId { get; set; }
        public DateTime EnrolmentDate { get; set; } = DateTime.UtcNow;
        public string EmergencyContactName { get; set; } = string.Empty;
        public string EmergencyContactPhone { get; set; } = string.Empty;
        public string Status { get; set; } = "Confirmed";        // Pending, Confirmed, Cancelled

        public User? Participant { get; set; }
        public EventCategory? Category { get; set; }
        public Result? Result { get; set; }
    }
}
