using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BlazorApp1.Server.Data;
using BlazorApp1.Shared.Models;
using BlazorApp1.Shared;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace BlazorApp1.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CubicMeterReadingController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CubicMeterReadingController> _logger;

        public CubicMeterReadingController(ApplicationDbContext context, ILogger<CubicMeterReadingController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<Response<List<CubicMeterReadingDto>>>> GetUserReadings(string userId)
        {
            try
            {
                _logger.LogInformation($"Getting cubic meter readings for user ID: {userId}");
                if (!int.TryParse(userId, out int userIdInt))
                {
                    _logger.LogWarning($"Invalid user ID format: {userId}");
                    return BadRequest(new Response<List<CubicMeterReadingDto>>
                    {
                        IsSuccess = false,
                        Message = $"Invalid user ID format: {userId}"
                    });
                }

                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var isAdmin = User.IsInRole("Admin") || User.IsInRole("SuperAdmin");

                if (!isAdmin && currentUserId != userId)
                {
                    _logger.LogWarning($"Unauthorized access attempt: User {currentUserId} tried to access readings for user {userId}");
                    return Forbid();
                }

                var user = await _context.Users.FindAsync(userIdInt);
                if (user == null)
                {
                    _logger.LogWarning($"User not found: {userId}");
                    return NotFound(new Response<List<CubicMeterReadingDto>>
                    {
                        IsSuccess = false,
                        Message = $"User not found: {userId}"
                    });
                }

                var readings = await _context.CubicMeterReadings
                    .Where(r => r.UserId == userIdInt)
                    .Select(r => new CubicMeterReadingDto
                    {
                        Id = r.Id,
                        UserId = r.UserId.ToString(),
                        UserName = r.User.UserName,
                        Reading = r.Reading,
                        PricePerCubicMeter = r.PricePerCubicMeter,
                        ReadingDate = r.ReadingDate
                    })
                    .OrderByDescending(r => r.ReadingDate)
                    .ToListAsync();

                _logger.LogInformation($"Found {readings.Count} readings for user {userId}");
                return Ok(new Response<List<CubicMeterReadingDto>>
                {
                    Data = readings,
                    IsSuccess = true,
                    Message = $"Retrieved {readings.Count} readings successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving readings for user {userId}");
                return StatusCode(500, new Response<List<CubicMeterReadingDto>>
                {
                    IsSuccess = false,
                    Message = $"An error occurred while retrieving readings: {ex.Message}"
                });
            }
        }

        [HttpGet("latest/{userId}")]
        public async Task<ActionResult<Response<CubicMeterReadingDto>>> GetLatestReading(string userId)
        {
            try
            {
                _logger.LogInformation($"Getting latest cubic meter reading for user ID: {userId}");
                if (!int.TryParse(userId, out int userIdInt))
                {
                    _logger.LogWarning($"Invalid user ID format: {userId}");
                    return BadRequest(new Response<CubicMeterReadingDto>
                    {
                        IsSuccess = false,
                        Message = $"Invalid user ID format: {userId}"
                    });
                }

                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var isAdmin = User.IsInRole("Admin") || User.IsInRole("SuperAdmin");

                if (!isAdmin && currentUserId != userId)
                {
                    _logger.LogWarning($"Unauthorized access attempt: User {currentUserId} tried to access latest reading for user {userId}");
                    return Forbid();
                }

                var user = await _context.Users.FindAsync(userIdInt);
                if (user == null)
                {
                    _logger.LogWarning($"User not found: {userId}");
                    return NotFound(new Response<CubicMeterReadingDto>
                    {
                        IsSuccess = false,
                        Message = $"User not found: {userId}"
                    });
                }

                var latestReading = await _context.CubicMeterReadings
                    .Where(r => r.UserId == userIdInt)
                    .OrderByDescending(r => r.ReadingDate)
                    .Select(r => new CubicMeterReadingDto
                    {
                        Id = r.Id,
                        UserId = r.UserId.ToString(),
                        UserName = r.User.UserName,
                        Reading = r.Reading,
                        PricePerCubicMeter = r.PricePerCubicMeter,
                        ReadingDate = r.ReadingDate
                    })
                    .FirstOrDefaultAsync();

                if (latestReading == null)
                {
                    return NotFound(new Response<CubicMeterReadingDto>
                    {
                        IsSuccess = false,
                        Message = "No readings found for this user"
                    });
                }

                return Ok(new Response<CubicMeterReadingDto>
                {
                    Data = latestReading,
                    IsSuccess = true,
                    Message = "Latest reading retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving latest reading for user {userId}");
                return StatusCode(500, new Response<CubicMeterReadingDto>
                {
                    IsSuccess = false,
                    Message = $"An error occurred while retrieving the latest reading: {ex.Message}"
                });
            }
        }

        [HttpPost]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<ActionResult<Response<CubicMeterReadingDto>>> AddReading([FromBody] CubicMeterReadingDto readingDto)
        {
            try
            {
                _logger.LogInformation($"Adding cubic meter reading for user {readingDto.UserId}");
                if (!int.TryParse(readingDto.UserId, out int userIdInt))
                {
                    _logger.LogWarning($"Invalid user ID format: {readingDto.UserId}");
                    return BadRequest(new Response<CubicMeterReadingDto>
                    {
                        IsSuccess = false,
                        Message = $"Invalid user ID format: {readingDto.UserId}"
                    });
                }

                var user = await _context.Users.FindAsync(userIdInt);
                if (user == null)
                {
                    _logger.LogWarning($"User not found: {readingDto.UserId}");
                    return NotFound(new Response<CubicMeterReadingDto>
                    {
                        IsSuccess = false,
                        Message = $"User not found: {readingDto.UserId}"
                    });
                }

                var reading = new CubicMeterReading
                {
                    UserId = userIdInt,
                    Reading = readingDto.Reading,
                    PricePerCubicMeter = readingDto.PricePerCubicMeter,
                    ReadingDate = DateTime.Now
                };

                _context.CubicMeterReadings.Add(reading);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Successfully added reading {reading.Id} for user {readingDto.UserId}");

                readingDto.Id = reading.Id;
                readingDto.ReadingDate = reading.ReadingDate;
                readingDto.UserName = user.UserName;

                return Ok(new Response<CubicMeterReadingDto>
                {
                    Data = readingDto,
                    IsSuccess = true,
                    Message = "Reading added successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding reading for user {readingDto.UserId}");
                return StatusCode(500, new Response<CubicMeterReadingDto>
                {
                    IsSuccess = false,
                    Message = $"An error occurred while adding the reading: {ex.Message}"
                });
            }
        }
    }
} 