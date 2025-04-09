using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace BlazorApp1.Shared.Validations
{
    public class CustomEmailAttribute : ValidationAttribute
    {
        private const string EmailPattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
        private readonly string[] AllowedDomains = new[]
        {
            "gmail.com",
            "yahoo.com",
            "outlook.com",
            "hotmail.com",
            "icloud.com",
            "aol.com",
            "protonmail.com"
        };

        public CustomEmailAttribute()
        {
            ErrorMessage = "Invalid email format. Please use a valid email domain (e.g., Gmail, Yahoo, Outlook).";
        }

        public override bool IsValid(object? value)
        {
            if (value == null)
            {
                return false;
            }

            string email = value.ToString()!;
            
            // Check if email matches the pattern
            if (!Regex.IsMatch(email, EmailPattern))
            {
                return false;
            }

            // Additional checks
            string[] parts = email.Split('@');
            if (parts.Length != 2)
            {
                return false;
            }

            string localPart = parts[0];
            string domainPart = parts[1].ToLower();

            // Check local part
            if (string.IsNullOrEmpty(localPart) || localPart.StartsWith(".") || localPart.EndsWith("."))
            {
                return false;
            }

            // Check domain part
            if (string.IsNullOrEmpty(domainPart) || domainPart.StartsWith(".") || domainPart.EndsWith("."))
            {
                return false;
            }

            // Check for consecutive dots
            if (localPart.Contains("..") || domainPart.Contains(".."))
            {
                return false;
            }

            // Check if domain is in the allowed list
            bool isValidDomain = false;
            foreach (var allowedDomain in AllowedDomains)
            {
                if (domainPart == allowedDomain || domainPart.EndsWith("." + allowedDomain))
                {
                    isValidDomain = true;
                    break;
                }
            }

            if (!isValidDomain)
            {
                ErrorMessage = $"Invalid email domain. Please use one of the following domains: {string.Join(", ", AllowedDomains)}";
                return false;
            }

            return true;
        }
    }
} 