using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using BlazorApp1.Shared.Models;

namespace BlazorApp1.Server.Data
{
    public class Bill
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(200)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string BillType { get; set; } = "Other";

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        public DateTime BillDate { get; set; }

        [Required]
        public DateTime DueDate { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Pending";

        public bool IsPaid { get; set; }

        public DateTime? PaymentDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? CubicMeter { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? PreviousCubicMeter { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? PricePerCubicMeter { get; set; }

        [StringLength(50)]
        public string? ORNumber { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual User User { get; set; } = null!;
    }
} 