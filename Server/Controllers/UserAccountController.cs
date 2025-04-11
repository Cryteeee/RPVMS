using BlazorApp1.Server.Data;
using BlazorApp1.Server.Models;
using BlazorApp1.Shared.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.Text.RegularExpressions;
using System.Linq;
using BlazorApp1.Server.Services;
using BlazorApp1.Client.Services;
using Microsoft.AspNetCore.Authorization;
using BlazorApp1.Server.Utilities;
using BlazorApp1.Shared.Constants;
using Microsoft.AspNetCore.Identity;
using SharedUserDetails = BlazorApp1.Shared.UserDetailsDto;
using BlazorApp1.Shared;

namespace BlazorApp1.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "SuperAdmin,Admin,User,Client")]
    public class UserAccountController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<UserAccountController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IUserService _userService;
        private readonly IEmailService _emailService;
        private readonly UserManager<User> _userManager;

        public UserAccountController(
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            ILogger<UserAccountController> logger,
            IConfiguration configuration,
            IUserService userService,
            IEmailService emailService,
            UserManager<User> userManager)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
            _configuration = configuration;
            _userService = userService;
            _emailService = emailService;
            _userManager = userManager;
        }

        [HttpGet("GetById/{id}")]
        [Authorize(Roles = "SuperAdmin,Admin,User,Client")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                _logger.LogInformation($"GetById called for ID: {id}");

                var user = await _userManager.FindByIdAsync(id.ToString());
                if (user == null)
                {
                    _logger.LogWarning($"User not found with ID: {id}");
                    return NotFound(new Response<RegistrationView> 
                    { 
                        IsSuccess = false, 
                        Message = "User not found",
                        Data = null
                    });
                }

                var result = new RegistrationView
                {
                    UserId = user.Id,
                    Email = user.Email,
                    Username = user.UserName,
                    DateOfBirth = user.UserDetails?.DateOfBirth,
                    Gender = user.UserDetails?.Gender,
                    Nationality = user.UserDetails?.Nationality,
                    PhotoUrl = user.UserDetails?.PhotoUrl,
                    IsEmailVerified = user.IsEmailVerified,
                    FullName = user.UserDetails?.FullName,
                    Address = user.UserDetails?.Address,
                    MaritalStatus = user.UserDetails?.MaritalStatus
                };

                return Ok(new Response<RegistrationView>
                {
                    IsSuccess = true,
                    Message = "User details retrieved successfully",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving user by ID {0}", id);
                return StatusCode(500, new Response<RegistrationView> 
                { 
                    IsSuccess = false, 
                    Message = $"An error occurred while retrieving user details: {ex.Message}",
                    Data = null 
                });
            }
        }

        [HttpGet("GetAll")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                _logger.LogInformation("GetAll endpoint called");
                
                var users = await _context.Users
                    .Include(u => u.UserDetails)
                    .Where(u => u.Role != "SuperAdmin")
                    .Select(u => new UserDto
                    {
                        Id = u.Id,
                        UserId = u.Id,
                        Email = u.Email,
                        Username = u.UserName,
                        Role = u.Role,
                        IsActive = u.IsActive,
                        CreatedAt = u.CreatedAt,
                        LastLoginAt = u.LastLoginAt,
                        IsEmailVerified = u.IsEmailVerified,
                        PhoneNumber = u.UserDetails != null ? u.UserDetails.ContactNumber : null
                    })
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} users from database", users.Count);

                var response = new Response<List<UserDto>>
                {
                    IsSuccess = true,
                    Message = "Users retrieved successfully",
                    Data = users
                };

                _logger.LogInformation("Returning response with {Count} users", users.Count);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving all users");
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new Response<List<UserDto>>
                    {
                        IsSuccess = false,
                        Message = $"An error occurred while retrieving users: {ex.Message}",
                        Data = null
                    });
            }
        }

        [HttpPut("AddUpdate")]
        public async Task<IActionResult> AddUpdate(RegistrationView entity)
        {
            try
            {
                var data = await _context.Users
                    .FirstOrDefaultAsync(a => a.Id == entity.UserId);

                if (data == null)
                {
                    return NotFound(new Response { IsSuccess = false, Message = "User not found" });
                }

                if (!string.IsNullOrEmpty(entity.Email))
                {
                    data.Email = entity.Email;
                }

                if (!string.IsNullOrEmpty(entity.Username))
                {
                    data.UserName = entity.Username;
                }

                if (!string.IsNullOrEmpty(entity.Password))
                {
                    data.PasswordHash = PasswordUtility.HashPassword(entity.Password);
                }

                _context.Users.Update(data);
                await _context.SaveChangesAsync();

                return Ok(new Response { IsSuccess = true, Message = "Record updated!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating user details for user ID {0}", entity.UserId);
                return StatusCode(500, new Response { IsSuccess = false, Message = ex.Message });
            }
        }

        [HttpPut("UpdatePassword")]
        public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordDto dto)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(a => a.Id == dto.UserId);

                if (user == null)
                {
                    return NotFound(new Response { IsSuccess = false, Message = "User not found." });
                }

                bool validPassword = PasswordUtility.VerifyPassword(dto.CurrentPassword, user.PasswordHash);
                if (!validPassword)
                {
                    return BadRequest(new Response { IsSuccess = false, Message = "Current password is incorrect." });
                }

                if (string.IsNullOrEmpty(dto.NewPassword))
                {
                    return BadRequest(new Response { IsSuccess = false, Message = "New password cannot be empty." });
                }

                if (!IsValidPassword(dto.NewPassword))
                {
                    return BadRequest(new Response { IsSuccess = false, Message = "New password must contain at least one uppercase letter, one lowercase letter, and one number." });
                }

                user.PasswordHash = PasswordUtility.HashPassword(dto.NewPassword);
                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                return Ok(new Response { IsSuccess = true, Message = "Password updated successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating password for user ID {0}", dto.UserId);
                return StatusCode(500, new Response { IsSuccess = false, Message = ex.Message });
            }
        }

        [HttpGet("GetUserDetails/{userId}")]
        [Authorize(Roles = "SuperAdmin,Admin,User,Client")]
        public async Task<IActionResult> GetUserDetails(int userId)
        {
            try
            {
                _logger.LogInformation($"Fetching user details for User ID: {userId}");

                // Get the authenticated user's claims
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userRoleClaim = User.FindFirst(ClaimTypes.Role)?.Value;

                if (string.IsNullOrEmpty(userIdClaim))
                {
                    _logger.LogWarning("No user ID claim found in token");
                    return Unauthorized(new Response<UserDetailsDto>
                    {
                        IsSuccess = false,
                        Message = "Invalid authentication token",
                        Data = null
                    });
                }

                // Check if user has permission to access this data
                bool isSuperAdmin = userRoleClaim == "SuperAdmin";
                bool isAdmin = userRoleClaim == "Admin";
                bool isOwnData = userIdClaim == userId.ToString();

                if (!isSuperAdmin && !isAdmin && !isOwnData)
                {
                    _logger.LogWarning($"User {userIdClaim} attempted unauthorized access to details for user {userId}");
                    return Forbid();
                }
                
                var userDetails = await _context.UserDetails
                    .AsNoTracking()
                    .Select(ud => new UserDetailsDto
                    {
                        UserId = ud.UserId,
                        DateOfBirth = ud.DateOfBirth,
                        Gender = ud.Gender ?? string.Empty,
                        Nationality = ud.Nationality ?? string.Empty,
                        PhotoUrl = ud.PhotoUrl ?? string.Empty,
                        PhotoFileName = ud.PhotoFileName ?? string.Empty,
                        FullName = ud.FullName ?? string.Empty,
                        Address = ud.Address ?? string.Empty,
                        MaritalStatus = ud.MaritalStatus ?? string.Empty,
                        ContactNumber = ud.ContactNumber ?? string.Empty
                    })
                    .FirstOrDefaultAsync(ud => ud.UserId == userId);

                if (userDetails == null)
                {
                    _logger.LogInformation($"No existing user details found for User ID: {userId}");
                    return Ok(new Response<UserDetailsDto>
                    {
                        IsSuccess = true,
                        Message = "No existing details found, returning default values",
                        Data = new UserDetailsDto 
                        {
                            UserId = userId,
                            DateOfBirth = DateTime.Today.AddYears(-18),
                            Gender = string.Empty,
                            Nationality = string.Empty,
                            PhotoUrl = string.Empty,
                            PhotoFileName = string.Empty,
                            FullName = string.Empty,
                            Address = string.Empty,
                            MaritalStatus = string.Empty,
                            ContactNumber = string.Empty
                        }
                    });
                }

                _logger.LogInformation($"Successfully retrieved user details for User ID: {userId}");
                return Ok(new Response<UserDetailsDto>
                {
                    IsSuccess = true,
                    Message = "User details retrieved successfully",
                    Data = userDetails
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving user details for User ID: {userId}");
                return StatusCode(500, new Response<UserDetailsDto>
                { 
                    IsSuccess = false, 
                    Message = $"An error occurred while retrieving user details: {ex.Message}",
                    Data = null
                });
            }
        }

        [HttpPut("UpdateUserDetails")]
        public async Task<IActionResult> UpdateUserDetails([FromBody] UserDetailsDto userDetailsDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new Response<UserDetailsDto> 
                { 
                    IsSuccess = false, 
                    Message = "Invalid model state",
                    Data = null 
                });
            }

            try
            {
                _logger.LogInformation($"Attempting to update user details for User ID: {userDetailsDto.UserId}");

                // Use UserManager to find the user
                var user = await _userManager.FindByIdAsync(userDetailsDto.UserId.ToString());
                if (user == null)
                {
                    _logger.LogWarning($"User not found with ID: {userDetailsDto.UserId}");
                    return NotFound(new Response<UserDetailsDto> 
                    { 
                        IsSuccess = false, 
                        Message = "User not found.",
                        Data = null 
                    });
                }

                // Validate date of birth
                if (userDetailsDto.DateOfBirth > DateTime.Today)
                {
                    return BadRequest(new Response<UserDetailsDto> 
                    { 
                        IsSuccess = false, 
                        Message = "Date of birth cannot be in the future.",
                        Data = null 
                    });
                }

                // Find existing user details or create new
                var existingUserDetails = await _context.UserDetails
                    .FirstOrDefaultAsync(ud => ud.UserId == userDetailsDto.UserId);

                if (existingUserDetails == null)
                {
                    existingUserDetails = new UserDetails
                    {
                        UserId = userDetailsDto.UserId,
                        Gender = userDetailsDto.Gender ?? string.Empty,
                        Nationality = userDetailsDto.Nationality ?? string.Empty,
                        DateOfBirth = userDetailsDto.DateOfBirth,
                        FullName = userDetailsDto.FullName ?? string.Empty,
                        Address = userDetailsDto.Address ?? string.Empty,
                        MaritalStatus = userDetailsDto.MaritalStatus ?? string.Empty,
                        ContactNumber = userDetailsDto.ContactNumber ?? string.Empty
                    };
                    _context.UserDetails.Add(existingUserDetails);
                }
                else
                {
                    existingUserDetails.Gender = userDetailsDto.Gender ?? string.Empty;
                    existingUserDetails.Nationality = userDetailsDto.Nationality ?? string.Empty;
                    existingUserDetails.DateOfBirth = userDetailsDto.DateOfBirth;
                    existingUserDetails.FullName = userDetailsDto.FullName ?? string.Empty;
                    existingUserDetails.Address = userDetailsDto.Address ?? string.Empty;
                    existingUserDetails.MaritalStatus = userDetailsDto.MaritalStatus ?? string.Empty;
                    existingUserDetails.ContactNumber = userDetailsDto.ContactNumber ?? string.Empty;
                }

                // Handle photo update
                string photoUrl = existingUserDetails.PhotoUrl;
                if (!string.IsNullOrEmpty(userDetailsDto.PhotoBase64))
                {
                    try
                    {
                        // Extract base64 data
                        var base64Data = userDetailsDto.PhotoBase64.Split(',').Last();
                        var imageBytes = Convert.FromBase64String(base64Data);

                        // Create unique filename
                        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(userDetailsDto.PhotoFileName ?? ".png")}";
                        var uploadPath = Path.Combine(_environment.WebRootPath, "uploads", "profile-photos");
                        
                        // Ensure directory exists
                        if (!Directory.Exists(uploadPath))
                        {
                            Directory.CreateDirectory(uploadPath);
                        }

                        // Save the file
                        var filePath = Path.Combine(uploadPath, fileName);
                        await System.IO.File.WriteAllBytesAsync(filePath, imageBytes);

                        // Update photo URL
                        photoUrl = $"/uploads/profile-photos/{fileName}";
                        _logger.LogInformation($"Saved new profile photo for user {userDetailsDto.UserId}: {photoUrl}");

                        // Delete old photo if exists
                        if (!string.IsNullOrEmpty(existingUserDetails.PhotoUrl))
                        {
                            var oldPhotoPath = Path.Combine(_environment.WebRootPath, existingUserDetails.PhotoUrl.TrimStart('/'));
                            if (System.IO.File.Exists(oldPhotoPath))
                            {
                                System.IO.File.Delete(oldPhotoPath);
                                _logger.LogInformation($"Deleted old profile photo: {oldPhotoPath}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing profile photo");
                        return StatusCode(500, new Response<UserDetailsDto> 
                        { 
                            IsSuccess = false, 
                            Message = "Failed to process profile photo",
                            Data = null 
                        });
                    }
                }

                // Update properties
                existingUserDetails.Gender = userDetailsDto.Gender ?? existingUserDetails.Gender;
                existingUserDetails.Nationality = userDetailsDto.Nationality ?? existingUserDetails.Nationality;
                existingUserDetails.DateOfBirth = userDetailsDto.DateOfBirth ?? existingUserDetails.DateOfBirth;
                existingUserDetails.PhotoUrl = photoUrl;
                existingUserDetails.PhotoFileName = userDetailsDto.PhotoFileName;
                existingUserDetails.FullName = userDetailsDto.FullName ?? existingUserDetails.FullName;
                existingUserDetails.Address = userDetailsDto.Address ?? existingUserDetails.Address;
                existingUserDetails.MaritalStatus = userDetailsDto.MaritalStatus ?? existingUserDetails.MaritalStatus;

                await _context.SaveChangesAsync();

                // Return the updated user details
                var updatedDetails = new UserDetailsDto
                {
                    UserId = existingUserDetails.UserId,
                    DateOfBirth = existingUserDetails.DateOfBirth,
                    Gender = existingUserDetails.Gender,
                    Nationality = existingUserDetails.Nationality,
                    PhotoUrl = existingUserDetails.PhotoUrl,
                    PhotoFileName = existingUserDetails.PhotoFileName,
                    FullName = existingUserDetails.FullName,
                    Address = existingUserDetails.Address,
                    MaritalStatus = existingUserDetails.MaritalStatus
                };

                _logger.LogInformation($"Successfully updated user details for User ID: {userDetailsDto.UserId}");
                return Ok(new Response<UserDetailsDto> 
                { 
                    IsSuccess = true, 
                    Message = "User details updated successfully.",
                    Data = updatedDetails 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating user details for User ID: {userDetailsDto.UserId}");
                return StatusCode(500, new Response<UserDetailsDto> 
                { 
                    IsSuccess = false, 
                    Message = $"An error occurred while updating user details: {ex.Message}",
                    Data = null 
                });
            }
        }

        private bool IsValidPassword(string password)
        {
            return Regex.IsMatch(password, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$");
        }

        [HttpPost("Login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(a => a.Email == dto.Email);

                if (user == null)
                {
                    return NotFound(new Response { IsSuccess = false, Message = "User not found." });
                }

                bool validPassword = PasswordUtility.VerifyPassword(dto.Password, user.PasswordHash);
                if (!validPassword)
                {
                    return BadRequest(new Response { IsSuccess = false, Message = "Invalid password." });
                }

                // Check if user is active (unless they are SuperAdmin)
                if (user.Role != "SuperAdmin" && !user.IsActive)
                {
                    return BadRequest(new Response { IsSuccess = false, Message = "Your account is not active. Please wait for an administrator to activate your account." });
                }

                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_configuration["JwtSettings:SecretKey"] ?? 
                    throw new InvalidOperationException("JWT Secret Key is not configured"));
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                        new Claim(ClaimTypes.Email, user.Email),
                        new Claim(ClaimTypes.Name, user.UserName),
                        new Claim(ClaimTypes.Role, user.Role),
                        new Claim("IsEmailVerified", user.IsEmailVerified.ToString()),
                        new Claim("IsActive", user.IsActive.ToString())
                    }),
                    Expires = DateTime.UtcNow.AddDays(7),
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                    Issuer = _configuration["JwtSettings:Issuer"],
                    Audience = _configuration["JwtSettings:Audience"],
                    NotBefore = DateTime.UtcNow
                };

                var token = tokenHandler.CreateToken(tokenDescriptor);

                return Ok(new Response<string>
                {
                    IsSuccess = true,
                    Message = "Login successful",
                    Data = tokenHandler.WriteToken(token)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while logging in");
                return StatusCode(500, new Response { IsSuccess = false, Message = ex.Message });
            }
        }

        [HttpPost("SendVerificationEmail")]
        public async Task<IActionResult> SendVerificationEmail([FromBody] int userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                {
                    return NotFound(new Response { IsSuccess = false, Message = "User account not found. Please try logging in again." });
                }

                // Check if email is already verified
                if (user.IsEmailVerified)
                {
                    return BadRequest(new Response { IsSuccess = false, Message = "Your email is already verified." });
                }

                // Check if enough time has passed since the last email
                if (user.LastEmailSentTime.HasValue)
                {
                    var timeSinceLastEmail = DateTime.UtcNow.Subtract(user.LastEmailSentTime.Value).TotalSeconds;
                    if (timeSinceLastEmail < 60)
                    {
                        var secondsToWait = Math.Ceiling(60 - timeSinceLastEmail);
                        return BadRequest(new Response 
                        { 
                            IsSuccess = false, 
                            Message = $"Please wait {secondsToWait} seconds before requesting another verification email." 
                        });
                    }
                }

                // Generate verification token
                var token = Guid.NewGuid().ToString();
                user.EmailVerificationToken = token;
                user.EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24);
                user.LastEmailSentTime = DateTime.UtcNow;

                // Create verification link with absolute URL
                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var verificationLink = $"{baseUrl}/verify-email?token={token}";

                // Create HTML email template
                var emailBody = $@"
                    <html>
                    <head>
                        <style>
                            body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                            .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                            .header {{ background-color: #FFC107; color: #000; padding: 20px; text-align: center; }}
                            .content {{ padding: 20px; background-color: #fff; }}
                            .button {{ display: inline-block; padding: 12px 24px; background-color: #FFC107; color: #000; text-decoration: none; border-radius: 4px; margin: 20px 0; }}
                            .footer {{ text-align: center; padding: 20px; color: #666; font-size: 12px; }}
                        </style>
                    </head>
                    <body>
                        <div class='container'>
                            <div class='header'>
                                <h1>Verify Your Email</h1>
                            </div>
                            <div class='content'>
                                <p>Hello {user.UserName},</p>
                                <p>Thank you for registering with our Management System. To complete your registration and verify your email address, please click the button below:</p>
                                <div style='text-align: center;'>
                                    <a href='{verificationLink}' class='button'>Verify Email Address</a>
                                </div>
                                <p>If the button doesn't work, you can copy and paste this link into your browser:</p>
                                <p>{verificationLink}</p>
                                <p>This verification link will expire in 24 hours.</p>
                                <p>If you didn't create an account with us, please ignore this email.</p>
                            </div>
                            <div class='footer'>
                                <p>This is an automated message, please do not reply to this email.</p>
                                <p>Sent at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>
                            </div>
                        </div>
                    </body>
                    </html>";

                try
                {
                    // Send verification email
                    await SendEmailAsync(user.Email, "Verify Your Email - Management System", emailBody);
                    
                    // Only save changes if email was sent successfully
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation($"Verification email sent successfully to {user.Email}");
                    return Ok(new Response { IsSuccess = true, Message = "Verification email sent successfully. Please check your inbox and spam folder." });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to send verification email to {user.Email}");
                    return StatusCode(500, new Response 
                    { 
                        IsSuccess = false, 
                        Message = "Unable to send verification email. Please check your email address or try again later." 
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SendVerificationEmail");
                return StatusCode(500, new Response { IsSuccess = false, Message = "An unexpected error occurred. Please try again later." });
            }
        }

        [HttpGet("VerifyEmail")]
        public async Task<IActionResult> VerifyEmail([FromQuery] string token)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => 
                    u.EmailVerificationToken == token && 
                    u.EmailVerificationTokenExpiry > DateTime.UtcNow);

                if (user == null)
                {
                    return BadRequest(new Response { IsSuccess = false, Message = "Invalid or expired verification token." });
                }

                user.IsEmailVerified = true;
                user.EmailVerificationToken = null;
                user.EmailVerificationTokenExpiry = null;
                await _context.SaveChangesAsync();

                return Ok(new Response { IsSuccess = true, Message = "Email verified successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying email");
                return StatusCode(500, new Response { IsSuccess = false, Message = "Failed to verify email." });
            }
        }

        private async Task SendEmailAsync(string to, string subject, string body)
        {
            try
            {
                await _emailService.SendEmailAsync(to, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email to {Email}", to);
                throw;
            }
        }

        [HttpPost("{userId}/photo")]
        public async Task<IActionResult> UploadProfilePhoto(int userId, IFormFile file)
        {
            try
            {
                _logger.LogInformation($"Starting profile photo upload for user {userId}");

                if (file == null || file.Length == 0)
                {
                    _logger.LogWarning("No file was uploaded");
                    return BadRequest(new Response<string>
                    {
                        IsSuccess = false,
                        Message = "No file was uploaded"
                    });
                }

                _logger.LogInformation($"Received file: {file.FileName}, Size: {file.Length}, Content Type: {file.ContentType}");

                // Validate file size (5MB max)
                if (file.Length > 5 * 1024 * 1024)
                {
                    _logger.LogWarning($"File size {file.Length} exceeds limit");
                    return BadRequest(new Response<string>
                    {
                        IsSuccess = false,
                        Message = "File size must be less than 5MB"
                    });
                }

                // Validate file type
                var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif" };
                if (!allowedTypes.Contains(file.ContentType.ToLower()))
                {
                    _logger.LogWarning($"Invalid content type: {file.ContentType}");
                    return BadRequest(new Response<string>
                    {
                        IsSuccess = false,
                        Message = "Only .jpg, .jpeg, .png, and .gif files are allowed"
                    });
                }

                // Ensure wwwroot directory exists
                var wwwrootPath = _environment.WebRootPath;
                if (string.IsNullOrEmpty(wwwrootPath))
                {
                    wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                    Directory.CreateDirectory(wwwrootPath);
                }

                // Create uploads and profile-photos directories
                var uploadsPath = Path.Combine(wwwrootPath, "uploads");
                var profilePhotosPath = Path.Combine(uploadsPath, "profile-photos");
                
                _logger.LogInformation($"Creating directories if not exist: {uploadsPath} and {profilePhotosPath}");
                Directory.CreateDirectory(uploadsPath);
                Directory.CreateDirectory(profilePhotosPath);

                // Create unique filename
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                var filePath = Path.Combine(profilePhotosPath, fileName);
                _logger.LogInformation($"Saving file to: {filePath}");

                // Save the file
                try
                {
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }
                    _logger.LogInformation("File saved successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to save file to disk");
                    return StatusCode(500, new Response<string>
                    {
                        IsSuccess = false,
                        Message = "Failed to save the uploaded file"
                    });
                }

                try
                {
                    // Update user details in database
                    var userDetails = await _context.UserDetails.FirstOrDefaultAsync(ud => ud.UserId == userId);
                    if (userDetails == null)
                    {
                        userDetails = new UserDetails
                        {
                            UserId = userId
                        };
                        _context.UserDetails.Add(userDetails);
                        _logger.LogInformation($"Created new UserDetails record for user {userId}");
                    }

                    // Delete old photo if exists
                    if (!string.IsNullOrEmpty(userDetails.PhotoUrl))
                    {
                        var oldPhotoPath = Path.Combine(wwwrootPath, userDetails.PhotoUrl.TrimStart('/'));
                        if (System.IO.File.Exists(oldPhotoPath))
                        {
                            try
                            {
                                System.IO.File.Delete(oldPhotoPath);
                                _logger.LogInformation($"Deleted old profile photo: {oldPhotoPath}");
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to delete old profile photo");
                            }
                        }
                    }

                    // Update photo URL
                    var photoUrl = $"/uploads/profile-photos/{fileName}";
                    userDetails.PhotoUrl = photoUrl;
                    userDetails.PhotoFileName = fileName;
                    userDetails.PhotoContentType = file.ContentType;

                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Database updated with new photo URL: {photoUrl}");

                    return Ok(new Response<string>
                    {
                        IsSuccess = true,
                        Message = "Photo uploaded successfully",
                        Data = photoUrl
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to update database with new photo");
                    // Try to clean up the uploaded file
                    try
                    {
                        if (System.IO.File.Exists(filePath))
                        {
                            System.IO.File.Delete(filePath);
                            _logger.LogInformation("Cleaned up file after database error");
                        }
                    }
                    catch
                    {
                        // Ignore cleanup errors
                    }
                    return StatusCode(500, new Response<string>
                    {
                        IsSuccess = false,
                        Message = "Failed to save photo information to database"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error during photo upload for user {userId}");
                return StatusCode(500, new Response<string>
                {
                    IsSuccess = false,
                    Message = "An unexpected error occurred while uploading the photo"
                });
            }
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] UserRegistrationDto model)
        {
            try
            {
                _logger.LogInformation("Starting registration process for user: {Username}", model.Username);

                if (!ModelState.IsValid)
                {
                    var errors = string.Join(", ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage));
                    _logger.LogWarning("Invalid model state during registration: {Errors}", errors);
                    return BadRequest(new Response { IsSuccess = false, Message = errors });
                }

                // Check if email is already registered
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    _logger.LogWarning("Registration failed: Email {Email} is already registered", model.Email);
                    return BadRequest(new Response { IsSuccess = false, Message = "Email is already registered" });
                }

                // Check if username is already taken
                var existingUsername = await _userManager.FindByNameAsync(model.Username);
                if (existingUsername != null)
                {
                    _logger.LogWarning("Registration failed: Username {Username} is already taken", model.Username);
                    return BadRequest(new Response { IsSuccess = false, Message = "Username is already taken" });
                }

                // Check if contact number is already registered
                var existingContact = await _context.UserDetails
                    .AnyAsync(u => u.ContactNumber == model.ContactNumber);
                if (existingContact)
                {
                    _logger.LogWarning("Registration failed: Contact number {ContactNumber} is already registered", model.ContactNumber);
                    return BadRequest(new Response { IsSuccess = false, Message = "Contact number is already registered" });
                }

                // Create new user
                var newUser = new User
                {
                    UserName = model.Username,
                    Email = model.Email,
                    Role = UserRoles.Client,
                    IsActive = false,
                    IsEmailVerified = false,
                    EmailVerificationToken = Guid.NewGuid().ToString(),
                    EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24),
                    CreatedAt = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(newUser, model.Password);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogError("Failed to create user: {Errors}", errors);
                    return BadRequest(new Response { IsSuccess = false, Message = errors });
                }

                // Add user to Client role
                result = await _userManager.AddToRoleAsync(newUser, UserRoles.Client);
                if (!result.Succeeded)
                {
                    await _userManager.DeleteAsync(newUser);
                    _logger.LogError("Failed to add user to Client role");
                    return BadRequest(new Response { IsSuccess = false, Message = "Failed to assign user role" });
                }

                // Create user details
                var userDetails = new UserDetails
                {
                    UserId = newUser.Id,
                    ContactNumber = model.ContactNumber,
                    Gender = model.Gender,
                    Nationality = model.Nationality,
                    DateOfBirth = model.DateOfBirth
                };

                _context.UserDetails.Add(userDetails);
                await _context.SaveChangesAsync();

                // Send verification email
                var verificationLink = $"{Request.Scheme}://{Request.Host}/verify-email?token={newUser.EmailVerificationToken}";
                var emailBody = $@"
                    <html>
                    <body>
                        <h2>Welcome to Management System!</h2>
                        <p>Hello {newUser.UserName},</p>
                        <p>Thank you for registering. Please verify your email address by clicking the link below:</p>
                        <p><a href='{verificationLink}'>Verify Email Address</a></p>
                        <p>If the link doesn't work, copy and paste this URL into your browser:</p>
                        <p>{verificationLink}</p>
                        <p>This link will expire in 24 hours.</p>
                    </body>
                    </html>";

                try
                {
                    await _emailService.SendEmailAsync(newUser.Email, "Verify Your Email - Management System", emailBody);
                    _logger.LogInformation("Verification email sent to: {Email}", newUser.Email);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send verification email to {Email}", newUser.Email);
                    // Continue with registration even if email fails
                }

                _logger.LogInformation("Registration successful for user: {Username}", model.Username);
                return Ok(new Response { IsSuccess = true, Message = "Registration successful! Please wait for an administrator to activate your account before logging in." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration");
                return StatusCode(500, new Response { IsSuccess = false, Message = "An error occurred during registration. Please try again later." });
            }
        }

        [HttpGet("GetClientProfiles")]
        [Authorize(Roles = "SuperAdmin,Admin,User,Client")]
        public async Task<IActionResult> GetClientProfiles()
        {
            try
            {
                _logger.LogInformation("GetClientProfiles endpoint called");
                
                var users = await _context.Users
                    .Include(u => u.UserDetails)
                    .Where(u => u.Role == "Client")
                    .Select(u => new UserDto
                    {
                        Id = u.Id,
                        UserId = u.Id,
                        Email = u.Email,
                        Username = u.UserName,
                        Role = u.Role,
                        IsActive = u.IsActive,
                        CreatedAt = u.CreatedAt,
                        LastLoginAt = u.LastLoginAt,
                        IsEmailVerified = u.IsEmailVerified,
                        PhoneNumber = u.UserDetails.ContactNumber
                    })
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} client profiles from database", users.Count);

                var response = new Response<List<UserDto>>
                {
                    IsSuccess = true,
                    Message = "Client profiles retrieved successfully",
                    Data = users
                };

                _logger.LogInformation("Returning response with {Count} client profiles", users.Count);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving client profiles");
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new Response<List<UserDto>>
                    {
                        IsSuccess = false,
                        Message = $"An error occurred while retrieving client profiles: {ex.Message}",
                        Data = null
                    });
            }
        }
    }
}
