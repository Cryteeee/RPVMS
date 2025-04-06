using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using BlazorApp1.Shared.Enums;

namespace BlazorApp1.Shared.Models
{
    public class BoardMessage
    {
        [Key]
        public int MessageId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(1000)]
        public string Content { get; set; } = string.Empty;

        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [Required]
        public bool IsRead { get; set; } = true;

        [Required]
        public bool IsDelivered { get; set; } = false;

        [Required]
        public MessagePriority Priority { get; set; } = MessagePriority.Normal;

        [Required]
        public string Type { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        [JsonIgnore]
        public virtual User User { get; set; } = null!;

        [JsonIgnore]
        public virtual ICollection<MessageRead> MessageReads { get; set; } = new List<MessageRead>();
    }
} 