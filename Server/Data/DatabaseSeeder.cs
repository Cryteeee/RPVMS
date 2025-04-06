using BlazorApp1.Server.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Text.RegularExpressions;
using BlazorApp1.Shared.Models;
using BlazorApp1.Shared.Constants;
using Microsoft.AspNetCore.Identity;
using System.Security.Cryptography;
using System.Text;

namespace BlazorApp1.Server.Data
{
    public static class DatabaseSeeder
    {
        private static readonly Regex PasswordComplexityRegex = new(
            @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$",
            RegexOptions.Compiled);

        public static async Task SeedSuperAdmin(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            try
            {
                logger.LogInformation("Starting Super Admin seeding process...");

                // Check if Super Admin exists
                var superAdmin = await context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Role == "SuperAdmin");

                if (superAdmin == null)
                {
                    logger.LogInformation("No Super Admin found. Creating one...");

                    // Get SuperAdmin credentials from configuration
                    var username = configuration["SuperAdmin:Username"];
                    var email = configuration["SuperAdmin:Email"];
                    var password = configuration["SuperAdmin:Password"];

                    // Validate configuration
                    if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                    {
                        throw new InvalidOperationException("SuperAdmin credentials not properly configured in appsettings.json");
                    }

                    // Validate email format
                    if (!IsValidEmail(email))
                    {
                        throw new InvalidOperationException($"Invalid email format for SuperAdmin: {email}");
                    }

                    // Validate password complexity
                    if (!IsValidPassword(password))
                    {
                        throw new InvalidOperationException("SuperAdmin password does not meet complexity requirements. " +
                            "Password must be at least 8 characters long and contain at least one uppercase letter, " +
                            "one lowercase letter, one number, and one special character.");
                    }

                    // Create Super Admin user
                    var newSuperAdmin = new User
                    {
                        UserName = username,
                        Email = email,
                        PasswordHash = PasswordUtility.HashPassword(password),
                        Role = "SuperAdmin",
                        IsEmailVerified = true, // Super Admin is automatically verified
                        IsActive = true, // Super Admin is automatically active
                        CreatedAt = DateTime.UtcNow
                    };

                    context.Users.Add(newSuperAdmin);
                    await context.SaveChangesAsync();

                    logger.LogInformation("Super Admin user created successfully with username: {Username}", username);
                }
                else
                {
                    // Verify SuperAdmin status
                    if (!superAdmin.IsActive || !superAdmin.IsEmailVerified)
                    {
                        logger.LogWarning("Existing Super Admin account needs activation. Updating status...");
                        
                        var existingSuperAdmin = await context.Users.FindAsync(superAdmin.Id);
                        if (existingSuperAdmin != null)
                        {
                            existingSuperAdmin.IsActive = true;
                            existingSuperAdmin.IsEmailVerified = true;
                            await context.SaveChangesAsync();
                            logger.LogInformation("Super Admin status updated successfully.");
                        }
                    }
                    else
                    {
                        logger.LogInformation("Super Admin user already exists and is properly configured.");
                    }
                }
            }
            catch (DbUpdateException ex)
            {
                logger.LogError(ex, "Database error while seeding Super Admin: {Message}", ex.Message);
                throw new Exception("A database error occurred while creating the Super Admin account.", ex);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while seeding the Super Admin user: {Message}", ex.Message);
                throw; // Rethrow to ensure the error is handled by the calling code
            }
        }

        private static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsValidPassword(string password)
        {
            return !string.IsNullOrEmpty(password) && PasswordComplexityRegex.IsMatch(password);
        }
    }
} 