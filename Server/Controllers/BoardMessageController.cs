using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using BlazorApp1.Server.Data;
using BlazorApp1.Shared.Models;
using System.Security.Claims;
using BlazorApp1.Shared;
using BlazorApp1.Shared.Constants;

namespace BlazorApp1.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "SuperAdmin,Admin,User")]
    public class BoardMessageController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<BoardMessageController> _logger;

        public BoardMessageController(
            ApplicationDbContext context,
            ILogger<BoardMessageController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<Response<List<BoardMessageDto>>>> GetMessages()
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                _logger.LogInformation($"Getting messages for user {currentUserId}");

                var messages = await _context.BoardMessages
                    .Include(m => m.User)
                    .Include(m => m.User.UserDetails)
                    .Include(m => m.MessageReads)
                    .Where(m => m.Type != "RequestSubmission" && 
                               m.Type != "ConcernSubmission" && 
                               m.Type != "SuggestionSubmission" &&
                               m.Type != "UserRegistration") // Filter out system notifications from Board Messages
                    .OrderByDescending(m => m.Timestamp)
                    .Select(msg => new BoardMessageDto
                    {
                        MessageId = msg.MessageId,
                        UserId = msg.UserId,
                        UserName = msg.User.UserName,
                        Role = msg.User.Role,
                        Content = msg.Content,
                        Timestamp = msg.Timestamp,
                        Priority = msg.Priority,
                        Type = msg.Type,
                        IsCurrentUser = msg.UserId == currentUserId,
                        ProfilePicture = msg.User.UserDetails != null ? msg.User.UserDetails.PhotoUrl : null,
                        IsRead = msg.MessageReads.Any(mr => mr.UserId == currentUserId && mr.IsRead) || msg.UserId == currentUserId
                    })
                    .ToListAsync();

                _logger.LogInformation($"Found {messages.Count} messages");
                return Ok(new Response<List<BoardMessageDto>>
                {
                    IsSuccess = true,
                    Message = "Messages retrieved successfully",
                    Data = messages
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting messages: {ex.Message}");
                return StatusCode(500, new Response<List<BoardMessageDto>>
                {
                    IsSuccess = false,
                    Message = "Internal server error while retrieving messages.",
                    Data = null
                });
            }
        }

        [HttpPost]
        public async Task<ActionResult<Response<BoardMessageDto>>> CreateMessage([FromBody] BoardMessageDto messageDto)
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                _logger.LogInformation($"Creating message for user {currentUserId}");

                var user = await _context.Users
                    .Include(u => u.UserDetails)
                    .FirstOrDefaultAsync(u => u.Id == currentUserId);
                if (user == null)
                {
                    _logger.LogWarning($"User {currentUserId} not found");
                    return NotFound(new Response<BoardMessageDto>
                    {
                        IsSuccess = false,
                        Message = "User not found",
                        Data = null
                    });
                }

                var message = new BoardMessage
                {
                    UserId = currentUserId,
                    Content = messageDto.Content,
                    Timestamp = DateTime.UtcNow,
                    Priority = messageDto.Priority,
                    Type = messageDto.Type ?? string.Empty
                };

                _context.BoardMessages.Add(message);
                await _context.SaveChangesAsync();

                // Create MessageRead records for all users
                var allUsers = await _context.Users.ToListAsync();
                var messageReads = allUsers.Select(u => new MessageRead
                {
                    MessageId = message.MessageId,
                    UserId = u.Id,
                    IsRead = u.Id == currentUserId // Mark as read for the sender
                }).ToList();

                _context.MessageReads.AddRange(messageReads);
                await _context.SaveChangesAsync();

                // Return the created message with all properties
                var createdMessageDto = new BoardMessageDto
                {
                    MessageId = message.MessageId,
                    UserId = message.UserId,
                    UserName = user.UserName,
                    Role = user.Role,
                    Content = message.Content,
                    Timestamp = message.Timestamp,
                    Priority = message.Priority,
                    Type = message.Type,
                    IsCurrentUser = true,
                    ProfilePicture = user.UserDetails != null ? user.UserDetails.PhotoUrl : null,
                    IsRead = true
                };

                _logger.LogInformation($"Created message with ID {message.MessageId}");
                return Ok(new Response<BoardMessageDto>
                {
                    IsSuccess = true,
                    Message = "Message created successfully",
                    Data = createdMessageDto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating message: {ex.Message}");
                return StatusCode(500, new Response<BoardMessageDto>
                {
                    IsSuccess = false,
                    Message = "Internal server error while creating message.",
                    Data = null
                });
            }
        }

        [HttpPut("{id}/read")]
        public async Task<ActionResult<Response>> MarkAsRead(int id)
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                _logger.LogInformation($"Marking message {id} as read for user {currentUserId}");

                var messageRead = await _context.MessageReads
                    .FirstOrDefaultAsync(mr => mr.MessageId == id && mr.UserId == currentUserId);

                if (messageRead == null)
                {
                    // If no MessageRead record exists, create one
                    messageRead = new MessageRead
                    {
                        MessageId = id,
                        UserId = currentUserId,
                        IsRead = true
                    };
                    _context.MessageReads.Add(messageRead);
                }
                else
                {
                    messageRead.IsRead = true;
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Message {id} marked as read for user {currentUserId}");

                return Ok(new Response
                {
                    IsSuccess = true,
                    Message = "Message marked as read"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error marking message as read: {ex.Message}");
                return StatusCode(500, new Response
                {
                    IsSuccess = false,
                    Message = "Error marking message as read"
                });
            }
        }

        [HttpDelete("clear")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<ActionResult<Response>> ClearAllMessages()
        {
            try
            {
                _logger.LogInformation("SuperAdmin initiated clearing all messages");

                // Get the execution strategy
                var strategy = _context.Database.CreateExecutionStrategy();

                // Execute with retry logic
                await strategy.ExecuteAsync(async () =>
                {
                    // Begin transaction
                    using var transaction = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        // First remove all message reads
                        var messageReadsDeleted = await _context.MessageReads.ExecuteDeleteAsync();
                        _logger.LogInformation($"Deleted {messageReadsDeleted} message reads");

                        // Then remove all messages
                        var messagesDeleted = await _context.BoardMessages.ExecuteDeleteAsync();
                        _logger.LogInformation($"Deleted {messagesDeleted} messages");

                        // Commit the transaction
                        await transaction.CommitAsync();
                        _logger.LogInformation("Successfully cleared all messages and related data");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error during transaction, rolling back");
                        await transaction.RollbackAsync();
                        throw;
                    }
                });

                return Ok(new Response
                {
                    IsSuccess = true,
                    Message = "All messages have been cleared successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing messages");
                return StatusCode(500, new Response
                {
                    IsSuccess = false,
                    Message = $"An error occurred while clearing messages: {ex.Message}"
                });
            }
        }

        [HttpGet("unread")]
        public async Task<ActionResult<Response<List<BoardMessageDto>>>> GetUnreadNotifications()
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                _logger.LogInformation($"Getting unread notifications for user {currentUserId}");

                var messages = await _context.BoardMessages
                    .Include(m => m.User)
                    .Include(m => m.User.UserDetails)
                    .Include(m => m.MessageReads)
                    .Where(m => !m.MessageReads.Any(mr => mr.UserId == currentUserId && mr.IsRead) && // Only unread messages
                              (m.UserId != currentUserId || // Messages from other users
                               m.Type == "RequestSubmission" || // System notifications
                               m.Type == "ConcernSubmission" ||
                               m.Type == "SuggestionSubmission" ||
                               m.Type == "UserRegistration"))
                    .OrderByDescending(m => m.Timestamp)
                    .Select(msg => new BoardMessageDto
                    {
                        MessageId = msg.MessageId,
                        UserId = msg.UserId,
                        UserName = msg.User.UserName,
                        Role = msg.User.Role,
                        Content = msg.Content,
                        Timestamp = msg.Timestamp,
                        Priority = msg.Priority,
                        Type = msg.Type,
                        IsCurrentUser = msg.UserId == currentUserId,
                        ProfilePicture = msg.User.UserDetails != null ? msg.User.UserDetails.PhotoUrl : null,
                        IsRead = false // Since we're only getting unread messages
                    })
                    .ToListAsync();

                _logger.LogInformation($"Found {messages.Count} unread notifications");
                return Ok(new Response<List<BoardMessageDto>>
                {
                    IsSuccess = true,
                    Message = "Unread notifications retrieved successfully",
                    Data = messages
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting unread notifications: {ex.Message}");
                return StatusCode(500, new Response<List<BoardMessageDto>>
                {
                    IsSuccess = false,
                    Message = "Internal server error while retrieving notifications.",
                    Data = null
                });
            }
        }
    }
} 