using BlazorApp1.Server.Data;
using BlazorApp1.Shared.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BlazorApp1.Server.Services
{
    public interface IEventService
    {
        Task<List<EventListDto>> GetAllEventsAsync();
        Task<List<EventListDto>> GetActiveEventsAsync();
        Task<EventPlan> GetEventByIdAsync(int id);
        Task<EventPlan> CreateEventAsync(CreateEventDto createEventDto, string userId, string userRole);
        Task<EventPlan> UpdateEventAsync(UpdateEventDto updateEventDto);
        Task<bool> DeleteEventAsync(int id);
        Task<bool> UpdateEventStatusAsync(int id, string status);
        Task CheckAndUpdateExpiredEvents();
    }

    public class EventService : IEventService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public EventService(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public async Task<List<EventListDto>> GetAllEventsAsync()
        {
            return await _context.EventPlans
                .Select(e => new EventListDto
                {
                    Id = e.Id,
                    Title = e.Title,
                    Description = e.Description,
                    ImageUrl = e.ImageUrl,
                    EventDate = e.EventDate,
                    ExpiryDate = e.ExpiryDate,
                    Status = e.Status,
                    Location = e.Location,
                    EventType = e.EventType,
                    CreatedBy = e.CreatedBy,
                    UserRole = e.UserRole,
                    IsActive = e.IsActive
                })
                .OrderByDescending(e => e.EventDate)
                .ToListAsync();
        }

        public async Task<List<EventListDto>> GetActiveEventsAsync()
        {
            return await _context.EventPlans
                .Where(e => e.IsActive && e.ExpiryDate > DateTime.UtcNow)
                .Select(e => new EventListDto
                {
                    Id = e.Id,
                    Title = e.Title,
                    Description = e.Description,
                    ImageUrl = e.ImageUrl,
                    EventDate = e.EventDate,
                    ExpiryDate = e.ExpiryDate,
                    Status = e.Status,
                    Location = e.Location,
                    EventType = e.EventType,
                    CreatedBy = e.CreatedBy,
                    UserRole = e.UserRole,
                    IsActive = e.IsActive
                })
                .OrderByDescending(e => e.EventDate)
                .ToListAsync();
        }

        public async Task<EventPlan> GetEventByIdAsync(int id)
        {
            return await _context.EventPlans.FindAsync(id);
        }

        public async Task<EventPlan> CreateEventAsync(CreateEventDto createEventDto, string userId, string userRole)
        {
            // Handle image upload
            string uniqueFileName = null;
            if (createEventDto.ImageContent != null)
            {
                string uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "events");
                Directory.CreateDirectory(uploadsFolder);
                uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(createEventDto.ImageFileName)}";
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                await File.WriteAllBytesAsync(filePath, createEventDto.ImageContent);
            }

            var eventPlan = new EventPlan
            {
                Title = createEventDto.Title,
                Description = createEventDto.Description,
                ImageUrl = uniqueFileName != null ? $"/uploads/events/{uniqueFileName}" : null,
                EventDate = createEventDto.EventDate,
                ExpiryDate = createEventDto.ExpiryDate,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = userId,
                UserRole = userRole,
                Status = "Active",
                Location = createEventDto.Location,
                EventType = createEventDto.EventType,
                IsActive = true,
                ImageFileName = uniqueFileName,
                ImageContentType = createEventDto.ImageContentType
            };

            _context.EventPlans.Add(eventPlan);
            await _context.SaveChangesAsync();
            return eventPlan;
        }

        public async Task<EventPlan> UpdateEventAsync(UpdateEventDto updateEventDto)
        {
            var eventPlan = await _context.EventPlans.FindAsync(updateEventDto.Id);
            if (eventPlan == null)
                return null;

            // Handle image update if new image is provided
            if (updateEventDto.ImageContent != null)
            {
                // Delete old image if exists
                if (!string.IsNullOrEmpty(eventPlan.ImageFileName))
                {
                    string oldFilePath = Path.Combine(_environment.WebRootPath, "uploads", "events", eventPlan.ImageFileName);
                    if (File.Exists(oldFilePath))
                        File.Delete(oldFilePath);
                }

                // Save new image
                string uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "events");
                string uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(updateEventDto.ImageFileName)}";
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                await File.WriteAllBytesAsync(filePath, updateEventDto.ImageContent);

                eventPlan.ImageUrl = $"/uploads/events/{uniqueFileName}";
                eventPlan.ImageFileName = uniqueFileName;
                eventPlan.ImageContentType = updateEventDto.ImageContentType;
            }

            eventPlan.Title = updateEventDto.Title;
            eventPlan.Description = updateEventDto.Description;
            eventPlan.EventDate = updateEventDto.EventDate;
            eventPlan.ExpiryDate = updateEventDto.ExpiryDate;
            eventPlan.Location = updateEventDto.Location;
            eventPlan.EventType = updateEventDto.EventType;
            eventPlan.IsActive = updateEventDto.IsActive;

            await _context.SaveChangesAsync();
            return eventPlan;
        }

        public async Task<bool> DeleteEventAsync(int id)
        {
            var eventPlan = await _context.EventPlans.FindAsync(id);
            if (eventPlan == null)
                return false;

            // Delete associated image if exists
            if (!string.IsNullOrEmpty(eventPlan.ImageFileName))
            {
                string filePath = Path.Combine(_environment.WebRootPath, "uploads", "events", eventPlan.ImageFileName);
                if (File.Exists(filePath))
                    File.Delete(filePath);
            }

            _context.EventPlans.Remove(eventPlan);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateEventStatusAsync(int id, string status)
        {
            var eventPlan = await _context.EventPlans.FindAsync(id);
            if (eventPlan == null)
                return false;

            eventPlan.Status = status;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task CheckAndUpdateExpiredEvents()
        {
            var expiredEvents = await _context.EventPlans
                .Where(e => e.IsActive && e.ExpiryDate <= DateTime.UtcNow)
                .ToListAsync();

            foreach (var eventPlan in expiredEvents)
            {
                eventPlan.IsActive = false;
                eventPlan.Status = "Expired";
            }

            await _context.SaveChangesAsync();
        }
    }
} 