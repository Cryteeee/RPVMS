using System.ComponentModel.DataAnnotations;
using BlazorApp1.Shared.Enums;

namespace BlazorApp1.Server.Data
{
    public class Notification
    {
        [Key]
        public int Id { get; set; }

        public int? UserId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [Required]
        [StringLength(1000)]
        public string Message { get; set; }

        [Required]
        public NotificationType Type { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        public bool IsRead { get; set; }

        public string ReferenceId { get; set; }

        public Notification()
        {
            CreatedAt = DateTime.UtcNow;
            IsRead = false;
        }
    }
} 