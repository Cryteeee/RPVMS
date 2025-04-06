using System;
using System.ComponentModel.DataAnnotations;

namespace BlazorApp1.Shared.Models
{
    public class SubmissionTracking
    {
        public int Id { get; set; }
        
        [Required]
        public string ClientIP { get; set; } = string.Empty;
        
        public DateTime SubmissionTime { get; set; }
        
        public bool IsBanned { get; set; }
        
        public DateTime? BanEndTime { get; set; }
    }
} 