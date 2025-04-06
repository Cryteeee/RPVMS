using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlazorApp1.Server.Data
{
    public class UserDetails
    {
        [Key]
        public int UserId { get; set; }
        
        public string? Gender { get; set; }
        public string? Nationality { get; set; }
        public DateTime? DateOfBirth { get; set; }
        
        // Photo-related properties
        public string? PhotoFileName { get; set; }
        public string? PhotoUrl { get; set; }
        public string? PhotoContentType { get; set; }

        public string? FullName { get; set; }
        public string? Address { get; set; }
        public string? MaritalStatus { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual User User { get; set; } = null!;
    }
} 