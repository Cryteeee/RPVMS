using System;
using System.ComponentModel.DataAnnotations;

namespace BlazorApp1.Shared.Models
{
    public class EventPlan
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public string ImageUrl { get; set; }

        [Required]
        public DateTime EventDate { get; set; }

        [Required]
        public DateTime ExpiryDate { get; set; }

        [Required]
        public DateTime CreatedDate { get; set; }

        [Required]
        public string CreatedBy { get; set; }

        [Required]
        [StringLength(50)]
        public string UserRole { get; set; }  // SuperAdmin, Admin, or User

        [Required]
        [StringLength(50)]
        public string Status { get; set; }  // Active, Expired, Cancelled

        [StringLength(200)]
        public string Location { get; set; }

        [StringLength(50)]
        public string EventType { get; set; }

        public bool IsActive { get; set; }

        public int? MaxAttendees { get; set; }

        public string ImageFileName { get; set; }

        public string ImageContentType { get; set; }
    }
} 