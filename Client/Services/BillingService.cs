using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using BlazorApp1.Shared;
using BlazorApp1.Shared.Models;
using Microsoft.Extensions.Logging;

namespace BlazorApp1.Client.Services
{
    public class BillingService : IBillingService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly ILogger<BillingService> _logger;

        public BillingService(IHttpClientFactory httpClientFactory, ILogger<BillingService> logger)
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

        public async Task<Response<List<BillingDto>>> GetUserBillsAsync(string userId)
        {
            try
            {
                _logger.LogInformation($"Getting bills for user {userId}");
                var httpClient = _httpClientFactory.CreateClient("ManagementSystem");
                var response = await httpClient.GetAsync($"api/Billing/user/{userId}");

                _logger.LogInformation($"Response status code: {response.StatusCode}");
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning($"Failed to get user bills. Status code: {response.StatusCode}, Error: {errorContent}");
                    return new Response<List<BillingDto>> { IsSuccess = false, Message = $"Failed to retrieve bills: {errorContent}" };
                }

                var content = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Received response content: {content}");
                var result = JsonSerializer.Deserialize<Response<List<BillingDto>>>(content, _jsonOptions);
                if (result?.Data != null)
                {
                    _logger.LogInformation($"Successfully retrieved {result.Data.Count} bills");
                }
                return result ?? new Response<List<BillingDto>> { IsSuccess = false, Message = "Failed to deserialize response" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user bills");
                return new Response<List<BillingDto>> { IsSuccess = false, Message = $"Error: {ex.Message}" };
            }
        }

        public async Task<Response> AddBillAsync(BillingDto bill)
        {
            try
            {
                _logger.LogInformation($"Adding bill for user {bill.UserId}: Amount={bill.Amount}, Description={bill.Description}");
                var httpClient = _httpClientFactory.CreateClient("ManagementSystem");
                var response = await httpClient.PostAsJsonAsync("api/Billing", bill);

                _logger.LogInformation($"Response status code: {response.StatusCode}");
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning($"Failed to add bill. Status code: {response.StatusCode}, Error: {errorContent}");
                    return new Response { IsSuccess = false, Message = $"Failed to add bill: {errorContent}" };
                }

                var content = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Received response content: {content}");
                var result = JsonSerializer.Deserialize<Response>(content, _jsonOptions);
                return result ?? new Response { IsSuccess = false, Message = "Failed to deserialize response" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding bill");
                return new Response { IsSuccess = false, Message = $"Error: {ex.Message}" };
            }
        }

