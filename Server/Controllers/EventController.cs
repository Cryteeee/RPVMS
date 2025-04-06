using BlazorApp1.Server.Services;
using BlazorApp1.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BlazorApp1.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EventController : ControllerBase
    {
        private readonly IEventService _eventService;

        public EventController(IEventService eventService)
        {
            _eventService = eventService;
        }

        [HttpGet]
        public async Task<ActionResult<List<EventListDto>>> GetAllEvents()
        {
            var events = await _eventService.GetAllEventsAsync();
            return Ok(events);
        }

        [HttpGet("active")]
        public async Task<ActionResult<List<EventListDto>>> GetActiveEvents()
        {
            var events = await _eventService.GetActiveEventsAsync();
            return Ok(events);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<EventPlan>> GetEventById(int id)
        {
            var eventPlan = await _eventService.GetEventByIdAsync(id);
            if (eventPlan == null)
                return NotFound();

            return Ok(eventPlan);
        }

        [HttpPost]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<ActionResult<EventPlan>> CreateEvent([FromBody] CreateEventDto createEventDto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userRole))
                return Unauthorized();

            var eventPlan = await _eventService.CreateEventAsync(createEventDto, userId, userRole);
            return CreatedAtAction(nameof(GetEventById), new { id = eventPlan.Id }, eventPlan);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<ActionResult<EventPlan>> UpdateEvent(int id, [FromBody] UpdateEventDto updateEventDto)
        {
            if (id != updateEventDto.Id)
                return BadRequest();

            var eventPlan = await _eventService.UpdateEventAsync(updateEventDto);
            if (eventPlan == null)
                return NotFound();

            return Ok(eventPlan);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<ActionResult> DeleteEvent(int id)
        {
            var result = await _eventService.DeleteEventAsync(id);
            if (!result)
                return NotFound();

            return NoContent();
        }

        [HttpPut("{id}/status")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<ActionResult> UpdateEventStatus(int id, [FromBody] string status)
        {
            var result = await _eventService.UpdateEventStatusAsync(id, status);
            if (!result)
                return NotFound();

            return NoContent();
        }

        [HttpPost("check-expired")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<ActionResult> CheckExpiredEvents()
        {
            await _eventService.CheckAndUpdateExpiredEvents();
            return NoContent();
        }
    }
} 