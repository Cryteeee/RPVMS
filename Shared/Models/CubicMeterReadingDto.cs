using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BlazorApp1.Shared.Models
{
    public class CubicMeterReadingDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [Required]
        [JsonPropertyName("userId")]
        public string UserId { get; set; }

        [JsonPropertyName("userName")]
        public string UserName { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Reading must be greater than 0")]
        [JsonPropertyName("reading")]
        public decimal Reading { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price per cubic meter must be greater than 0")]
        [JsonPropertyName("pricePerCubicMeter")]
        public decimal PricePerCubicMeter { get; set; }

        [JsonPropertyName("readingDate")]
        public DateTime ReadingDate { get; set; }
    }
} 