namespace BlazorApp1.Shared.Enums
{
    public enum SubmissionStatus
    {
        Pending,
        InProgress,
        Resolved,
        Rejected,
        Deleted,
        Archived
    }

    public enum SubmissionPriority
    {
        Low,
        Medium,
        High,
        Critical
    }

    public enum SubmissionType
    {
        Concern,
        Request,
        Suggestion
    }
} 