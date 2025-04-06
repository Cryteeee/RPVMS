using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BlazorApp1.Server.Data;
using BlazorApp1.Shared.Models;
using BlazorApp1.Shared.Enums;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace BlazorApp1.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SubmissionsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SubmissionsController> _logger;
        private readonly UserManager<User> _userManager;

        public SubmissionsController(
            ApplicationDbContext context,
            ILogger<SubmissionsController> logger,
            UserManager<User> userManager)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
        }

        [HttpPost]
        [Authorize(Roles = "SuperAdmin,Admin,Client")]
        public async Task<ActionResult<SubmissionDto>> CreateSubmission(SubmissionDto submissionDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User ID not found");
                }

                // Verify user is a client
                var appUser = await _userManager.FindByIdAsync(userId);
                if (appUser == null)
                {
                    return NotFound("User not found");
                }

                var roles = await _userManager.GetRolesAsync(appUser);
                if (!roles.Contains("Client"))
                {
                    return Forbid("Only clients can submit forms");
                }

                var submission = new SubmissionEntity
                {
                    UserId = int.Parse(userId),
                    Subject = submissionDto.Subject,
                    Title = submissionDto.Title,
                    Description = submissionDto.Description,
                    Type = submissionDto.Type,
                    Priority = submissionDto.Priority,
                    Status = SubmissionStatus.Pending,
                    Category = submissionDto.Category,
                    Location = submissionDto.Location,
                    PreferredDate = submissionDto.PreferredDate,
                    SubmittedDate = DateTime.UtcNow,
                    LastUpdated = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Attachments = string.Empty
                };

                _context.Submissions.Add(submission);
                await _context.SaveChangesAsync();

                // Map back to DTO
                submissionDto.Id = submission.Id;
                submissionDto.SubmittedDate = submission.SubmittedDate;
                submissionDto.LastUpdated = submission.LastUpdated ?? submission.UpdatedAt;
                submissionDto.UserEmail = appUser.Email ?? "";
                submissionDto.UserName = appUser.UserName ?? "";
                submissionDto.UserId = submission.UserId;

                return CreatedAtAction(nameof(GetSubmission), new { id = submission.Id }, submissionDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating submission");
                if (ex is DbUpdateException)
                {
                    return StatusCode(500, "Database error occurred while saving the submission. Please try again.");
                }
                return StatusCode(500, "An unexpected error occurred while creating the submission. Please try again later.");
            }
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<SubmissionDto>>> GetSubmissions()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User ID not found");
                }

                // Check if user is a client
                var user = await _userManager.FindByIdAsync(userId);
                var isClient = user != null && await _userManager.IsInRoleAsync(user, "Client");

                var query = _context.Submissions
                    .Include(s => s.User)
                    .Where(s => s.UserId == int.Parse(userId));

                // For clients, show all their submissions including deleted ones
                if (!isClient)
                {
                    // For admin users, exclude archived and deleted submissions
                    query = query.Where(s => 
                        s.Status != SubmissionStatus.Archived && 
                        s.Status != SubmissionStatus.Deleted);
                }

                var submissions = await query
                    .OrderByDescending(s => s.SubmittedDate)
                    .Select(s => new SubmissionDto
                    {
                        Id = s.Id,
                        Subject = s.Subject,
                        Description = s.Description,
                        Type = s.Type,
                        Priority = s.Priority ?? "",
                        Status = s.Status,
                        Category = s.Category ?? "",
                        Location = s.Location ?? "",
                        PreferredDate = s.PreferredDate,
                        SubmittedDate = s.SubmittedDate,
                        LastUpdated = s.LastUpdated ?? s.UpdatedAt,
                        UserEmail = s.User != null ? s.User.Email ?? "" : "",
                        UserName = s.User != null ? s.User.UserName ?? "" : "",
                        UserId = s.UserId
                    })
                    .ToListAsync();

                return Ok(submissions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving submissions");
                return StatusCode(500, "An unexpected error occurred while retrieving submissions. Please try again later.");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<SubmissionDto>> GetSubmission(int id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User ID not found");
                }

                var submission = await _context.Submissions
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(s => s.Id == id && s.UserId == int.Parse(userId));

                if (submission == null)
                {
                    return NotFound($"Submission with ID {id} not found or you don't have permission to view it.");
                }

                var submissionDto = new SubmissionDto
                {
                    Id = submission.Id,
                    Subject = submission.Subject,
                    Description = submission.Description,
                    Type = submission.Type,
                    Priority = submission.Priority ?? "",
                    Status = submission.Status,
                    Category = submission.Category ?? "",
                    Location = submission.Location ?? "",
                    PreferredDate = submission.PreferredDate,
                    SubmittedDate = submission.SubmittedDate,
                    LastUpdated = submission.LastUpdated ?? submission.UpdatedAt,
                    UserEmail = submission.User != null ? submission.User.Email ?? "" : "",
                    UserName = submission.User != null ? submission.User.UserName ?? "" : "",
                    UserId = submission.UserId
                };

                return Ok(submissionDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving submission");
                return StatusCode(500, "An unexpected error occurred while retrieving the submission. Please try again later.");
            }
        }

        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] SubmissionStatus newStatus)
        {
            try
            {
                var submission = await _context.Submissions.FindAsync(id);
                if (submission == null)
                {
                    return NotFound($"Submission with ID {id} not found.");
                }

                // Update the status and last updated timestamp
                submission.Status = newStatus;
                submission.LastUpdated = DateTime.UtcNow;

                // If the status is Resolved or Rejected, mark as archived
                if (newStatus == SubmissionStatus.Resolved || newStatus == SubmissionStatus.Rejected)
                {
                    _logger.LogInformation($"Archiving submission {id} due to {newStatus} status");
                    submission.Status = newStatus; // Preserve the resolved/rejected status
                }

                // Save changes
                await _context.SaveChangesAsync();

                // Return the updated status
                return Ok(new { status = newStatus });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating submission status");
                return StatusCode(500, "An error occurred while updating the submission status.");
            }
        }

        [HttpPut("{id}/archive")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> ArchiveSubmission(int id)
        {
            try
            {
                var submission = await _context.Submissions.FindAsync(id);
                if (submission == null)
                {
                    return NotFound($"Submission with ID {id} not found.");
                }

                submission.Status = SubmissionStatus.Archived;
                submission.LastUpdated = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error archiving submission");
                return StatusCode(500, "An error occurred while archiving the submission.");
            }
        }

        [HttpGet("archived")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<ActionResult<IEnumerable<SubmissionDto>>> GetArchivedSubmissions()
        {
            try
            {
                var submissions = await _context.Submissions
                    .Include(s => s.User)
                    .Where(s => s.Status == SubmissionStatus.Archived || 
                               s.Status == SubmissionStatus.Deleted ||
                               s.Status == SubmissionStatus.Resolved ||
                               s.Status == SubmissionStatus.Rejected)
                    .OrderByDescending(s => s.LastUpdated)
                    .Select(s => new SubmissionDto
                    {
                        Id = s.Id,
                        Subject = s.Subject,
                        Description = s.Description,
                        Type = s.Type,
                        Priority = s.Priority ?? "",
                        Status = s.Status,
                        Category = s.Category ?? "",
                        Location = s.Location ?? "",
                        PreferredDate = s.PreferredDate,
                        SubmittedDate = s.SubmittedDate,
                        LastUpdated = s.LastUpdated ?? s.UpdatedAt,
                        UserEmail = s.User != null ? s.User.Email ?? "" : "",
                        UserName = s.User != null ? s.User.UserName ?? "" : "",
                        UserId = s.UserId,
                        IsClientSubmission = true
                    })
                    .ToListAsync();

                return Ok(submissions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving archived submissions");
                return StatusCode(500, "An error occurred while retrieving archived submissions.");
            }
        }

        [HttpPut("{id}/restore")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> RestoreSubmission(int id)
        {
            try
            {
                var submission = await _context.Submissions.FindAsync(id);
                if (submission == null)
                {
                    return NotFound($"Submission with ID {id} not found");
                }

                submission.Status = SubmissionStatus.Pending;
                submission.LastUpdated = DateTime.UtcNow;
                submission.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring submission {Id}", id);
                return StatusCode(500, "An error occurred while restoring the submission");
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> DeleteSubmission(int id)
        {
            try
            {
                var submission = await _context.Submissions.FindAsync(id);
                if (submission == null)
                {
                    return NotFound($"Submission with ID {id} not found");
                }

                // Instead of removing, mark as deleted
                submission.Status = SubmissionStatus.Deleted;
                submission.LastUpdated = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting submission {Id}", id);
                return StatusCode(500, "An error occurred while deleting the submission");
            }
        }

        [HttpGet("all")]
        [Authorize(Roles = "SuperAdmin,Admin,User")]
        public async Task<ActionResult<IEnumerable<SubmissionDto>>> GetAllSubmissions()
        {
            try
            {
                var isUserRole = User.IsInRole("User");
                var submissions = await _context.Submissions
                    .Include(s => s.User)
                    .Where(s => !isUserRole || s.Status != SubmissionStatus.Deleted) // Filter out deleted items for User role
                    .OrderByDescending(s => s.SubmittedDate)
                    .Select(s => new SubmissionDto
                    {
                        Id = s.Id,
                        Subject = s.Subject,
                        Title = s.Title,
                        Description = s.Description,
                        Type = s.Type,
                        Priority = s.Priority ?? "",
                        Status = s.Status,
                        Category = s.Category ?? "",
                        Location = s.Location ?? "",
                        PreferredDate = s.PreferredDate,
                        SubmittedDate = s.SubmittedDate,
                        LastUpdated = s.LastUpdated ?? s.UpdatedAt,
                        UserEmail = s.User != null ? s.User.Email ?? "" : "",
                        UserName = s.User != null ? s.User.UserName ?? "" : "",
                        UserId = s.UserId,
                        IsClientSubmission = true // All submissions from this endpoint are from clients
                    })
                    .ToListAsync();

                return Ok(submissions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all submissions");
                return StatusCode(500, "An unexpected error occurred while retrieving submissions. Please try again later.");
            }
        }
    }
} 