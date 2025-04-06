using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BlazorApp1.Shared.Models;

namespace BlazorApp1.Server.Models
{
    public class CubicMeterPriceSetting
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PricePerCubicMeter { get; set; }

        public DateTime LastUpdated { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual User User { get; set; }
    }
} 