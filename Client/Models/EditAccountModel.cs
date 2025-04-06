using System;
using System.ComponentModel.DataAnnotations;

namespace BlazorApp1.Client.Models
{
    public class EditAccountModel : IValidatableObject
    {
        public int UserId { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? CurrentPassword { get; set; }
        public string? NewPassword { get; set; }
        public string? ConfirmPassword { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            // Validate username if provided
            if (!string.IsNullOrWhiteSpace(Username) && (Username.Length < 3 || Username.Length > 50))
            {
                results.Add(new ValidationResult(
                    "Username must be between 3 and 50 characters",
                    new[] { nameof(Username) }
                ));
            }

            // Validate email if provided
            if (!string.IsNullOrWhiteSpace(Email))
            {
                bool isValidEmail = false;
                try
                {
                    var addr = new System.Net.Mail.MailAddress(Email);
                    isValidEmail = addr.Address == Email;
                }
                catch
                {
                    isValidEmail = false;
                }

                if (!isValidEmail)
                {
                    results.Add(new ValidationResult(
                        "Please enter a valid email address",
                        new[] { nameof(Email) }
                    ));
                }
            }

            // Only validate password fields if attempting to change password
            if (!string.IsNullOrWhiteSpace(NewPassword))
            {
                if (string.IsNullOrWhiteSpace(CurrentPassword))
                {
                    results.Add(new ValidationResult(
                        "Current password is required when changing password",
                        new[] { nameof(CurrentPassword) }
                    ));
                }

                if (NewPassword.Length < 8 || !NewPassword.Any(char.IsUpper) || 
                    !NewPassword.Any(char.IsLower) || !NewPassword.Any(char.IsDigit))
                {
                    results.Add(new ValidationResult(
                        "Password must be at least 8 characters long and contain at least one uppercase letter, one lowercase letter, and one number",
                        new[] { nameof(NewPassword) }
                    ));
                }

                if (string.IsNullOrWhiteSpace(ConfirmPassword))
                {
                    results.Add(new ValidationResult(
                        "Password confirmation is required when setting a new password",
                        new[] { nameof(ConfirmPassword) }
                    ));
                }
                else if (NewPassword != ConfirmPassword)
                {
                    results.Add(new ValidationResult(
                        "The new password and confirmation password do not match",
                        new[] { nameof(ConfirmPassword) }
                    ));
                }
            }

            return results;
        }
    }
} 