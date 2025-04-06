using System.Text.Json.Serialization;

namespace BlazorApp1.Shared
{
    public class Response
    {
        [JsonPropertyName("isSuccess")]
        public bool IsSuccess { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("data")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object? Data { get; set; }

        public Response() { }

        public Response(bool isSuccess, string? message = null, object? data = null)
        {
            IsSuccess = isSuccess;
            Message = message;
            Data = data;
        }
    }

    public class Response<T>
    {
        [JsonPropertyName("isSuccess")]
        public bool IsSuccess { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("data")]
        public T? Data { get; set; }

        public Response() { }

        public Response(bool isSuccess, string? message = null, T? data = default)
        {
            IsSuccess = isSuccess;
            Message = message;
            Data = data;
        }

        public static implicit operator Response(Response<T> response)
        {
            return new Response
            {
                IsSuccess = response.IsSuccess,
                Message = response.Message,
                Data = response.Data
            };
        }
    }
}

