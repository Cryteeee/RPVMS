using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using BlazorApp1.Shared.Models;
using BlazorApp1.Shared.Enums;
using BlazorApp1.Shared.Constants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.Extensions.Configuration;
using System.Net.Mail;
using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using BlazorApp1.Server.Hubs;
using BlazorApp1.Server.Data;
using System.IO;
using System.Text.RegularExpressions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using BlazorApp1.Server.Utilities;
using BlazorApp1.Shared;
using BlazorApp1.Server.Services;
using BCrypt.Net;
using Microsoft.AspNetCore.WebUtilities;

namespace BlazorApp1.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<AccountController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly IHubContext<UserHub> _userHubContext;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole<int>> _roleManager;
        private readonly INotificationService _notificationService;

        public AccountController(
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            ILogger<AccountController> logger,
            IConfiguration configuration,
            IEmailService emailService,
            IHubContext<NotificationHub> hubContext,
            IHubContext<UserHub> userHubContext,
            UserManager<User> userManager,
            RoleManager<IdentityRole<int>> roleManager,
            INotificationService notificationService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
            _userHubContext = userHubContext ?? throw new ArgumentNullException(nameof(userHubContext));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
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

        private async Task SendVerificationEmail(string email, string token)
        {
            var verificationLink = $"{Request.Scheme}://{Request.Host}/verify-email?token={token}";
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
                            <p>Hello,</p>
                            <p>Please verify your email address by clicking the button below:</p>
                            <div style='text-align: center;'>
                                <a href='{verificationLink}' class='button'>Verify Email Address</a>
                            </div>
                            <p>If the button doesn't work, you can copy and paste this link into your browser:</p>
                            <p>{verificationLink}</p>
                            <p>This verification link will expire in 24 hours.</p>
                            <p>If you didn't request this verification email, please ignore it or contact support.</p>
                        </div>
                        <div class='footer'>
                            <p>This is an automated message, please do not reply to this email.</p>
                            <p>Sent at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>
                        </div>
                    </div>
                </body>
                </html>";

            await SendEmailAsync(email, "Verify Your Email - Management System", emailBody);
        }

        [HttpPut("AddUpdate")]
        public async Task<IActionResult> AddUpdate(RegistrationView entity)
        {
            try
            {
                var data = await _context.Users.FirstOrDefaultAsync(a => a.Id == entity.UserId);

                if (data == null)
                {
                    return NotFound(new Response { IsSuccess = false, Message = "User not found" });
                }

                if (string.IsNullOrEmpty(entity.CurrentPassword))
                {
                    return BadRequest(new Response { IsSuccess = false, Message = "Current password is required for making changes" });
                }

                bool validPassword = PasswordUtility.VerifyPassword(entity.CurrentPassword, data.PasswordHash);
                if (!validPassword)
                {
                    return BadRequest(new Response { IsSuccess = false, Message = "Current password is incorrect" });
                }

                if (data.Role == "SuperAdmin")
                {
                    return BadRequest(new Response { IsSuccess = false, Message = "PROHIBITED: SuperAdmin credentials cannot be modified for security reasons." });
                }

                if (!string.IsNullOrEmpty(entity.Email))
                {
                    if (data.Role == "SuperAdmin")
                    {
                        return BadRequest(new Response { IsSuccess = false, Message = "PROHIBITED: SuperAdmin email cannot be modified for security reasons." });
                    }

                    if (!Regex.IsMatch(entity.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                    {
                        return BadRequest(new Response { IsSuccess = false, Message = "Invalid email format" });
                    }

                    var emailExists = await _context.Users
                        .AnyAsync(u => u.Email.ToLower() == entity.Email.ToLower() && u.Id != entity.UserId);
                    if (emailExists)
                    {
                        return BadRequest(new Response { IsSuccess = false, Message = "Email is already in use" });
                    }

                    if (data.Email.ToLower() != entity.Email.ToLower())
                    {
                        _logger.LogInformation($"Email change detected for user {data.Id}. Old: {data.Email}, New: {entity.Email}");
                        data.Email = entity.Email;
                        data.IsEmailVerified = false;
                        data.EmailVerificationToken = Guid.NewGuid().ToString();
                        data.EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24);

                        // Send verification email for the new/changed email address
                        var verificationLink = $"{Request.Scheme}://{Request.Host}/verify-email?token={data.EmailVerificationToken}";
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
                                        <p>Hello {data.UserName},</p>
                                        <p>You have changed your email address. To ensure account security, you need to verify this email address before you can use all account features.</p>
                                        <div style='text-align: center;'>
                                            <a href='{verificationLink}' class='button'>Verify Email Address</a>
                                        </div>
                                        <p>If the button doesn't work, you can copy and paste this link into your browser:</p>
                                        <p>{verificationLink}</p>
                                        <p>This verification link will expire in 24 hours.</p>
                                        <p>If you didn't make this change, please contact support immediately.</p>
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
                            await SendEmailAsync(entity.Email, "Verify Your Email - Management System", emailBody);
                            _logger.LogInformation($"Verification email sent to new address: {entity.Email}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to send verification email for new address");
                            // Continue with the update even if email sending fails
                        }
                    }
                }

                if (!string.IsNullOrEmpty(entity.Username))
                {
                    if (data.Role == "SuperAdmin")
                    {
                        return BadRequest(new Response { IsSuccess = false, Message = "PROHIBITED: SuperAdmin username cannot be modified for security reasons." });
                    }

                    if (entity.Username.Length < 3 || entity.Username.Length > 50)
                    {
                        return BadRequest(new Response { IsSuccess = false, Message = "Username must be between 3 and 50 characters" });
                    }

                    var usernameExists = await _context.Users
                        .AnyAsync(u => u.UserName.ToLower() == entity.Username.ToLower() && u.Id != entity.UserId);
                    if (usernameExists)
                    {
                        return BadRequest(new Response { IsSuccess = false, Message = "Username is already in use" });
                    }

                    data.UserName = entity.Username;
                }

                if (!string.IsNullOrEmpty(entity.Password))
                {
                    data.PasswordHash = PasswordUtility.HashPassword(entity.Password);
                }

                _context.Users.Update(data);
                await _context.SaveChangesAsync();

                return Ok(new Response { IsSuccess = true, Message = "Account updated successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating account");
                return StatusCode(500, new Response { IsSuccess = false, Message = ex.Message });
            }
        }

        [HttpGet("check-email")]
        [AllowAnonymous]
        public async Task<IActionResult> CheckEmailAvailability([FromQuery] string email)
        {
            try
            {
                var exists = await _context.Users.AnyAsync(u => u.Email.ToLower() == email.ToLower());
                return Ok(new Response { IsSuccess = !exists, Message = exists ? "Email is already registered" : "Email is available" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking email availability");
                return StatusCode(500, new Response { IsSuccess = false, Message = "Error checking email availability" });
            }
        }

        [HttpGet("check-contact-number")]
        [AllowAnonymous]
        public async Task<IActionResult> CheckContactNumberAvailability([FromQuery] string contactNumber)
        {
            try
            {
                var exists = await _context.UserDetails.AnyAsync(u => u.ContactNumber == contactNumber);
                return Ok(new Response { IsSuccess = !exists, Message = exists ? "Contact number is already registered" : "Contact number is available" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking contact number availability");
                return StatusCode(500, new Response { IsSuccess = false, Message = "Error checking contact number availability" });
            }
        }

        [HttpPost("Register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] UserRegistrationDto model)
        {
            try
            {
                _logger.LogInformation("Starting registration process for user: {Username}", model.Username);

                // Basic validation
                if (!ModelState.IsValid)
                {
                    var errors = string.Join(", ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage));
                    _logger.LogWarning("Invalid model state during registration: {Errors}", errors);
                    return BadRequest(new Response { IsSuccess = false, Message = $"Invalid model state: {errors}" });
                }

                // Validate contact number format
                if (string.IsNullOrEmpty(model.ContactNumber))
                {
                    _logger.LogWarning("Contact number is required");
                    return BadRequest(new Response { IsSuccess = false, Message = "Contact number is required" });
                }

                if (!Regex.IsMatch(model.ContactNumber, @"^09\d{9}$"))
                {
                    _logger.LogWarning("Invalid contact number format: {ContactNumber}", model.ContactNumber);
                    return BadRequest(new Response { IsSuccess = false, Message = "Contact number must start with '09' and contain exactly 11 digits" });
                }

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Check if contact number is already in use
                    var isContactNumberTaken = await _context.UserDetails
                        .AnyAsync(u => u.ContactNumber == model.ContactNumber);

                    if (isContactNumberTaken)
                    {
                        _logger.LogWarning("Registration failed: Contact number {ContactNumber} is already registered", model.ContactNumber);
                        return BadRequest(new Response { IsSuccess = false, Message = "Contact number is already registered" });
                    }

                    // Check email uniqueness
                    try
                    {
                        var emailExists = await _userManager.FindByEmailAsync(model.Email);
                        if (emailExists != null)
                        {
                            _logger.LogWarning("Registration failed: Email {Email} is already in use", model.Email);
                            return BadRequest(new Response { IsSuccess = false, Message = "Email is already in use" });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error checking email uniqueness");
                        throw new Exception("Error checking email uniqueness", ex);
                    }

                    // Check username uniqueness
                    try
                    {
                        var usernameExists = await _userManager.FindByNameAsync(model.Username);
                        if (usernameExists != null)
                        {
                            _logger.LogWarning("Registration failed: Username {Username} is already in use", model.Username);
                            return BadRequest(new Response { IsSuccess = false, Message = "Username is already in use" });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error checking username uniqueness");
                        throw new Exception("Error checking username uniqueness", ex);
                    }

                    _logger.LogInformation("Creating new user with Client role");

                    // Create new user
                    var user = new User
                    {
                        UserName = model.Username,
                        Email = model.Email,
                        Role = UserRoles.Client, // Set default role
                        IsActive = false // Set to false by default
                    };

                    // Create the user using UserManager
                    var result = await _userManager.CreateAsync(user, model.Password);
                    if (!result.Succeeded)
                    {
                        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                        _logger.LogError("Failed to create user: {Errors}", errors);
                        throw new Exception($"Failed to create user: {errors}");
                    }

                    // Add user to the Client role in ASP.NET Core Identity
                    await _userManager.AddToRoleAsync(user, UserRoles.Client);
                    
                    // Create user details
                    var userDetails = new UserDetails
                    {
                        UserId = user.Id,
                        FullName = $"{model.Username}",  // Using username as initial full name since we don't have first/last name
                        ContactNumber = model.ContactNumber
                    };
                    
                    _context.UserDetails.Add(userDetails);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Successfully created user details for user ID: {UserId}", user.Id);

                    // Generate email verification token and send email
                    var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
                    
                    // Send verification email
                    await SendVerificationEmail(user.Email, encodedToken);

                    // Send notification to SuperAdmin and Admin users
                    try
                    {
                        _logger.LogInformation("Starting notification process for new user registration");
                        
                        // Get all SuperAdmin and Admin users
                        var adminUsers = await _context.Users
                            .Where(u => u.Role == UserRoles.SuperAdmin || u.Role == UserRoles.Admin)
                            .ToListAsync();

                        _logger.LogInformation("Found {Count} admin users to notify. SuperAdmin: {SuperAdminCount}, Admin: {AdminCount}", 
                            adminUsers.Count,
                            adminUsers.Count(u => u.Role == UserRoles.SuperAdmin),
                            adminUsers.Count(u => u.Role == UserRoles.Admin));

                        if (adminUsers.Any())
                        {
                            // Create notification message
                            _logger.LogInformation("Creating notification message for user {UserId}", user.Id);
                            var notificationMessage = new BoardMessage
                            {
                                UserId = user.Id,
                                Content = $"New user registration: {user.UserName} ({user.Email})",
                                Timestamp = DateTime.UtcNow,
                                Priority = MessagePriority.Normal,
                                IsRead = false,
                                Type = "UserRegistration"
                            };

                            // Save the message to the database
                            _context.BoardMessages.Add(notificationMessage);
                            await _context.SaveChangesAsync();
                            _logger.LogInformation("Created notification message with ID {MessageId}", notificationMessage.MessageId);

                            // Create DTO for SignalR
                            var notificationDto = new BoardMessageDto
                            {
                                MessageId = notificationMessage.MessageId,
                                UserId = notificationMessage.UserId,
                                Content = notificationMessage.Content,
                                Timestamp = notificationMessage.Timestamp,
                                Priority = notificationMessage.Priority,
                                IsRead = notificationMessage.IsRead,
                                Type = notificationMessage.Type
                            };

                            // Send through SignalR hub directly
                            await _hubContext.Clients.Group("Admins").SendAsync("ReceiveNotification", notificationDto);
                            _logger.LogInformation("Sent notification through SignalR to Admins group");

                            // Also send through NotificationService
                            await _notificationService.SendNotificationAsync(notificationMessage, adminUsers.Select(u => u.Id).ToList());
                            _logger.LogInformation("Sent notification through NotificationService");
                        }
                        else
                        {
                            _logger.LogWarning("No admin users found to notify about new user registration");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send admin notifications for new user registration");
                        // Continue with registration even if notification fails
                    }

                    // Commit transaction
                    await transaction.CommitAsync();
                    _logger.LogInformation("Successfully completed registration for user ID: {UserId}", user.Id);

                    return Ok(new Response { IsSuccess = true, Message = "Registration successful. Please check your email to verify your account." });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Transaction failed during registration");
                    await transaction.RollbackAsync();
                    throw new Exception("Failed to complete registration process", ex);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration: {ErrorMessage}", ex.Message);
                if (ex.InnerException != null)
                {
                    _logger.LogError(ex.InnerException, "Inner exception during registration: {ErrorMessage}", ex.InnerException.Message);
                }
                return StatusCode(500, new Response { IsSuccess = false, Message = $"An error occurred during registration: {ex.Message}" });
            }
        }

        [HttpPost("Login")]
        [AllowAnonymous]
        public async Task<ActionResult<Response<string>>> Login([FromBody] LoginVm model)
        {
            try
            {
                _logger.LogInformation($"Login attempt for email: {model.Email}");
                
                var user = await _userManager.FindByEmailAsync(model.Email);

                if (user == null)
                {
                    _logger.LogWarning($"Login failed - user not found for email: {model.Email}");
                    return BadRequest(new Response<string>
                    {
                        IsSuccess = false,
                        Message = "Invalid email or password",
                        Data = null
                    });
                }

                var result = await _userManager.CheckPasswordAsync(user, model.Password);
                if (!result)
                {
                    _logger.LogWarning($"Login failed - invalid password for email: {model.Email}");
                    return BadRequest(new Response<string>
                    {
                        IsSuccess = false,
                        Message = "Invalid email or password",
                        Data = null
                    });
                }

                // Special handling for SuperAdmin
                if (user.Role == "SuperAdmin")
                {
                    // SuperAdmin can always log in
                    user.IsActive = true;
                    user.IsEmailVerified = true;
                }
                else
                {
                    // For non-SuperAdmin users, check if account is active
                    if (!user.IsActive)
                    {
                        return BadRequest(new Response<string>
                        {
                            IsSuccess = false,
                            Message = "Your account is not active. Please wait for an administrator to activate your account.",
                            Data = null
                        });
                    }
                }

                // Update LastLoginAt
                user.LastLoginAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Generating JWT token for user: {user.Email}");

                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_configuration["JwtSettings:SecretKey"]);
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
                var tokenString = tokenHandler.WriteToken(token);

                _logger.LogInformation($"Login successful for user: {user.Email}");

                return Ok(new Response<string>
                {
                    IsSuccess = true,
                    Message = user.IsEmailVerified ? "Login successful" : "Login successful. Please verify your email to access all features.",
                    Data = tokenString
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                return StatusCode(500, new Response<string>
                {
                    IsSuccess = false,
                    Message = "An error occurred during login",
                    Data = null
                });
            }
        }

        [HttpGet("GetById/{id}")]
        public async Task<ActionResult<Response<RegistrationView>>> GetById(int id)
        {
            try
            {
                _logger.LogInformation($"Getting user details for ID: {id}");

                var user = await _context.Users
                    .Include(r => r.UserDetails)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (user == null)
                {
                    _logger.LogWarning($"User not found for ID: {id}");
                    return Ok(new Response<RegistrationView>
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
                    IsEmailVerified = user.IsEmailVerified
                };

                _logger.LogInformation($"Successfully retrieved user details for ID: {id}");
                
                return Ok(new Response<RegistrationView>
                {
                    IsSuccess = true,
                    Message = "User details retrieved successfully",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving user details for ID: {id}");
                return Ok(new Response<RegistrationView>
                {
                    IsSuccess = false,
                    Message = "An error occurred while retrieving user details",
                    Data = null
                });
            }
        }

        [HttpPut("UpdatePassword")]
        public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordDto dto)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(a => a.Id == dto.UserId);

                if (user == null)
                {
                    return NotFound(new Response { IsSuccess = false, Message = "User not found." });
                }

                bool validPassword = PasswordUtility.VerifyPassword(dto.CurrentPassword, user.PasswordHash);
                if (!validPassword)
                {
                    return BadRequest(new Response { IsSuccess = false, Message = "Current password is incorrect." });
                }

                // Additional security check for SuperAdmin
                if (user.Role == "SuperAdmin")
                {
                    if (string.IsNullOrEmpty(dto.SecurityKey))
                    {
                        return BadRequest(new Response { IsSuccess = false, Message = "Security key is required for SuperAdmin." });
                    }

                    string superAdminSecurityKey = _configuration["SuperAdminSecurityKey"] ?? "breakpoint";
                    
                    if (dto.SecurityKey != superAdminSecurityKey)
                    {
                        return BadRequest(new Response { IsSuccess = false, Message = "Invalid Security Key" });
                    }
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
                _logger.LogError(ex, "Error updating password");
                return StatusCode(500, new Response { IsSuccess = false, Message = ex.Message });
            }
        }

        private bool IsValidPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password)) return false;
            if (password.Length < 8) return false;
            if (!password.Any(char.IsUpper)) return false;
            if (!password.Any(char.IsLower)) return false;
            if (!password.Any(char.IsDigit)) return false;
            return true;
        }

        [HttpPost("RequestPasswordReset")]
        [AllowAnonymous]
        public async Task<IActionResult> RequestPasswordReset([FromBody] string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                {
                    return BadRequest(new Response { IsSuccess = false, Message = "Email is required" });
                }

                if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                {
                    return BadRequest(new Response { IsSuccess = false, Message = "Invalid email format" });
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
                if (user == null)
                {
                    // Return specific message for unrecognized email
                    return BadRequest(new Response { IsSuccess = false, Message = "not registered" });
                }

                if (!user.IsEmailVerified)
                {
                    return BadRequest(new Response { IsSuccess = false, Message = "Email is not verified. Please verify email first." });
                }

                // Check if enough time has passed since the last password reset email
                if (user.LastPasswordResetEmailSentTime.HasValue)
                {
                    var timeSinceLastEmail = DateTime.UtcNow.Subtract(user.LastPasswordResetEmailSentTime.Value).TotalSeconds;
                    if (timeSinceLastEmail < 60)
                    {
                        var secondsToWait = Math.Ceiling(60 - timeSinceLastEmail);
                        return BadRequest(new Response 
                        { 
                            IsSuccess = false, 
                            Message = $"Please wait {secondsToWait} seconds before requesting another password reset email." 
                        });
                    }
                }

                // Generate reset token
                var token = Guid.NewGuid().ToString();
                user.PasswordResetToken = token;
                user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(24);
                user.LastPasswordResetEmailSentTime = DateTime.UtcNow;

                // Create reset link with absolute URL
                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var resetLink = $"{baseUrl}/reset-password?token={token}";

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
                                <h1>Reset Your Password</h1>
                            </div>
                            <div class='content'>
                                <p>Hello,</p>
                                <p>We received a request to reset your password. Click the button below to create a new password:</p>
                                <div style='text-align: center;'>
                                    <a href='{resetLink}' class='button'>Reset Password</a>
                                </div>
                                <p>If the button doesn't work, you can copy and paste this link into your browser:</p>
                                <p>{resetLink}</p>
                                <p>This link will expire in 24 hours.</p>
                                <p>If you didn't request a password reset, please ignore this email.</p>
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
                    // Send reset email
                    await SendEmailAsync(user.Email, "Reset Your Password - Management System", emailBody);
                    
                    // Only save changes if email was sent successfully
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation($"Password reset email sent successfully to {user.Email}");
                    return Ok(new Response { IsSuccess = true, Message = "Password reset instructions have been sent to your email address." });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to send password reset email to {user.Email}");
                    return StatusCode(500, new Response 
                    { 
                        IsSuccess = false, 
                        Message = "Unable to send password reset email. Please try again later." 
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RequestPasswordReset");
                return StatusCode(500, new Response { IsSuccess = false, Message = "An unexpected error occurred. Please try again later." });
            }
        }

        [HttpPost("ResetPassword")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordModel model)
        {
            try
            {
                if (string.IsNullOrEmpty(model.Token) || string.IsNullOrEmpty(model.NewPassword))
                {
                    return BadRequest(new Response { IsSuccess = false, Message = "Invalid request" });
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => 
                    u.PasswordResetToken == model.Token && 
                    u.PasswordResetTokenExpiry > DateTime.UtcNow);

                if (user == null)
                {
                    return BadRequest(new Response { IsSuccess = false, Message = "Invalid or expired reset token." });
                }

                if (!Regex.IsMatch(model.NewPassword, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$"))
                {
                    return BadRequest(new Response { IsSuccess = false, Message = "Password must contain at least one uppercase letter, one lowercase letter, one number, and be at least 8 characters long." });
                }

                user.PasswordHash = PasswordUtility.HashPassword(model.NewPassword);
                user.PasswordResetToken = null;
                user.PasswordResetTokenExpiry = null;
                await _context.SaveChangesAsync();

                return Ok(new Response { IsSuccess = true, Message = "Password has been reset successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password");
                return StatusCode(500, new Response { IsSuccess = false, Message = "Failed to reset password." });
            }
        }

        [HttpPost("SendVerificationEmail")]
        public async Task<IActionResult> SendVerificationEmail([FromBody] SendVerificationEmailRequest request)
        {
            try
            {
                if (request?.UserId == null)
                {
                    return BadRequest(new Response { IsSuccess = false, Message = "UserId is required." });
                }

                var user = await _context.Users.FirstOrDefaultAsync(a => a.Id == request.UserId);
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
                        var remainingSeconds = Math.Ceiling(60 - timeSinceLastEmail);
                        return BadRequest(new Response 
                        { 
                            IsSuccess = false, 
                            Message = $"Please wait {remainingSeconds} seconds before requesting another verification email." 
                        });
                    }
                }

                // Generate new verification token
                user.EmailVerificationToken = Guid.NewGuid().ToString();
                user.EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24);
                user.LastEmailSentTime = DateTime.UtcNow;

                // Create verification link
                var verificationLink = $"{Request.Scheme}://{Request.Host}/verify-email?token={user.EmailVerificationToken}";

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
                                <p>Please verify your email address by clicking the button below:</p>
                                <div style='text-align: center;'>
                                    <a href='{verificationLink}' class='button'>Verify Email Address</a>
                                </div>
                                <p>If the button doesn't work, you can copy and paste this link into your browser:</p>
                                <p>{verificationLink}</p>
                                <p>This verification link will expire in 24 hours.</p>
                                <p>If you didn't request this verification email, please ignore it or contact support.</p>
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
                    
                    // Save changes only after email is sent successfully
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
                        Message = "Unable to send verification email. Please try again later." 
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
                if (string.IsNullOrEmpty(token))
                {
                    return BadRequest(new Response { IsSuccess = false, Message = "Verification token is missing." });
                }

                // Find user with matching token
                var user = await _context.Users.FirstOrDefaultAsync(u => u.EmailVerificationToken == token);

                if (user == null)
                {
                    return BadRequest(new Response { IsSuccess = false, Message = "Invalid verification token. Please request a new verification email." });
                }

                // Check if token is expired
                if (user.EmailVerificationTokenExpiry == null || user.EmailVerificationTokenExpiry < DateTime.UtcNow)
                {
                    return BadRequest(new Response { IsSuccess = false, Message = "Verification token has expired. Please request a new verification email." });
                }

                // Update user verification status
                user.IsEmailVerified = true;
                user.EmailVerificationToken = null;
                user.EmailVerificationTokenExpiry = null;
                user.LastEmailSentTime = null;

                await _context.SaveChangesAsync();
                
                // Notify connected clients about the email verification status change
                await _hubContext.Clients.All.SendAsync("EmailVerificationStatusChanged", user.Id, true);
                
                _logger.LogInformation($"Email verified successfully for user {user.Id}");

                return Ok(new Response { IsSuccess = true, Message = "Your email has been successfully verified! You can now access all account features." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying email with token: {Token}", token);
                return StatusCode(500, new Response { IsSuccess = false, Message = "An error occurred while verifying your email. Please try again later." });
            }
        }

        [HttpPut("role/{userId}")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> UpdateUserRole(int userId, [FromBody] UpdateRoleDto model)
        {
            try
            {
                _logger.LogInformation($"Attempting to update role for user {userId} to {model.Role}");
                
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning($"User not found with ID: {userId}");
                    return NotFound(new Response<bool> { IsSuccess = false, Message = "User not found" });
                }

                _logger.LogInformation($"Current role for user {userId}: {user.Role}");

                if (user.Role == "SuperAdmin")
                {
                    _logger.LogWarning($"Attempt to modify SuperAdmin role for user {userId}");
                    return BadRequest(new Response<bool> { IsSuccess = false, Message = "Cannot modify SuperAdmin role" });
                }

                var oldRole = user.Role;
                user.Role = model.Role;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Successfully updated role for user {userId} from {oldRole} to {model.Role}");
                return Ok(new Response<bool> { IsSuccess = true, Message = "User role updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating role for user {userId}");
                return StatusCode(500, new Response<bool> { IsSuccess = false, Message = "Error updating user role" });
            }
        }

        private async Task NotifyClientCountChanged()
        {
            var activeClientCount = await _context.Users
                .Where(u => u.IsActive && u.Role == UserRoles.Client)
                .CountAsync();
            
            await _userHubContext.Clients.All.SendAsync("ClientCountChanged", activeClientCount);
        }

        [HttpPut("deactivate/{userId}")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> DeactivateUser(int userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return NotFound(new Response { IsSuccess = false, Message = "User not found" });
                }

                if (user.Role == "SuperAdmin")
                {
                    return BadRequest(new Response { IsSuccess = false, Message = "Cannot deactivate SuperAdmin account" });
                }

                user.IsActive = false;
                await _context.SaveChangesAsync();

                // Notify clients about the updated count if the deactivated user was a client
                if (user.Role == UserRoles.Client)
                {
                    await NotifyClientCountChanged();
                }

                return Ok(new Response { IsSuccess = true, Message = "User deactivated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating user");
                return StatusCode(500, new Response { IsSuccess = false, Message = "Error deactivating user" });
            }
        }

        [HttpPut("activate/{userId}")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> ActivateUser(int userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return NotFound(new Response { IsSuccess = false, Message = "User not found" });
                }

                // Set user as active and only set role to Client if they don't have a role
                user.IsActive = true;
                if (string.IsNullOrEmpty(user.Role))
                {
                    user.Role = UserRoles.Client;
                }

                // Only handle email verification if not already verified
                if (!user.IsEmailVerified)
                {
                    // Generate new verification token
                    user.EmailVerificationToken = Guid.NewGuid().ToString();
                    user.EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24);
                    user.LastEmailSentTime = DateTime.UtcNow;

                    // Create verification link
                    var verificationLink = $"{Request.Scheme}://{Request.Host}/verify-email?token={user.EmailVerificationToken}";

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
                                    <p>Your account has been activated! You can now log in and use the system.</p>
                                    <p>To complete your account setup and enable all features, please verify your email address by clicking the button below:</p>
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
                                await SendEmailAsync(user.Email, "Account Activated - Verify Your Email - Management System", emailBody);
                                _logger.LogInformation($"Verification email sent successfully to {user.Email}");
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"Failed to send verification email to {user.Email}");
                                // Continue with activation even if email sending fails
                            }
                }

                // Save changes regardless of email status
                await _context.SaveChangesAsync();

                // Notify clients about the updated count if the activated user is a client
                if (user.Role == UserRoles.Client)
                {
                    await NotifyClientCountChanged();
                }

                var message = user.IsEmailVerified
                    ? "User activated successfully."
                    : "User activated successfully. A verification email has been sent to complete email verification.";

                return Ok(new Response { IsSuccess = true, Message = message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating user");
                return StatusCode(500, new Response { IsSuccess = false, Message = "Error activating user" });
            }
        }

        [HttpDelete("{userId}")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.UserDetails)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    return NotFound(new Response { IsSuccess = false, Message = "User not found" });
                }

                if (user.Role == "SuperAdmin")
                {
                    return BadRequest(new Response { IsSuccess = false, Message = "Cannot delete SuperAdmin account" });
                }

                if (user.UserDetails != null)
                {
                    _context.UserDetails.Remove(user.UserDetails);
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                return Ok(new Response { IsSuccess = true, Message = "User deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user");
                return StatusCode(500, new Response { IsSuccess = false, Message = "Error deleting user" });
            }
        }

        [HttpDelete("Delete/{userId}")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> DeleteUserWithSegment(int userId)
        {
            return await DeleteUser(userId);
        }

        [HttpPut("UpdateAccount")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> UpdateAccount([FromBody] UpdateAccountDto model)
        {
            try
            {
                var user = await _context.Users.FindAsync(model.UserId);
                if (user == null)
                {
                    return NotFound(new Response { IsSuccess = false, Message = "User not found" });
                }

                // Prevent modifying SuperAdmin accounts
                if (user.Role == "SuperAdmin")
                {
                    return BadRequest(new Response { IsSuccess = false, Message = "Cannot modify SuperAdmin account" });
                }

                // Validate current password if changing password
                if (!string.IsNullOrWhiteSpace(model.NewPassword))
                {
                    bool validPassword = PasswordUtility.VerifyPassword(model.CurrentPassword, user.PasswordHash);
                    if (!validPassword)
                    {
                        return BadRequest(new Response { IsSuccess = false, Message = "Current password is incorrect" });
                    }

                    if (!IsValidPassword(model.NewPassword))
                    {
                        return BadRequest(new Response { IsSuccess = false, Message = "New password must contain at least one uppercase letter, one lowercase letter, and one number" });
                    }

                    user.PasswordHash = PasswordUtility.HashPassword(model.NewPassword);
                }

                // Update role if provided
                if (!string.IsNullOrWhiteSpace(model.Role))
                {
                    // Prevent assigning SuperAdmin role
                    if (model.Role == "SuperAdmin")
                    {
                        return BadRequest(new Response { IsSuccess = false, Message = "Cannot assign SuperAdmin role" });
                    }
                    user.Role = model.Role;
                }

                // Update email if provided and different from current
                if (!string.IsNullOrWhiteSpace(model.Email) && model.Email.Trim() != user.Email)
                {
                    // Check if email is already in use by another user
                    var emailExists = await _context.Users
                        .AnyAsync(u => u.Email == model.Email.Trim() && u.Id != model.UserId);
                    if (emailExists)
                    {
                        return BadRequest(new Response { IsSuccess = false, Message = "Email is already in use" });
                    }

                    // Update email and reset verification status
                    user.Email = model.Email.Trim();
                    user.IsEmailVerified = false;
                    user.EmailVerificationToken = Guid.NewGuid().ToString();
                    user.EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24);
                    user.LastEmailSentTime = null;

                    _logger.LogInformation($"Email changed for user {user.Id}. Old verification status reset.");
                }

                // Update username if provided
                if (!string.IsNullOrWhiteSpace(model.Username) && model.Username.Trim() != user.UserName)
                {
                    // Check if username is already in use
                    var usernameExists = await _context.Users
                        .AnyAsync(u => u.UserName == model.Username.Trim() && u.Id != model.UserId);
                    if (usernameExists)
                    {
                        return BadRequest(new Response { IsSuccess = false, Message = "Username is already in use" });
                    }

                    user.UserName = model.Username.Trim();
                }

                await _context.SaveChangesAsync();

                // If email was changed, notify connected clients
                if (!string.IsNullOrWhiteSpace(model.Email) && model.Email.Trim() != user.Email)
                {
                    await _hubContext.Clients.All.SendAsync("EmailVerificationStatusChanged", user.Id, false);
                }

                return Ok(new Response { IsSuccess = true, Message = "Account updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating account for user {UserId}", model.UserId);
                return StatusCode(500, new Response { IsSuccess = false, Message = "An error occurred while updating the account" });
            }
        }

        [HttpPost("ResetSuperAdmin")]
        public async Task<IActionResult> ResetSuperAdmin()
        {
            try
            {
                // First, ensure the SuperAdmin role exists
                var roleExists = await _roleManager.RoleExistsAsync(UserRoles.SuperAdmin);
                if (!roleExists)
                {
                    await _roleManager.CreateAsync(new IdentityRole<int>(UserRoles.SuperAdmin));
                }

                var superAdmin = await _userManager.Users.FirstOrDefaultAsync(u => u.Role == UserRoles.SuperAdmin);
                if (superAdmin == null)
                {
                    // Create SuperAdmin if it doesn't exist
                    superAdmin = new User
                    {
                        UserName = "superadmin",
                        Email = "superadmin@village.com",
                        Role = UserRoles.SuperAdmin, // Set custom Role property
                        IsEmailVerified = true,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    var result = await _userManager.CreateAsync(superAdmin, "SuperAdmin123!");
                    if (!result.Succeeded)
                    {
                        return StatusCode(500, new Response { IsSuccess = false, Message = "Failed to create SuperAdmin user" });
                    }

                    // Ensure the user is in the SuperAdmin role
                    result = await _userManager.AddToRoleAsync(superAdmin, UserRoles.SuperAdmin);
                    if (!result.Succeeded)
                    {
                        return StatusCode(500, new Response { IsSuccess = false, Message = "Failed to assign SuperAdmin role" });
                    }
                }
                else
                {
                    // Reset existing SuperAdmin
                    var token = await _userManager.GeneratePasswordResetTokenAsync(superAdmin);
                    var result = await _userManager.ResetPasswordAsync(superAdmin, token, "SuperAdmin123!");
                    
                    if (!result.Succeeded)
                    {
                        return StatusCode(500, new Response { IsSuccess = false, Message = "Failed to reset SuperAdmin password" });
                    }

                    // Ensure the user is in the SuperAdmin role
                    if (!await _userManager.IsInRoleAsync(superAdmin, UserRoles.SuperAdmin))
                    {
                        result = await _userManager.AddToRoleAsync(superAdmin, UserRoles.SuperAdmin);
                        if (!result.Succeeded)
                        {
                            return StatusCode(500, new Response { IsSuccess = false, Message = "Failed to assign SuperAdmin role" });
                        }
                    }

                    superAdmin.Role = UserRoles.SuperAdmin; // Ensure custom Role property is set
                    superAdmin.IsEmailVerified = true;
                    superAdmin.IsActive = true;
                    await _context.SaveChangesAsync();
                }

                return Ok(new Response { IsSuccess = true, Message = "SuperAdmin account has been reset successfully. Password: SuperAdmin123!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting SuperAdmin");
                return StatusCode(500, new Response { IsSuccess = false, Message = "Failed to reset SuperAdmin" });
            }
        }

        public class SendVerificationEmailRequest
        {
            public int UserId { get; set; }
        }

        public class UpdateRoleDto
        {
            public string Role { get; set; } = string.Empty;
        }
    }
} 