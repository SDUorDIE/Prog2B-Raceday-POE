using System.ComponentModel.DataAnnotations;

namespace RaceDayMVC.Models;

public sealed class LoginViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}

public sealed class RegisterViewModel
{
    [Required, StringLength(60)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(60)]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(8), StringLength(100), DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [StringLength(30)]
    public string? PhoneNumber { get; set; }

    [Required]
    public string Role { get; set; } = "Participant";

    [DataType(DataType.Password)]
    public string? OrganiserCode { get; set; }
}

public sealed class UserProfile
{
    public int UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string Role { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
}

public sealed class ProfileViewModel
{
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    [Required, StringLength(60)] public string FirstName { get; set; } = string.Empty;
    [Required, StringLength(60)] public string LastName { get; set; } = string.Empty;
    [StringLength(30)] public string? PhoneNumber { get; set; }
}

public sealed class OrganiserDashboardViewModel
{
    public List<EventSummary> Events { get; set; } = [];
    public Dictionary<int, int> EnrolmentCounts { get; set; } = [];
    public int TotalEnrolments => EnrolmentCounts.Values.Sum();
    public int UpcomingEvents => Events.Count(e => e.EventDate >= DateOnly.FromDateTime(DateTime.Today) && e.Status == "Published");
}

public class EventSummary
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
    public string? BannerImageUrl { get; set; }
}

public sealed class EventCategory
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

public sealed class EventDetail : EventSummary
{
    public string OrganiserName { get; set; } = string.Empty;
    public List<EventCategory> Categories { get; set; } = [];
}

public sealed class EventsIndexViewModel
{
    public string? Search { get; set; }
    public string? EventType { get; set; }
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
    public List<EventSummary> Events { get; set; } = [];
    public string? ErrorMessage { get; set; }
}

public sealed class EventRequestViewModel
{
    public int EventId { get; set; }
    public string? BannerImageUrl { get; set; }
    public IFormFile? BannerImage { get; set; }

    [Required, StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string EventType { get; set; } = "Running";

    [Required]
    public DateOnly? EventDate { get; set; }

    [Required]
    public TimeOnly? StartTime { get; set; }

    [Required, StringLength(150)]
    public string Venue { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string City { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Province { get; set; } = string.Empty;

    [Required, StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Range(0.01, 1000)]
    public decimal DistanceKm { get; set; }

    [Required]
    public DateOnly? RegistrationCloseDate { get; set; }

    [Required]
    public string Status { get; set; } = "Published";
}

public sealed class CategoryRequestViewModel
{
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Range(0.01, 1000)]
    public decimal DistanceKm { get; set; }

    [Range(0, 100000)]
    public decimal EntryFee { get; set; }

    [Range(1, 100000)]
    public int Capacity { get; set; }

    [Range(0, 120)]
    public byte? MinimumAge { get; set; }

    [Range(0, 120)]
    public byte? MaximumAge { get; set; }
}

public sealed class CategoryManagementViewModel
{
    public EventDetail Event { get; set; } = new();
    public CategoryRequestViewModel Form { get; set; } = new();
}

public sealed class EnrolmentRequestViewModel
{
    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }

    [Required, StringLength(120)]
    public string EmergencyContactName { get; set; } = string.Empty;

    [Required, StringLength(30)]
    public string EmergencyContactPhone { get; set; } = string.Empty;
}

public sealed class EnrolmentSummary
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

public sealed class ResultSummary
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateOnly? EventDate { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public TimeOnly? FinishTime { get; set; }
    public int? OverallPosition { get; set; }
    public int? CategoryPosition { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime RecordedAt { get; set; }
}

public sealed class EnterEventViewModel
{
    public EventDetail Event { get; set; } = new();
    public EnrolmentRequestViewModel Form { get; set; } = new();
}

public sealed class ResultRequestViewModel
{
    public TimeOnly? FinishTime { get; set; }

    [Range(1, 100000)]
    public int? OverallPosition { get; set; }

    [Range(1, 100000)]
    public int? CategoryPosition { get; set; }

    [Required]
    public string Status { get; set; } = "Finished";
}

public sealed class EventResultsViewModel
{
    public EventDetail Event { get; set; } = new();
    public List<EnrolmentSummary> Enrolments { get; set; } = [];
    public List<ResultSummary> Results { get; set; } = [];
}
