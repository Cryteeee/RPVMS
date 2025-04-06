using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using BlazorApp1.Shared.Models;

namespace BlazorApp1.Server.Data
{
    public class CubicMeterReading
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Reading { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PricePerCubicMeter { get; set; }

        [Required]
        public DateTime ReadingDate { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual User User { get; set; } = null!;
    }
} 