using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Claims;
using BlazorApp1.Client.Services;
using BlazorApp1.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using BlazorApp1.Shared.Models;
using BlazorApp1.Server.Data;
using BlazorApp1.Server.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using BlazorApp1.Server.Services;
using BlazorApp1.Shared.Constants;
using Microsoft.AspNetCore.Identity;

namespace BlazorApp1.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserManagementController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<UserManagementController> _logger;
        private readonly IEmailService _emailService;

        public UserManagementController(
            ApplicationDbContext context, 
            IConfiguration configuration,
            ILogger<UserManagementController> logger,
            IEmailService emailService)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _emailService = emailService;
        }

        [HttpGet("check-email")]
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

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegistrationDto userRegistration)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new Response { IsSuccess = false, Message = "Invalid registration data" });
                }

                // Validate email format
                if (!Regex.IsMatch(userRegistration.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                {
                    return BadRequest(new Response { IsSuccess = false, Message = "Invalid email format" });
                }

                // Check if email is already registered
                var isEmailTaken = await _context.Users
                    .AnyAsync(u => u.Email.ToLower() == userRegistration.Email.ToLower());

                if (isEmailTaken)
                {
                    return BadRequest(new Response { IsSuccess = false, Message = "Email is already registered" });
                }

                // Validate password
                if (!Regex.IsMatch(userRegistration.Password, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$"))
                {
                    return BadRequest(new Response { 
                        IsSuccess = false, 
                        Message = "Password must contain at least one uppercase letter, one lowercase letter, one number, and be at least 8 characters long" 
                    });
                }

                // Validate password confirmation
                if (userRegistration.Password != userRegistration.ConfirmPassword)
                {
                    return BadRequest(new Response { IsSuccess = false, Message = "Passwords do not match" });
                }

                // Create new user with Client role
                var newUser = new User
                {
                    UserName = userRegistration.Username,
                    Email = userRegistration.Email,
                    PasswordHash = PasswordUtility.HashPassword(userRegistration.Password),
                    IsEmailVerified = false,
                    EmailVerificationToken = Guid.NewGuid().ToString(),
                    EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24),
                    Role = UserRoles.Client, // Set role to Client by default for new registrations
                    IsActive = false // Explicitly set to false
                };

                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                // Create user details with contact number
                var userDetails = new UserDetails
                {
                    UserId = newUser.Id,
                    ContactNumber = userRegistration.ContactNumber
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
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send verification email to {Email}", newUser.Email);
                    // Don't return error to client, just log it
                }

                return Ok(new Response { IsSuccess = true, Message = "Registration successful! Please wait for an administrator to activate your account before logging in." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration");
                return StatusCode(500, new Response { IsSuccess = false, Message = "An error occurred during registration" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                var user = await _context.Users
                    .Include(r => r.UserDetails)
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (user == null)
                {
                    return NotFound(new Response { IsSuccess = false, Message = "User not found" });
                }

                if (user.UserDetails != null)
                {
                    var userDetails = await _context.UserDetails.FirstOrDefaultAsync(ud => ud.UserId == id);
                    if (userDetails != null)
                    {
                        _context.UserDetails.Remove(userDetails);
                    }
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

        [HttpPost("login")]
        public async Task<IActionResult> LoginAsync([FromBody] LoginVm model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new Response { IsSuccess = false, Message = "Invalid login data" });
                }

                // Validate email format
                if (!Regex.IsMatch(model.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                {
                    return BadRequest(new Response { IsSuccess = false, Message = "Invalid email format" });
                }

                // Find user by email (case-insensitive)
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == model.Email.ToLower());

                if (user == null)
                {
                    _logger.LogWarning($"Login attempt failed: User not found for email {model.Email}");
                    return BadRequest(new Response<string>
                    {
                        IsSuccess = false,
                        Message = "Invalid email or password"
                    });
                }

                // Verify password
                bool validPassword = PasswordUtility.VerifyPassword(model.Password, user.PasswordHash);
                if (!validPassword)
                {
                    _logger.LogWarning($"Login attempt failed: Invalid password for email {model.Email}");
                    return BadRequest(new Response<string>
                    {
                        IsSuccess = false,
                        Message = "Invalid email or password"
                    });
                }

                // Check if user is active (unless they are SuperAdmin)
                if (user.Role != UserRoles.SuperAdmin)
                {
                    if (!user.IsActive)
                    {
                        _logger.LogWarning($"Login attempt failed: Account is not active for email {model.Email}");
                        return BadRequest(new Response<string>
                        {
                            IsSuccess = false,
                            Message = "Your account is not active. Please wait for an administrator to activate your account."
                        });
                    }
                }

                // Generate JWT token
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
                    SigningCredentials = new SigningCredentials(
                        new SymmetricSecurityKey(key), 
                        SecurityAlgorithms.HmacSha256Signature),
                    Issuer = _configuration["JwtSettings:Issuer"],
                    Audience = _configuration["JwtSettings:Audience"],
                    NotBefore = DateTime.UtcNow
                };

                var token = tokenHandler.CreateToken(tokenDescriptor);
                var tokenString = tokenHandler.WriteToken(token);

                _logger.LogInformation($"User {model.Email} logged in successfully");

                return Ok(new Response<string>
                {
                    IsSuccess = true,
                    Message = "Login successful",
                    Data = tokenString
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "JWT configuration error");
                return StatusCode(500, new Response<string>
                {
                    IsSuccess = false,
                    Message = "Server configuration error"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                return StatusCode(500, new Response<string>
                {
                    IsSuccess = false,
                    Message = "An error occurred during login"
                });
            }
        }
    }
}
