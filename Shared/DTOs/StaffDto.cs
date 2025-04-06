using System;
using System.ComponentModel.DataAnnotations;

namespace BlazorApp1.Shared.DTOs
{
    public class StaffDto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(100, ErrorMessage = "Name cannot be longer than 100 characters")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Position is required")]
        [StringLength(100, ErrorMessage = "Position cannot be longer than 100 characters")]
        public string Position { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Bio cannot be longer than 500 characters")]
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

    public class CreateStaffDto
    {
        [Required(ErrorMessage = "Name is required")]
        [StringLength(100, ErrorMessage = "Name cannot be longer than 100 characters")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Position is required")]
        [StringLength(100, ErrorMessage = "Position cannot be longer than 100 characters")]
        public string Position { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Bio cannot be longer than 500 characters")]
        public string? Bio { get; set; }

        [RegularExpression(@"^(https?:\/\/)?([\da-z\.-]+)\.([a-z\.]{2,6})([\/\w \.-]*)*\/?$", 
            ErrorMessage = "Please enter a valid URL for Facebook")]
        public string? FacebookUrl { get; set; }

        [RegularExpression(@"^(https?:\/\/)?([\da-z\.-]+)\.([a-z\.]{2,6})([\/\w \.-]*)*\/?$", 
            ErrorMessage = "Please enter a valid URL for Instagram")]
        public string? InstagramUrl { get; set; }
    }

    public class UpdateStaffDto : CreateStaffDto
    {
        public int Id { get; set; }
    }
} 