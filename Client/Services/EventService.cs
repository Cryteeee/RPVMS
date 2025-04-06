using System.Net.Http.Json;
using BlazorApp1.Shared.Models;

namespace BlazorApp1.Client.Services
{
    public class EventService : IEventService
    {
        private readonly HttpClient _httpClient;

        public EventService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<EventListDto>> GetAllEventsAsync()
        {
            return await _httpClient.GetFromJsonAsync<List<EventListDto>>("api/event") ?? new List<EventListDto>();
        }

        public async Task<List<EventListDto>> GetActiveEventsAsync()
        {
            return await _httpClient.GetFromJsonAsync<List<EventListDto>>("api/event/active") ?? new List<EventListDto>();
        }

        public async Task<EventPlan> GetEventByIdAsync(int id)
        {
            return await _httpClient.GetFromJsonAsync<EventPlan>($"api/event/{id}") 
                ?? throw new Exception("Event not found");
        }

        public async Task<EventPlan> CreateEventAsync(CreateEventDto createEventDto)
        {
            var response = await _httpClient.PostAsJsonAsync("api/event", createEventDto);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<EventPlan>() 
                ?? throw new Exception("Failed to create event");
        }

        public async Task<EventPlan> UpdateEventAsync(UpdateEventDto updateEventDto)
        {
            var response = await _httpClient.PutAsJsonAsync($"api/event/{updateEventDto.Id}", updateEventDto);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<EventPlan>() 
                ?? throw new Exception("Failed to update event");
        }

        public async Task<bool> DeleteEventAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"api/event/{id}");
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateEventStatusAsync(int id, string status)
        {
            var response = await _httpClient.PutAsJsonAsync($"api/event/{id}/status", status);
            return response.IsSuccessStatusCode;
        }

        public async Task CheckExpiredEventsAsync()
        {
            var response = await _httpClient.PostAsync("api/event/check-expired", null);
            response.EnsureSuccessStatusCode();
        }
    }
} 