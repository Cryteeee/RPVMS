using System.ComponentModel.DataAnnotations;

namespace BlazorApp1.Shared.Models
{
    public class EventListDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public DateTime EventDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public string Status { get; set; }
        public string Location { get; set; }
        public string EventType { get; set; }
        public string CreatedBy { get; set; }
        public string UserRole { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateEventDto
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [Required]
        public string Description { get; set; }

        public byte[] ImageContent { get; set; }
        public string ImageFileName { get; set; }
        public string ImageContentType { get; set; }

        [Required]
        public DateTime EventDate { get; set; }

        [Required]
        public DateTime ExpiryDate { get; set; }

        [Required]
        [StringLength(200)]
        public string Location { get; set; }

        [Required]
        [StringLength(50)]
        public string EventType { get; set; }
    }

    public class UpdateEventDto
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [Required]
        public string Description { get; set; }

        public byte[] ImageContent { get; set; }
        public string ImageFileName { get; set; }
        public string ImageContentType { get; set; }

        [Required]
        public DateTime EventDate { get; set; }

        [Required]
        public DateTime ExpiryDate { get; set; }

        [Required]
        [StringLength(200)]
        public string Location { get; set; }

        [Required]
        [StringLength(50)]
        public string EventType { get; set; }

        public bool IsActive { get; set; }
    }
} 