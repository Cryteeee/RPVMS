using System.Net.Http.Json;
using BlazorApp1.Shared.Models;
using BlazorApp1.Shared.Enums;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BlazorApp1.Client.Services
{
    public class SubmissionService : ISubmissionService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<SubmissionService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public SubmissionService(HttpClient httpClient, ILogger<SubmissionService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };
        }

        public async Task<List<SubmissionDto>> GetSubmissionsAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<SubmissionDto>>("api/submissions", _jsonOptions) ?? new List<SubmissionDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting submissions");
                throw;
            }
        }

        public async Task<SubmissionDto> GetSubmissionAsync(int id)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<SubmissionDto>($"api/submissions/{id}", _jsonOptions) ?? new SubmissionDto();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting submission {Id}", id);
                throw;
            }
        }

        public async Task<SubmissionDto> CreateSubmissionAsync(SubmissionDto submission)
        {
            try
            {
                _logger.LogInformation("Attempting to create submission: {Type}, {Title}", submission.Type, submission.Title);
                
                var response = await _httpClient.PostAsJsonAsync("api/submissions", submission, _jsonOptions);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to create submission. Status: {StatusCode}, Error: {Error}", 
                        response.StatusCode, errorContent);
                    
                    throw new HttpRequestException(
                        $"Failed to create submission. Status: {response.StatusCode}, Error: {errorContent}");
                }

                var result = await response.Content.ReadFromJsonAsync<SubmissionDto>(_jsonOptions);
                if (result == null)
                {
                    throw new InvalidOperationException("Received null response when creating submission");
                }

                _logger.LogInformation("Successfully created submission with ID: {Id}", result.Id);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating submission");
                throw;
            }
        }

        public async Task<List<SubmissionDto>> GetArchivedSubmissionsAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<SubmissionDto>>("api/submissions/archived", _jsonOptions) ?? new List<SubmissionDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting archived submissions");
                return new List<SubmissionDto>();
            }
        }

        public async Task<bool> RestoreSubmissionAsync(int id)
        {
            try
            {
                var response = await _httpClient.PutAsync($"api/submissions/{id}/restore", null);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring submission {Id}", id);
                return false;
            }
        }

        public async Task<bool> DeleteSubmissionAsync(int id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"api/submissions/{id}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting submission {Id}", id);
                return false;
            }
        }

        public async Task<bool> ArchiveSubmissionAsync(int id)
        {
            try
            {
                var response = await _httpClient.PutAsync($"api/submissions/{id}/archive", null);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error archiving submission {Id}", id);
                return false;
            }
        }

        public async Task<bool> UpdateStatusAsync(int id, SubmissionStatus newStatus)
        {
            try
            {
                _logger.LogInformation("Updating submission {Id} status to {Status}", id, newStatus);

                var response = await _httpClient.PutAsJsonAsync($"api/submissions/{id}/status", newStatus);
                
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
                
                // If the update failed and it's not a 404, log the error
                if (response.StatusCode != System.Net.HttpStatusCode.NotFound)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to update submission status. Status: {StatusCode}, Error: {Error}", 
                        response.StatusCode, errorContent);
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating status for submission {Id}", id);
                return false;
            }
        }
    }
} 