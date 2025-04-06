using System;
using System.ComponentModel.DataAnnotations;
using BlazorApp1.Shared;
using BlazorApp1.Shared.Enums;

namespace BlazorApp1.Shared.Models
{
    public enum FormType
    {
        Request = 1,
        Concern = 2,
        Suggestion = 3
    }

    public enum UrgencyLevel
    {
        Low = 1,
        Medium = 2,
        High = 3,
        Critical = 4
    }

    public enum PriorityLevel
    {
        Low = 1,
        Medium = 2,
        High = 3,
        Critical = 4
    }

    public class ContactForm
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        public string Title { get; set; } = string.Empty;

        public FormType FormType { get; set; }

        public string Category { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } = string.Empty;

        // IP tracking fields
        public string ClientIP { get; set; } = string.Empty;
        public DateTime SubmissionTimestamp { get; set; } = DateTime.UtcNow;

        // Request specific fields
        [RequiredIf("FormType", FormType.Request, ErrorMessage = "Request type is required")]
        public RequestType? RequestType { get; set; }

        [RequiredIf("FormType", FormType.Request, ErrorMessage = "Urgency level is required")]
        public UrgencyLevel? UrgencyLevel { get; set; } = Models.UrgencyLevel.Low;

        [RequiredIf("FormType", FormType.Request, ErrorMessage = "Preferred date is required")]
        public DateTime? PreferredDate { get; set; }

        // Concern specific fields
        [RequiredIf("FormType", FormType.Concern, ErrorMessage = "Priority level is required")]
        public PriorityLevel? PriorityLevel { get; set; } = Models.PriorityLevel.Low;

        [RequiredIf("FormType", FormType.Concern, ErrorMessage = "Location is required")]
        public string Location { get; set; } = string.Empty;

        // Concern category field
        [RequiredIf("FormType", FormType.Concern, ErrorMessage = "Concern category is required")]
        public ConcernCategory? ConcernCategory { get; set; }

        // Suggestion specific fields
        [RequiredIf("FormType", FormType.Suggestion, ErrorMessage = "Category is required")]
        public string SuggestionCategory { get; set; } = string.Empty;

        // Common fields
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Status fields
        public bool IsRead { get; set; } = false;
        public bool IsResolved { get; set; } = false;
        public SubmissionStatus Status { get; set; } = SubmissionStatus.Pending;

        // Archive fields
        public bool IsArchived { get; set; } = false;
        public DateTime? ArchivedDate { get; set; }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class RequiredIfAttribute : ValidationAttribute
    {
        private readonly string _propertyName;
        private readonly object _desiredValue;

        public RequiredIfAttribute(string propertyName, object desiredValue, string errorMessage = null)
        {
            _propertyName = propertyName;
            _desiredValue = desiredValue;
            if (!string.IsNullOrEmpty(errorMessage))
            {
                ErrorMessage = errorMessage;
            }
            else
            {
                ErrorMessage = "The field is required.";
            }
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var property = validationContext.ObjectType.GetProperty(_propertyName);
            if (property == null)
            {
                return ValidationResult.Success;
            }

            var propertyValue = property.GetValue(validationContext.ObjectInstance);
            if (!_desiredValue.Equals(propertyValue))
            {
                return ValidationResult.Success;
            }

            if (value == null)
            {
                return new ValidationResult(ErrorMessage);
            }

            if (value is string str && string.IsNullOrWhiteSpace(str))
            {
                return new ValidationResult(ErrorMessage);
            }

            if (value is DateTime dateTime && dateTime == default)
            {
                return new ValidationResult(ErrorMessage);
            }

            return ValidationResult.Success;
        }
    }

    // Classes for IP-based security
    public class SuspiciousActivityReport
    {
        public string IP { get; set; } = string.Empty;
        public int SubmissionCount { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class IPBanRequest
    {
        public string IP { get; set; } = string.Empty;
        public int DurationHours { get; set; } = 24;
    }

} 