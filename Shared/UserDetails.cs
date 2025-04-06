using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using BlazorApp1.Shared.Models;

namespace BlazorApp1.Shared.Models
{
    public class UserDetails
    {
        [Key]
        [ForeignKey("User")]
        public int UserId { get; set; }

        public string? Gender { get; set; }

        public string? Nationality { get; set; }

        public DateTime? DateOfBirth { get; set; }

        public string? PhotoFileName { get; set; }

        public string? PhotoUrl { get; set; }

        public string? PhotoContentType { get; set; }

        public string? FullName { get; set; }

        public string? Address { get; set; }

        public string? MaritalStatus { get; set; }

        [Phone(ErrorMessage = "Invalid phone number format")]
        [StringLength(11, ErrorMessage = "Contact number must be exactly 11 digits")]
        [RegularExpression(@"^09\d{9}$", ErrorMessage = "Contact number must start with '09' and contain exactly 11 digits")]
        [Column(TypeName = "nvarchar(20)")]
        public string? ContactNumber { get; set; }

        [JsonIgnore]
        public virtual User User { get; set; } = null!;
    }
}
