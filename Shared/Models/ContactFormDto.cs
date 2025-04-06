using System;
using BlazorApp1.Shared.Models;

namespace BlazorApp1.Shared.Models
{
    public class ContactFormDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public PriorityLevel? PriorityLevel { get; set; }
        public string? Category { get; set; }
        public string? Location { get; set; }
        public FormType FormType { get; set; }
        public bool IsRead { get; set; }
        public bool IsResolved { get; set; }
        public RequestType? RequestType { get; set; }
        public UrgencyLevel? UrgencyLevel { get; set; }
        public DateTime? PreferredDate { get; set; }
        public ConcernCategory? ConcernCategory { get; set; }
        public string? SuggestionCategory { get; set; }
    }
} 