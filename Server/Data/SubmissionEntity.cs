using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BlazorApp1.Shared.Enums;
using BlazorApp1.Shared.Models;

namespace BlazorApp1.Server.Data
{
    [Table("Submissions")]
    public class SubmissionEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(200)]
        public string Subject { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [Required]
        [StringLength(2000)]
        public string Description { get; set; }

        [Required]
        [StringLength(50)]
        public string Type { get; set; }

        [Required]
        [StringLength(50)]
        public string Priority { get; set; }

        [Required]
        public SubmissionStatus Status { get; set; }

        [StringLength(100)]
        public string Category { get; set; }

        [StringLength(200)]
        public string Location { get; set; }

        public DateTime? PreferredDate { get; set; }

        [Required]
        public DateTime SubmittedDate { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastUpdated { get; set; }

        [StringLength(1000)]
        public string Attachments { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        public SubmissionEntity()
        {
            Status = SubmissionStatus.Pending;
        }
    }
} 