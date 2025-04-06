using System.ComponentModel.DataAnnotations;
using BlazorApp1.Shared.Enums;

namespace BlazorApp1.Shared.Models
{
    public class SubmissionDto : IValidatableObject
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Subject is required")]
        [StringLength(200, ErrorMessage = "Subject cannot exceed 200 characters")]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Type is required")]
        [StringLength(50, ErrorMessage = "Type cannot exceed 50 characters")]
        public string Type { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "Priority cannot exceed 50 characters")]
        public string Priority { get; set; } = string.Empty;

        public SubmissionStatus Status { get; set; } = SubmissionStatus.Pending;

        [StringLength(100, ErrorMessage = "Category cannot exceed 100 characters")]
        public string Category { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "Location cannot exceed 200 characters")]
        public string Location { get; set; } = string.Empty;

        public DateTime? PreferredDate { get; set; }

        public List<string> Attachments { get; set; } = new List<string>();

        public string UserEmail { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public int UserId { get; set; }
        public DateTime SubmittedDate { get; set; }
        public DateTime LastUpdated { get; set; }
        public bool IsClientSubmission { get; set; }

        // Helper methods for UI
        public bool IsResolved => Status == SubmissionStatus.Resolved;
        public bool IsPending => Status == SubmissionStatus.Pending;
        public bool IsInProgress => Status == SubmissionStatus.InProgress;
        public bool IsRejected => Status == SubmissionStatus.Rejected;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // Priority is required for concerns and requests, but not for suggestions
            if (Type?.ToLower() != "suggestion" && string.IsNullOrEmpty(Priority))
            {
                yield return new ValidationResult(
                    "Priority is required for this type of submission",
                    new[] { nameof(Priority) }
                );
            }
        }

        public string GetStatusClass() => Status switch
        {
            SubmissionStatus.Pending => "bg-warning text-dark",
            SubmissionStatus.InProgress => "bg-info text-white",
            SubmissionStatus.Resolved => "bg-success text-white",
            SubmissionStatus.Rejected => "bg-danger text-white",
            _ => "bg-secondary text-white"
        };

        public string GetPriorityClass() => Priority?.ToLower() switch
        {
            "high" => "text-danger",
            "medium" => "text-warning",
            "low" => "text-success",
            _ => "text-secondary"
        };
    }
} 