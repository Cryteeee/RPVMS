using BlazorApp1.Shared.Models;
using System.ComponentModel.DataAnnotations;

namespace BlazorApp1.Shared
{
    public class ContactFormDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Location { get; set; }
        public string Category { get; set; }
        public FormType FormType { get; set; }
        public UrgencyLevel UrgencyLevel { get; set; }
        public PriorityLevel PriorityLevel { get; set; }
        public DateTime PreferredDate { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public bool IsRead { get; set; }
        public ConcernCategory? ConcernCategory { get; set; }
        public RequestType? RequestType { get; set; }
    }
}