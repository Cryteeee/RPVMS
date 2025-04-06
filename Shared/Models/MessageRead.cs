using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace BlazorApp1.Shared.Models
{
    public class MessageRead
    {
        [Required]
        public int MessageId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public bool IsRead { get; set; }
        
        [ForeignKey("MessageId")]
        [JsonIgnore]
        public virtual BoardMessage Message { get; set; } = null!;

        [ForeignKey("UserId")]
        [JsonIgnore]
        public virtual User User { get; set; } = null!;
    }
} 