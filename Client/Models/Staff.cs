using System;
using System.ComponentModel.DataAnnotations;

namespace BlazorApp1.Client.Models
{
    public class Staff
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Position { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Bio { get; set; }

        public string? ImageUrl { get; set; }

        [RegularExpression(@"^(https?:\/\/)?([\da-z\.-]+)\.([a-z\.]{2,6})([\/\w \.-]*)*\/?$", 
            ErrorMessage = "Please enter a valid URL for Facebook")]
        public string? FacebookUrl { get; set; }

        [RegularExpression(@"^(https?:\/\/)?([\da-z\.-]+)\.([a-z\.]{2,6})([\/\w \.-]*)*\/?$", 
            ErrorMessage = "Please enter a valid URL for Instagram")]
        public string? InstagramUrl { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
} 