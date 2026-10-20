namespace RaceDayAPI.Models
{
    public class Result
    {
        public int ResultId { get; set; }
        public int EnrolmentId { get; set; }
        public TimeOnly? FinishTime { get; set; }
        public int? OverallPosition { get; set; }
        public int? CategoryPosition { get; set; }
        public string Status { get; set; } = "Finished";         // Finished, DNF, DNS, DSQ
        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

        public Enrolment? Enrolment { get; set; }
    }
}
