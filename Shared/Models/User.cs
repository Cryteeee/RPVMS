using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;

namespace BlazorApp1.Shared.Models
{
    public class User : IdentityUser<int>
    {
        public User() : base()
        {
        }

        public User(string userName) : base(userName)
        {
        }

        [Required]
        [StringLength(50)]
        public string Role { get; set; } = "User";

        public bool IsEmailVerified { get; set; }
        
        public string? EmailVerificationToken { get; set; }
        
        public DateTime? EmailVerificationTokenExpiry { get; set; }

        public DateTime? LastEmailSentTime { get; set; }

        // Password Reset Fields
        public string? PasswordResetToken { get; set; }
        public DateTime? PasswordResetTokenExpiry { get; set; }
        public DateTime? LastPasswordResetEmailSentTime { get; set; }

        // User Activity Fields
        public bool IsActive { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }

        // Navigation property
        public virtual UserDetails? UserDetails { get; set; }

        [JsonIgnore]
        public virtual ICollection<MessageRead> MessageReads { get; set; } = new List<MessageRead>();
    }
} 