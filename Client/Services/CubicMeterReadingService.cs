using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using BlazorApp1.Shared;
using BlazorApp1.Shared.Models;
using Microsoft.Extensions.Logging;

namespace BlazorApp1.Client.Services
{
    public class CubicMeterReadingService : ICubicMeterReadingService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly ILogger<CubicMeterReadingService> _logger;

        public CubicMeterReadingService(IHttpClientFactory httpClientFactory, ILogger<CubicMeterReadingService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Converters = { new JsonStringEnumConverter() }
            };
        }

        public async Task<Response<List<CubicMeterReadingDto>>> GetUserReadingsAsync(string userId)
        {
            try
            {
                _logger.LogInformation($"Getting cubic meter readings for user {userId}");
                var httpClient = _httpClientFactory.CreateClient("ManagementSystem");
                var response = await httpClient.GetAsync($"api/CubicMeterReading/user/{userId}");

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning($"Failed to get user readings. Status code: {response.StatusCode}, Error: {errorContent}");
                    return new Response<List<CubicMeterReadingDto>> { IsSuccess = false, Message = $"Failed to retrieve readings: {errorContent}" };
                }

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<Response<List<CubicMeterReadingDto>>>(content, _jsonOptions);
                return result ?? new Response<List<CubicMeterReadingDto>> { IsSuccess = false, Message = "Failed to deserialize response" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user readings");
                return new Response<List<CubicMeterReadingDto>> { IsSuccess = false, Message = $"Error: {ex.Message}" };
            }
        }

        public async Task<Response<CubicMeterReadingDto>> AddReadingAsync(CubicMeterReadingDto reading)
        {
            try
            {
                _logger.LogInformation($"Adding cubic meter reading for user {reading.UserId}");
                var httpClient = _httpClientFactory.CreateClient("ManagementSystem");
                var response = await httpClient.PostAsJsonAsync("api/CubicMeterReading", reading);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning($"Failed to add reading. Status code: {response.StatusCode}, Error: {errorContent}");
                    return new Response<CubicMeterReadingDto> { IsSuccess = false, Message = $"Failed to add reading: {errorContent}" };
                }

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<Response<CubicMeterReadingDto>>(content, _jsonOptions);
                return result ?? new Response<CubicMeterReadingDto> { IsSuccess = false, Message = "Failed to deserialize response" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding reading");
                return new Response<CubicMeterReadingDto> { IsSuccess = false, Message = $"Error: {ex.Message}" };
            }
        }

        public async Task<Response<CubicMeterReadingDto>> GetLatestReadingAsync(string userId)
        {
            try
            {
                _logger.LogInformation($"Getting latest cubic meter reading for user {userId}");
                var httpClient = _httpClientFactory.CreateClient("ManagementSystem");
                var response = await httpClient.GetAsync($"api/CubicMeterReading/latest/{userId}");

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning($"Failed to get latest reading. Status code: {response.StatusCode}, Error: {errorContent}");
                    return new Response<CubicMeterReadingDto> { IsSuccess = false, Message = $"Failed to retrieve latest reading: {errorContent}" };
                }

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<Response<CubicMeterReadingDto>>(content, _jsonOptions);
                return result ?? new Response<CubicMeterReadingDto> { IsSuccess = false, Message = "Failed to deserialize response" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving latest reading");
                return new Response<CubicMeterReadingDto> { IsSuccess = false, Message = $"Error: {ex.Message}" };
            }
        }
    }
} 