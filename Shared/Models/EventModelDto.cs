using System;
using System.ComponentModel.DataAnnotations;

namespace BlazorApp1.Shared.Models
{
    public class EventModelDto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; }

        public string ImageUrl { get; set; }

        [Required(ErrorMessage = "Event date is required")]
        public DateTime EventDate { get; set; }

        [Required(ErrorMessage = "Expiry date is required")]
        [CustomValidation(typeof(EventModelDto), "ValidateExpiryDate")]
        public DateTime ExpiryDate { get; set; }

        public DateTime CreatedDate { get; set; }

        public string CreatedBy { get; set; }

        [Required(ErrorMessage = "User role is required")]
        [StringLength(50)]
        public string UserRole { get; set; }

        [Required(ErrorMessage = "Status is required")]
        [StringLength(50)]
        public string Status { get; set; }

        [StringLength(200)]
        public string Location { get; set; }

        [StringLength(50)]
        public string EventType { get; set; }

        public bool IsActive { get; set; }

        // Custom validation for expiry date
        public static ValidationResult ValidateExpiryDate(DateTime expiryDate, ValidationContext context)
        {
            var instance = (EventModelDto)context.ObjectInstance;
            
            if (expiryDate <= instance.EventDate)
            {
                return new ValidationResult("Expiry date must be after the event date");
            }

            if (expiryDate <= DateTime.Now)
            {
                return new ValidationResult("Expiry date must be in the future");
            }

            return ValidationResult.Success;
        }
    }
} 