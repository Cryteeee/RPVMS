using BlazorApp1.Shared.Models;

namespace BlazorApp1.Client.Services
{
    public interface IEventService
    {
        Task<List<EventListDto>> GetAllEventsAsync();
        Task<List<EventListDto>> GetActiveEventsAsync();
        Task<EventPlan> GetEventByIdAsync(int id);
        Task<EventPlan> CreateEventAsync(CreateEventDto createEventDto);
        Task<EventPlan> UpdateEventAsync(UpdateEventDto updateEventDto);
        Task<bool> DeleteEventAsync(int id);
        Task<bool> UpdateEventStatusAsync(int id, string status);
        Task CheckExpiredEventsAsync();
    }
} 