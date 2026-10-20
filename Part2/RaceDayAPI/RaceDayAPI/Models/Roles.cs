namespace RaceDayAPI.Models
{
    /// <summary>The two roles in the system. Stored in the auth cookie as a claim.</summary>
    public static class Roles
    {
        public const string Organiser = "Organiser";
        public const string Participant = "Participant";
    }
}
