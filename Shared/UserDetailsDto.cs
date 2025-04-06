using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BlazorApp1.Shared
{
    [JsonSerializable(typeof(UserDetailsDto))]
    public class UserDetailsDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        
        [JsonPropertyName("userId")]
        public int UserId { get; set; }
        
        [JsonPropertyName("gender")]
        public string? Gender { get; set; }
        
        [JsonPropertyName("nationality")]
        public string? Nationality { get; set; }
        
        [JsonPropertyName("dateOfBirth")]
        [JsonConverter(typeof(JsonDateTimeConverter))]
        public DateTime? DateOfBirth { get; set; }

        [JsonPropertyName("fullName")]
        public string? FullName { get; set; }

        [JsonPropertyName("address")]
        public string? Address { get; set; }

        [JsonPropertyName("maritalStatus")]
        public string? MaritalStatus { get; set; }

        [JsonPropertyName("contactNumber")]
        [Phone(ErrorMessage = "Invalid phone number format")]
        [StringLength(11, ErrorMessage = "Contact number must be exactly 11 digits")]
        [RegularExpression(@"^09\d{9}$", ErrorMessage = "Contact number must start with '09' and contain exactly 11 digits")]
        public string? ContactNumber { get; set; }

        // Photo-related properties
        [JsonPropertyName("photoFileName")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? PhotoFileName { get; set; }
        
        [JsonPropertyName("photoUrl")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? PhotoUrl { get; set; }
        
        [JsonPropertyName("photoContentType")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? PhotoContentType { get; set; }
        
        [JsonPropertyName("photoBase64")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? PhotoBase64 { get; set; }

        public UserDetailsDto()
        {
            Gender = string.Empty;
            Nationality = string.Empty;
            DateOfBirth = DateTime.Today.AddYears(-18);
        }
    }

    public class JsonDateTimeConverter : JsonConverter<DateTime>
    {
        private const string DateFormat = "yyyy-MM-dd";

        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var dateString = reader.GetString();
            return DateTime.TryParse(dateString, out var result) ? result : DateTime.Today.AddYears(-18);
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString(DateFormat));
        }
    }
}

