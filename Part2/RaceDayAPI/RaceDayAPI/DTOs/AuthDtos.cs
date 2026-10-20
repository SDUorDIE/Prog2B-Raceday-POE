using System.ComponentModel.DataAnnotations;

namespace RaceDayAPI.DTOs
{
    /// <summary>Details needed to create an account.</summary>
    public class RegisterDto
    {
        [Required, StringLength(60)] public string FirstName { get; set; } = string.Empty;
        [Required, StringLength(60)] public string LastName { get; set; } = string.Empty;
        [Required, EmailAddress, StringLength(255)] public string Email { get; set; } = string.Empty;
        [Required, MinLength(8), StringLength(100)] public string Password { get; set; } = string.Empty;
        [StringLength(30)] public string? PhoneNumber { get; set; }

        /// <summary>"Organiser" or "Participant".</summary>
        [Required] public string Role { get; set; } = string.Empty;

        /// <summary>Secret code required when registering as an Organiser.</summary>
        public string? OrganiserCode { get; set; }
    }

    public class LoginDto
    {
        [Required, EmailAddress] public string Email { get; set; } = string.Empty;
        [Required] public string Password { get; set; } = string.Empty;
    }

    public class UpdateProfileDto
    {
        [Required, StringLength(60)] public string FirstName { get; set; } = string.Empty;
        [Required, StringLength(60)] public string LastName { get; set; } = string.Empty;
        [StringLength(30)] public string? PhoneNumber { get; set; }
    }

    /// <summary>Safe view of a user - never includes the password hash.</summary>
    public class UserProfileDto
    {
        public int UserId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string Role { get; set; } = string.Empty;
    }
}
