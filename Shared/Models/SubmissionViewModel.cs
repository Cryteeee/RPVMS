using BlazorApp1.Shared.Enums;

namespace BlazorApp1.Shared.Models
{
    public class SubmissionViewModel
    {
        public int Id { get; set; }
        public string? Subject { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Type { get; set; }
        public string? Priority { get; set; }
        public SubmissionStatus Status { get; set; }
        public string? Category { get; set; }
        public string? Location { get; set; }
        public DateTime? PreferredDate { get; set; }
        public List<string> Attachments { get; set; } = new List<string>();
        public string? UserEmail { get; set; }
        public string? UserName { get; set; }
        public string? UserId { get; set; }
        public DateTime SubmittedDate { get; set; }
        public DateTime LastUpdated { get; set; }
        public bool IsResolved => Status == SubmissionStatus.Resolved;
        public bool IsPending => Status == SubmissionStatus.Pending;
        public bool IsInProgress => Status == SubmissionStatus.InProgress;
        public bool IsRejected => Status == SubmissionStatus.Rejected;

        public string StatusDisplayName => Status.ToString();
        public string? PriorityDisplayName => Priority;
        public string? TypeDisplayName => Type;

        public string GetStatusClass()
        {
            return Status switch
            {
                SubmissionStatus.Pending => "bg-warning text-dark",
                SubmissionStatus.InProgress => "bg-info text-white",
                SubmissionStatus.Resolved => "bg-success text-white",
                SubmissionStatus.Rejected => "bg-danger text-white",
                _ => "bg-secondary text-white"
            };
        }

        public string GetPriorityClass()
        {
            return Priority?.ToLower() switch
            {
                "high" => "text-danger",
                "medium" => "text-warning",
                "low" => "text-success",
                _ => "text-secondary"
            };
        }
    }
} 