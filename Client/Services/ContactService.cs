using BlazorApp1.Shared.Models;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace BlazorApp1.Client.Services
{
    public class ContactService : IContactService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl = "api/contact";
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly ILogger<ContactService> _logger;

        public ContactService(HttpClient httpClient, ILogger<ContactService> logger)
        {
            _httpClient = httpClient;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            };
            _logger = logger;
        }

        public async Task<IEnumerable<ConcernViewModel>> GetConcernsAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<IEnumerable<ConcernViewModel>>($"{_baseUrl}/concerns", _jsonOptions) 
                    ?? Enumerable.Empty<ConcernViewModel>();
            }
            catch (JsonException ex)
            {
                Console.Error.WriteLine($"JSON deserialization error: {ex.Message}");
                return Enumerable.Empty<ConcernViewModel>();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error getting concerns: {ex.Message}");
                return Enumerable.Empty<ConcernViewModel>();
            }
        }

        public async Task<ContactForm?> GetContactFormByIdAsync(int? id)
        {
            try
            {
                if (!id.HasValue)
                {
                    return null;
                }
                return await _httpClient.GetFromJsonAsync<ContactForm>($"{_baseUrl}/{id.Value}", _jsonOptions);
            }
            catch (JsonException ex)
            {
                Console.Error.WriteLine($"JSON deserialization error: {ex.Message}");
                throw;
            }
        }

        public async Task<ContactForm> CreateContactFormAsync(ContactForm contactForm)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/create", contactForm, _jsonOptions);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<ContactForm>(_jsonOptions);
            }
            catch (JsonException ex)
            {
                Console.Error.WriteLine($"JSON serialization error: {ex.Message}");
                throw;
            }
        }

        public async Task UpdateContactFormAsync(ContactForm contactForm)
        {
            try
            {
                // First get the existing form to ensure we have all required fields
                var existingForm = await GetContactFormByIdAsync(contactForm.Id);
                if (existingForm == null)
                {
                    throw new KeyNotFoundException($"Contact form not found with ID: {contactForm.Id}");
                }

                // Create a ConcernViewModel with all necessary fields from the existing form
                var concernVM = new ConcernViewModel
                {
                    Id = existingForm.Id,
                    Title = existingForm.Title,
                    Description = existingForm.Description,
                    UserEmail = existingForm.Email,
                    DateSubmitted = existingForm.CreatedAt,
                    PriorityLevel = existingForm.PriorityLevel,
                    Category = existingForm.Category,
                    Location = existingForm.Location,
                    FormType = existingForm.FormType,
                    RequestType = existingForm.RequestType,
                    UrgencyLevel = existingForm.UrgencyLevel,
                    PreferredDate = existingForm.PreferredDate,
                    ConcernCategory = existingForm.ConcernCategory,
                    // Update with the new status fields
                    IsRead = contactForm.IsRead,
                    IsResolved = contactForm.IsResolved,
                    Status = contactForm.Status
                };

                // Use the correct endpoint for status updates
                var response = await _httpClient.PutAsJsonAsync($"{_baseUrl}/{contactForm.Id}/status", concernVM, _jsonOptions);
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"Failed to update contact form: {error}");
                }
            }
            catch (JsonException ex)
            {
                Console.Error.WriteLine($"JSON serialization error: {ex.Message}");
                throw;
            }
            catch (HttpRequestException ex)
            {
                Console.Error.WriteLine($"HTTP request error: {ex.Message}");
                throw;
            }
        }

        public async Task<IEnumerable<ContactForm>> GetAllContactFormsAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<IEnumerable<ContactForm>>(_baseUrl, _jsonOptions) 
                    ?? Enumerable.Empty<ContactForm>();
            }
            catch (JsonException ex)
            {
                Console.Error.WriteLine($"JSON deserialization error: {ex.Message}");
                return Enumerable.Empty<ContactForm>();
            }
        }

        public async Task<bool> DeleteContactFormAsync(int? id)
        {
            try
            {
                if (!id.HasValue)
                {
                    return false;
                }
                var response = await _httpClient.DeleteAsync($"{_baseUrl}/{id.Value}");
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Console.Error.WriteLine($"Error deleting contact form: {error}");
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error deleting contact form: {ex.Message}");
                throw; // Rethrow to handle in the component
            }
        }

        public async Task<List<SubmissionDto>> GetArchivedFormsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/contact/archived");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<SubmissionDto>>(_jsonOptions) ?? new List<SubmissionDto>();
                }
                _logger.LogError("Failed to get archived forms. Status code: {StatusCode}", response.StatusCode);
                return new List<SubmissionDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting archived forms");
                return new List<SubmissionDto>();
            }
        }

        public async Task<bool> RestoreFormAsync(int id)
        {
            try
            {
                var response = await _httpClient.PutAsync($"api/contact/{id}/restore", null);
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
                _logger.LogError("Failed to restore form {Id}. Status code: {StatusCode}", id, response.StatusCode);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring form {Id}", id);
                return false;
            }
        }

        public async Task<bool> DeleteFormAsync(int id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"api/contact/{id}");
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
                _logger.LogError("Failed to delete form {Id}. Status code: {StatusCode}", id, response.StatusCode);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting form {Id}", id);
                return false;
            }
        }

        public async Task<bool> ArchiveFormAsync(int id)
        {
            try
            {
                var response = await _httpClient.PostAsync($"api/contact/{id}/archive", null);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error archiving contact form with ID {id}");
                return false;
            }
        }
    }
} 