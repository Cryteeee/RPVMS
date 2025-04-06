using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BlazorApp1.Shared
{
    public class RegistrationView
    {
        [JsonPropertyName("userId")]
        public int UserId { get; set; }

        [JsonPropertyName("email")]
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("username")]
        [Required(ErrorMessage = "Username is required")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters")]
        public string Username { get; set; } = string.Empty;

        [JsonPropertyName("password")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long")]
        public string? Password { get; set; }

        [JsonPropertyName("currentPassword")]
        [Required(ErrorMessage = "Current password is required for changes")]
        public string CurrentPassword { get; set; } = string.Empty;

        [JsonPropertyName("newPassword")]
        public string? NewPassword { get; set; }

        [JsonPropertyName("confirmNewPassword")]
        public string? ConfirmNewPassword { get; set; }

        [JsonPropertyName("gender")]
        public string? Gender { get; set; }

        [JsonPropertyName("nationality")]
        public string? Nationality { get; set; }

        [JsonPropertyName("dateOfBirth")]
        public DateTime? DateOfBirth { get; set; }

        [JsonPropertyName("photoUrl")]
        public string? PhotoUrl { get; set; }

        [JsonPropertyName("photoBase64")]
        public string? PhotoBase64 { get; set; }

        [JsonPropertyName("isEmailVerified")]
        public bool IsEmailVerified { get; set; }

        [JsonPropertyName("fullName")]
        public string? FullName { get; set; }

        [JsonPropertyName("address")]
        public string? Address { get; set; }

        [JsonPropertyName("maritalStatus")]
        public string? MaritalStatus { get; set; }
    }
} 