        public async Task<Response> MarkBillAsPaidAsync(int billId, string orNumber)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient("ManagementSystem");
                var response = await httpClient.PutAsJsonAsync($"api/Billing/{billId}/pay", new { ORNumber = orNumber });

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning($"Failed to mark bill as paid. Status code: {response.StatusCode}, Error: {errorContent}");
                    return new Response { IsSuccess = false, Message = "Failed to mark bill as paid" };
                }

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<Response>(content, _jsonOptions);
                return result ?? new Response { IsSuccess = false, Message = "Failed to deserialize response" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking bill as paid");
                return new Response { IsSuccess = false, Message = ex.Message };
            }
        }

        public async Task<Response<BillingDto>> UpdateBillAsync(BillingDto bill)
        {
            try
            {
                var response = await _httpClientFactory.CreateClient("ManagementSystem").PutAsJsonAsync($"api/Billing/{bill.Id}", bill);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<Response<BillingDto>>();
                    return result ?? new Response<BillingDto> { IsSuccess = false, Message = "Failed to deserialize response" };
                }
                return new Response<BillingDto> { IsSuccess = false, Message = "Failed to update bill" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating bill");
                return new Response<BillingDto> { IsSuccess = false, Message = ex.Message };
            }
        }

        public async Task<Response> DeleteBillAsync(int billId)
        {
            try
            {
                var response = await _httpClientFactory.CreateClient("ManagementSystem").DeleteAsync($"api/Billing/{billId}");
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<Response>();
                    return result ?? new Response { IsSuccess = false, Message = "Failed to deserialize response" };
                }
                return new Response { IsSuccess = false, Message = "Failed to delete bill" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting bill");
                return new Response { IsSuccess = false, Message = ex.Message };
            }
        }

        public async Task<Response> CompressBillsAsync(string userId)
        {
            try
            {
                _logger.LogInformation($"Compressing bills for user {userId}");
                var httpClient = _httpClientFactory.CreateClient("ManagementSystem");
                var response = await httpClient.PostAsync($"api/Billing/compress/{userId}", null);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning($"Failed to compress bills. Status code: {response.StatusCode}, Error: {errorContent}");
                    return new Response { IsSuccess = false, Message = $"Failed to compress bills: {errorContent}" };
                }

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<Response>(content, _jsonOptions);
                return result ?? new Response { IsSuccess = false, Message = "Failed to deserialize response" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error compressing bills");
                return new Response { IsSuccess = false, Message = ex.Message };
            }
        }

        public async Task<Response> DeleteAllBillsAsync(string userId)
        {
            try
            {
                _logger.LogInformation($"Deleting all bills for user {userId}");
                var httpClient = _httpClientFactory.CreateClient("ManagementSystem");
                var response = await httpClient.DeleteAsync($"api/Billing/all/{userId}");

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning($"Failed to delete all bills. Status code: {response.StatusCode}, Error: {errorContent}");
                    return new Response { IsSuccess = false, Message = $"Failed to delete all bills: {errorContent}" };
                }

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<Response>(content, _jsonOptions);
                return result ?? new Response { IsSuccess = false, Message = "Failed to deserialize response" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting all bills");
                return new Response { IsSuccess = false, Message = ex.Message };
            }
        }

        public async Task<Response> SetCubicMeterPriceAsync(string userId, decimal pricePerCubicMeter)
        {
            var request = new { UserId = userId, PricePerCubicMeter = pricePerCubicMeter };
            var response = await _httpClientFactory.CreateClient("ManagementSystem").PostAsJsonAsync("api/Billing/setcubicprice", request);
            return await response.Content.ReadFromJsonAsync<Response>();
        }

        public async Task<Response> SetCubicNumberAsync(string userId, decimal cubicNumber, DateTime dueDate)
        {
            var request = new { UserId = userId, CubicNumber = cubicNumber, DueDate = dueDate };
            var response = await _httpClientFactory.CreateClient("ManagementSystem").PostAsJsonAsync("api/Billing/setcubicnumber", request);
            return await response.Content.ReadFromJsonAsync<Response>();
        }

        public async Task<Response> SetMonthlyPriceAsync(string userId, decimal monthlyPrice)
        {
            try
            {
                _logger.LogInformation($"Setting monthly price for user {userId}: {monthlyPrice}");
                var request = new { UserId = userId, MonthlyPrice = monthlyPrice };
                var httpClient = _httpClientFactory.CreateClient("ManagementSystem");
                var response = await httpClient.PostAsJsonAsync("api/Billing/setmonthlyprice", request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning($"Failed to set monthly price. Status code: {response.StatusCode}, Error: {errorContent}");
                    return new Response { IsSuccess = false, Message = $"Failed to set monthly price: {errorContent}" };
                }

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<Response>(content, _jsonOptions);
                return result ?? new Response { IsSuccess = false, Message = "Failed to deserialize response" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting monthly price");
                return new Response { IsSuccess = false, Message = $"Error: {ex.Message}" };
            }
        }

        public async Task<byte[]> DownloadBillsAsync(string userId)
        {
            try
            {
                _logger.LogInformation($"Downloading bills for user {userId}");
                var httpClient = _httpClientFactory.CreateClient("ManagementSystem");
                var response = await httpClient.GetAsync($"api/Billing/download/{userId}");

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning($"Failed to download bills. Status code: {response.StatusCode}, Error: {errorContent}");
                    throw new Exception($"Failed to download bills: {errorContent}");
                }

                return await response.Content.ReadAsByteArrayAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading bills");
                throw;
            }
        }

        public async Task<byte[]> DownloadAllBillsAsync()
        {
            try
            {
                _logger.LogInformation("Downloading all bills");
                var httpClient = _httpClientFactory.CreateClient("ManagementSystem");
                var response = await httpClient.GetAsync("api/Billing/downloadall");

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning($"Failed to download all bills. Status code: {response.StatusCode}, Error: {errorContent}");
                    throw new Exception($"Failed to download all bills: {errorContent}");
                }

                return await response.Content.ReadAsByteArrayAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading all bills");
                throw;
            }
        }
    }
} 