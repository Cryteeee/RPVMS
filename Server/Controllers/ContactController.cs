using BlazorApp1.Server.Services;
using BlazorApp1.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BlazorApp1.Server.Data;
using BlazorApp1.Shared;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using BlazorApp1.Shared.Enums;

namespace BlazorApp1.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ContactController : ControllerBase
    {
        private readonly IContactService _contactService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ContactController> _logger;
        private readonly IMemoryCache _cache;
        private const int MAX_SUBMISSIONS_PER_HOUR = 3;
        private const int SUBMISSION_COOLDOWN_MINUTES = 5;

        public ContactController(IContactService contactService, ApplicationDbContext context, ILogger<ContactController> logger, IMemoryCache cache)
        {
            _contactService = contactService;
            _context = context;
            _logger = logger;
            _cache = cache;
        }

        [HttpGet("get-client-ip")]
        [AllowAnonymous]
        public ActionResult<string> GetClientIP()
        {
            try
            {
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ??
                        Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim() ??
                        "127.0.0.1";
                return Ok(ip);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting client IP");
                return Ok("127.0.0.1");
            }
        }

        [HttpGet("check-ip-ban")]
        [AllowAnonymous]
        public async Task<ActionResult<bool>> CheckIPBan(string ip)
        {
            try
            {
                var bannedSubmission = await _context.SubmissionTrackings
                    .FirstOrDefaultAsync(s => s.ClientIP == ip && s.IsBanned && s.BanEndTime > DateTime.UtcNow);
                return Ok(bannedSubmission != null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking IP ban");
                return Ok(false);
            }
        }

        [HttpGet("get-submission-count")]
        [AllowAnonymous]
        public async Task<ActionResult<SubmissionTrackingStatus>> GetSubmissionCount(string ip)
        {
            try
            {
                var oneHourAgo = DateTime.UtcNow.AddHours(-1);
                var submissions = await _context.SubmissionTrackings
                    .Where(s => s.ClientIP == ip && s.SubmissionTime >= oneHourAgo)
                    .OrderByDescending(s => s.SubmissionTime)
                    .ToListAsync();

                var lastSubmission = submissions.FirstOrDefault();
                var status = new SubmissionTrackingStatus
                {
                    Count = submissions.Count,
                    LastSubmissionTime = lastSubmission?.SubmissionTime ?? DateTime.MinValue,
                    NextSubmissionTime = lastSubmission?.SubmissionTime.AddMinutes(SUBMISSION_COOLDOWN_MINUTES) ?? DateTime.MinValue,
                    CanSubmit = submissions.Count < MAX_SUBMISSIONS_PER_HOUR &&
                               (lastSubmission == null || 
                                DateTime.UtcNow.Subtract(lastSubmission.SubmissionTime).TotalMinutes >= SUBMISSION_COOLDOWN_MINUTES)
                };

                return Ok(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting submission count");
                return StatusCode(500, "Error checking submission limits");
            }
        }

        [HttpGet("clearcache")]
        [AllowAnonymous]
        public async Task<IActionResult> ClearCache()
        {
            try
            {
                // Clean up old submissions (older than 24 hours)
                var oldSubmissions = await _context.SubmissionTrackings
                    .Where(s => s.SubmissionTime < DateTime.UtcNow.AddHours(-24))
                    .ToListAsync();
                _context.SubmissionTrackings.RemoveRange(oldSubmissions);
                await _context.SaveChangesAsync();

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cache");
                return StatusCode(500, "Error clearing cache");
            }
        }

        [HttpPost("create")]
        [AllowAnonymous]
        public async Task<ActionResult<ContactForm>> CreateContactForm([FromBody] ContactForm contactForm)
        {
            try
            {
                _logger.LogInformation("Received contact form data: {@ContactForm}", contactForm);

                if (contactForm == null)
                {
                    _logger.LogError("Contact form is null");
                    return BadRequest("Contact form data is required");
                }

                // Get client IP if not provided
                string ip = contactForm.ClientIP;
                if (string.IsNullOrEmpty(ip))
                {
                    ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? 
                            Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim() ?? 
                            "unknown";
                    contactForm.ClientIP = ip;
                }

                // Clean up old submissions (older than 24 hours)
                var oldSubmissions = await _context.SubmissionTrackings
                    .Where(s => s.SubmissionTime < DateTime.UtcNow.AddHours(-24))
                    .ToListAsync();
                _context.SubmissionTrackings.RemoveRange(oldSubmissions);
                await _context.SaveChangesAsync();

                // Check if IP is banned
                var bannedSubmission = await _context.SubmissionTrackings
                    .FirstOrDefaultAsync(s => s.ClientIP == ip && s.IsBanned && s.BanEndTime > DateTime.UtcNow);
                    
                if (bannedSubmission != null)
                {
                    var remainingTime = bannedSubmission.BanEndTime.Value - DateTime.UtcNow;
                    return BadRequest($"Your IP has been temporarily banned. Please try again in {Math.Ceiling(remainingTime.TotalHours)} hours.");
                }

                // Check submission limits
                var oneHourAgo = DateTime.UtcNow.AddHours(-1);
                var submissions = await _context.SubmissionTrackings
                    .Where(s => s.ClientIP == ip && s.SubmissionTime >= oneHourAgo)
                    .OrderByDescending(s => s.SubmissionTime)
                    .ToListAsync();

                var submissionCount = submissions.Count;
                var lastSubmission = submissions.FirstOrDefault();

                // Check hourly limit
                if (submissionCount >= MAX_SUBMISSIONS_PER_HOUR)
                {
                    return BadRequest($"Maximum submission limit of {MAX_SUBMISSIONS_PER_HOUR} per hour reached. Please try again later.");
                }

                // Check cooldown period
                if (lastSubmission != null)
                {
                    var timeSinceLastSubmission = DateTime.UtcNow - lastSubmission.SubmissionTime;
                    if (timeSinceLastSubmission.TotalMinutes < SUBMISSION_COOLDOWN_MINUTES)
                    {
                        var remainingMinutes = SUBMISSION_COOLDOWN_MINUTES - timeSinceLastSubmission.TotalMinutes;
                        return BadRequest($"Please wait {Math.Ceiling(remainingMinutes)} minutes before submitting another form.");
                    }
                }

                // Add current submission to tracking
                var tracking = new SubmissionTracking
                {
                    ClientIP = ip,
                    SubmissionTime = DateTime.UtcNow,
                    IsBanned = false
                };
                _context.SubmissionTrackings.Add(tracking);

                // Check for suspicious activity (many submissions in short time)
                var recentSubmissions = await _context.SubmissionTrackings
                    .Where(s => s.ClientIP == ip && s.SubmissionTime >= DateTime.UtcNow.AddHours(-24))
                    .CountAsync();

                if (recentSubmissions >= 20) // If more than 20 submissions in 24 hours
                {
                    tracking.IsBanned = true;
                    tracking.BanEndTime = DateTime.UtcNow.AddHours(24);
                    await _context.SaveChangesAsync();
                    return BadRequest("Your IP has been temporarily banned due to suspicious activity. Please try again in 24 hours.");
                }

                await _context.SaveChangesAsync();

                // Validate required fields based on form type
                var validationErrors = new List<string>();

                if (string.IsNullOrWhiteSpace(contactForm.Title))
                {
                    validationErrors.Add("Title is required");
                }

                if (string.IsNullOrWhiteSpace(contactForm.Description))
                {
                    validationErrors.Add("Description is required");
                }

                switch (contactForm.FormType)
                {
                    case FormType.Request:
                        if (!contactForm.RequestType.HasValue)
                        {
                            validationErrors.Add("Request Type is required for Request forms");
                        }
                        if (!contactForm.UrgencyLevel.HasValue)
                        {
                            validationErrors.Add("Urgency Level is required for Request forms");
                        }
                        if (!contactForm.PreferredDate.HasValue)
                        {
                            validationErrors.Add("Preferred Date is required for Request forms");
                        }
                        else if (contactForm.PreferredDate.Value.Date < DateTime.Today)
                        {
                            validationErrors.Add("Preferred Date cannot be in the past");
                        }
                        break;

                    case FormType.Concern:
                        if (!contactForm.PriorityLevel.HasValue)
                        {
                            validationErrors.Add("Priority Level is required for Concern forms");
                        }
                        if (string.IsNullOrWhiteSpace(contactForm.Location))
                        {
                            validationErrors.Add("Location is required for Concern forms");
                        }
                        if (!contactForm.ConcernCategory.HasValue)
                        {
                            validationErrors.Add("Concern Category is required for Concern forms");
                        }
                        break;

                    case FormType.Suggestion:
                        if (string.IsNullOrWhiteSpace(contactForm.SuggestionCategory))
                        {
                            validationErrors.Add("Suggestion Category is required for Suggestion forms");
                        }
                        break;
                }

                if (validationErrors.Any())
                {
                    var errorMessage = string.Join(", ", validationErrors);
                    _logger.LogError("Validation errors: {Errors}", errorMessage);
                    return BadRequest(errorMessage);
                }

                // Set creation date if not set
                if (contactForm.CreatedAt == default)
                {
                    contactForm.CreatedAt = DateTime.UtcNow;
                }

                _logger.LogInformation("Saving contact form to database");
                var result = await _contactService.CreateContactFormAsync(contactForm);
                _logger.LogInformation("Successfully created contact form with ID: {Id}", result.Id);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating contact form: {Message}", ex.Message);
                if (ex.InnerException != null)
                {
                    _logger.LogError("Inner Exception: {Message}", ex.InnerException.Message);
                }
                return StatusCode(500, $"An error occurred while saving the contact form: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ContactForm>>> GetAllContactForms()
        {
            try
            {
                var forms = await _contactService.GetAllContactFormsAsync();
                return Ok(forms);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving contact forms");
                return StatusCode(500, "An error occurred while retrieving contact forms");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ContactForm>> GetContactFormById(int id)
        {
            try
            {
                var form = await _contactService.GetContactFormByIdAsync(id);
                if (form == null)
                {
                    _logger.LogWarning("Contact form not found: {id}", id);
                    return NotFound();
                }
                return Ok(form);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving contact form: {id}", id);
                return StatusCode(500, "An error occurred while retrieving the contact form");
            }
        }

        [HttpGet("concerns")]
        [Authorize(Roles = "SuperAdmin,Admin,User")]
        public async Task<ActionResult<IEnumerable<ConcernViewModel>>> GetConcerns()
        {
            try
            {
                _logger.LogInformation("Starting to retrieve all forms from ContactForms table");
                
                var forms = await _contactService.GetAllContactFormsAsync();
                var concerns = forms
                    .Where(c => c.Status != SubmissionStatus.Deleted) // Filter out deleted items for User role
                    .OrderByDescending(c => c.CreatedAt)
                    .Select(cf => new ConcernViewModel
                    {
                        Id = cf.Id,
                        Title = cf.Title ?? string.Empty,
                        Description = cf.Description ?? string.Empty,
                        UserEmail = cf.Email ?? string.Empty,
                        DateSubmitted = cf.CreatedAt,
                        PriorityLevel = cf.PriorityLevel.HasValue && Enum.IsDefined(typeof(BlazorApp1.Shared.Models.PriorityLevel), cf.PriorityLevel.Value) 
                            ? cf.PriorityLevel 
                            : BlazorApp1.Shared.Models.PriorityLevel.Low,
                        Category = cf.Category ?? string.Empty,
                        Location = cf.Location ?? string.Empty,
                        FormType = cf.FormType,
                        IsRead = cf.IsRead,
                        IsResolved = cf.IsResolved,
                        RequestType = cf.RequestType.HasValue && Enum.IsDefined(typeof(BlazorApp1.Shared.Models.RequestType), cf.RequestType.Value)
                            ? cf.RequestType
                            : null,
                        ConcernCategory = cf.ConcernCategory.HasValue && Enum.IsDefined(typeof(BlazorApp1.Shared.Models.ConcernCategory), cf.ConcernCategory.Value)
                            ? cf.ConcernCategory
                            : null,
                        UrgencyLevel = cf.UrgencyLevel.HasValue && Enum.IsDefined(typeof(BlazorApp1.Shared.Models.UrgencyLevel), cf.UrgencyLevel.Value)
                            ? cf.UrgencyLevel
                            : null,
                        PreferredDate = cf.PreferredDate == default ? null : cf.PreferredDate
                    }).ToList();

                return Ok(concerns);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving concerns: {Message}", ex.Message);
                return StatusCode(500, "An error occurred while retrieving concerns");
            }
        }

        [HttpPut("{id}/status")]
        [Authorize(Roles = "SuperAdmin,Admin,User")]
        public async Task<IActionResult> UpdateStatus(int id, ConcernViewModel concernVM)
        {
            try
            {
                var contactForm = await _contactService.GetContactFormByIdAsync(id);
                if (contactForm == null)
                {
                    return NotFound();
                }

                // Update all status fields
                contactForm.IsRead = concernVM.IsRead;
                contactForm.IsResolved = concernVM.IsResolved;
                contactForm.Status = concernVM.Status;

                // Archive the form if it's resolved or rejected
                if (concernVM.Status == SubmissionStatus.Resolved || concernVM.Status == SubmissionStatus.Rejected)
                {
                    contactForm.IsArchived = true;
                    contactForm.ArchivedDate = DateTime.UtcNow;
                    _logger.LogInformation($"Archiving contact form {id} due to status change to {concernVM.Status}");
                }

                await _contactService.UpdateContactFormAsync(contactForm);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating contact form status");
                return StatusCode(500, "Internal server error while updating contact form status");
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> DeleteContactForm(int id)
        {
            try
            {
                var contactForm = await _context.ContactForms.FindAsync(id);
                if (contactForm == null)
                {
                    return NotFound();
                }

                // Instead of removing, mark as deleted and archived
                contactForm.Status = SubmissionStatus.Deleted;
                contactForm.IsArchived = true;
                contactForm.ArchivedDate = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();
                _logger.LogInformation("Contact form {id} marked as deleted and archived", id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting contact form with ID: {id}", id);
                return StatusCode(500, "An error occurred while deleting the contact form");
            }
        }

        [HttpDelete("{id}/permanent")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> DeleteContactFormPermanently(int id)
        {
            try
            {
                var contactForm = await _context.ContactForms.FindAsync(id);
                if (contactForm == null)
                {
                    return NotFound();
                }

                // Permanently remove from database
                _context.ContactForms.Remove(contactForm);
                await _context.SaveChangesAsync();

                // Also remove any associated messages or notifications
                var messages = await _context.BoardMessages
                    .Where(m => m.Content.Contains(contactForm.Title))
                    .ToListAsync();
                
                if (messages.Any())
                {
                    var messageIds = messages.Select(m => m.MessageId).ToList();
                    var messageReads = await _context.MessageReads
                        .Where(mr => messageIds.Contains(mr.MessageId))
                        .ToListAsync();

                    _context.MessageReads.RemoveRange(messageReads);
                    _context.BoardMessages.RemoveRange(messages);
                    await _context.SaveChangesAsync();
                }

                return Ok(new { message = "Contact form has been permanently deleted" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error permanently deleting contact form with ID: {id}", id);
                return StatusCode(500, "An error occurred while permanently deleting the contact form");
            }
        }

        [HttpDelete("clear-all")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<ActionResult<Response>> ClearAllContacts()
        {
            try
            {
                _logger.LogInformation("SuperAdmin initiated clearing all contacts");

                // Get the execution strategy
                var strategy = _context.Database.CreateExecutionStrategy();

                // Execute with retry logic
                await strategy.ExecuteAsync(async () =>
                {
                    // Begin transaction
                    using var transaction = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        // Delete all contact forms
                        var contactsDeleted = await _context.ContactForms.ExecuteDeleteAsync();
                        _logger.LogInformation($"Deleted {contactsDeleted} contact forms");

                        // Commit the transaction
                        await transaction.CommitAsync();
                        _logger.LogInformation("Successfully cleared all contact forms");
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
                    Message = "All contacts have been cleared successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing contacts");
                return StatusCode(500, new Response
                {
                    IsSuccess = false,
                    Message = $"An error occurred while clearing contacts: {ex.Message}"
                });
            }
        }

        [HttpPost("{id}/archive")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> ArchiveContactForm(int id)
        {
            try
            {
                var result = await _contactService.ArchiveContactFormAsync(id);
                if (!result)
                {
                    return NotFound($"Contact form with ID {id} not found.");
                }

                return Ok(new { message = "Contact form archived successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error archiving contact form with ID {id}");
                return StatusCode(500, "An error occurred while archiving the contact form.");
            }
        }

        [HttpGet("archived")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<ActionResult<IEnumerable<SubmissionDto>>> GetArchivedContactForms()
        {
            try
            {
                var forms = await _contactService.GetArchivedContactFormsAsync();
                var submissionDtos = forms.Select(cf => new SubmissionDto
                {
                    Id = cf.Id,
                    Subject = cf.Title,
                    Description = cf.Description,
                    Type = cf.FormType.ToString(),
                    Priority = cf.PriorityLevel?.ToString() ?? "",
                    Status = cf.Status == SubmissionStatus.Deleted ? SubmissionStatus.Deleted :
                            cf.IsResolved ? SubmissionStatus.Resolved : 
                            cf.Status == SubmissionStatus.Rejected ? SubmissionStatus.Rejected :
                            cf.IsArchived ? SubmissionStatus.Archived : cf.Status,
                    Category = cf.Category,
                    Location = cf.Location,
                    PreferredDate = cf.PreferredDate,
                    SubmittedDate = cf.CreatedAt,
                    LastUpdated = cf.ArchivedDate ?? cf.CreatedAt,
                    UserEmail = cf.Email,
                    UserName = cf.Email,
                    IsClientSubmission = false  // Contact forms are always non-client submissions
                }).ToList();

                return Ok(submissionDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving archived contact forms");
                return StatusCode(500, "An error occurred while retrieving archived contact forms");
            }
        }

        [HttpPut("{id}/restore")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> RestoreContactForm(int id)
        {
            try
            {
                var contactForm = await _context.ContactForms.FindAsync(id);
                if (contactForm == null)
                {
                    return NotFound($"Contact form with ID {id} not found");
                }

                // Allow restoration regardless of current status (including deleted)
                contactForm.Status = SubmissionStatus.Pending;
                contactForm.IsArchived = false;
                contactForm.ArchivedDate = null;
                contactForm.IsResolved = false;
                
                await _context.SaveChangesAsync();
                _logger.LogInformation("Contact form {id} has been restored from status {status}", id, contactForm.Status);
                
                return Ok(new { message = "Contact form restored successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring contact form with ID: {id}", id);
                return StatusCode(500, "An error occurred while restoring the contact form");
            }
        }
    }

    public class SubmissionTrackingStatus
    {
        public static Shared.Enums.SubmissionStatus Rejected { get; internal set; }
        public int Count { get; set; }
        public bool CanSubmit { get; set; }
        public DateTime LastSubmissionTime { get; set; }
        public DateTime NextSubmissionTime { get; set; }
    }
} 
