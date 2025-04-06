using System;

namespace BlazorApp1.Server.Models
{
    public class Users
    {
        public string? PhotoUrl { get; set; }
        public string? PhotoFileName { get; set; }
        public bool IsEmailVerified { get; set; }
        public string? EmailVerificationToken { get; set; }
        public DateTime? EmailVerificationTokenExpiry { get; set; }
    }
} 