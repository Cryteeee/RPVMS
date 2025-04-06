using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BlazorApp1.Shared.Models
{
    public class BillingDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [Required]
        [JsonPropertyName("userId")]
        public string UserId { get; set; }

        [JsonPropertyName("userName")]
        public string UserName { get; set; }

        [Required]
        [StringLength(200, MinimumLength = 5)]
        [JsonPropertyName("description")]
        public string Description { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("billDate")]
        public DateTime BillDate { get; set; }

        [Required]
        [JsonPropertyName("dueDate")]
        public DateTime DueDate { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = "Pending";

        [JsonPropertyName("isPaid")]
        public bool IsPaid { get; set; }

        [JsonPropertyName("paymentDate")]
        public DateTime? PaymentDate { get; set; }

        [Required]
        [JsonPropertyName("billType")]
        public string BillType { get; set; } = "Other";

        [JsonPropertyName("cubicMeter")]
        public decimal? CubicMeter { get; set; }

        [JsonPropertyName("previousCubicMeter")]
        public decimal? PreviousCubicMeter { get; set; }

        [JsonPropertyName("pricePerCubicMeter")]
        public decimal? PricePerCubicMeter { get; set; }

        [JsonPropertyName("orNumber")]
        public string? ORNumber { get; set; }
    }
} 