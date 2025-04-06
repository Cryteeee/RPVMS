using System;
using System.ComponentModel.DataAnnotations;
using BlazorApp1.Shared.Constants;

namespace BlazorApp1.Shared
{
    public class UpdateAccountDto : IValidatableObject
    {
        public int UserId { get; set; }

        [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters")]
        public string? Username { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string? Email { get; set; }

        public string? CurrentPassword { get; set; }

        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$", 
            ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, and one number")]
        public string? NewPassword { get; set; }

        [Compare("NewPassword", ErrorMessage = "The password and confirmation password do not match")]
        public string? ConfirmPassword { get; set; }
        
        public string? Role { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // Only validate password fields if attempting to change password
            if (!string.IsNullOrWhiteSpace(NewPassword))
            {
                if (string.IsNullOrWhiteSpace(CurrentPassword))
                {
                    yield return new ValidationResult(
                        "Current password is required when changing password",
                        new[] { nameof(CurrentPassword) }
                    );
                }

                if (string.IsNullOrWhiteSpace(ConfirmPassword))
                {
                    yield return new ValidationResult(
                        "Password confirmation is required when setting a new password",
                        new[] { nameof(ConfirmPassword) }
                    );
                }
            }
        }
    }
} 