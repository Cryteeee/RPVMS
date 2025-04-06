using System;
using System.Threading.Tasks;
using BlazorApp1.Shared.Models;
using BlazorApp1.Shared.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using BlazorApp1.Server.Utilities;
using BlazorApp1.Shared;

namespace BlazorApp1.Server.Data
{
    public static class SuperAdminInitializer
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

            try
            {
                // Ensure database exists and is up to date
                await context.Database.MigrateAsync();

                // Check if SuperAdmin exists
                var superAdmin = await userManager.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Role == "SuperAdmin");

                if (superAdmin == null)
                {
                    // Create Super Admin user with the correct password hashing
                    var newSuperAdmin = new User
                    {
                        UserName = "superadmin",
                        Email = "superadmin@village.com",
                        Role = "SuperAdmin",
                        IsEmailVerified = true,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        LastLoginAt = null
                    };

                    var result = await userManager.CreateAsync(newSuperAdmin, "Admin@123");
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(newSuperAdmin, "SuperAdmin");
                        logger.LogInformation("SuperAdmin user created successfully.");
                    }
                    else
                    {
                        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                        throw new Exception($"Failed to create SuperAdmin user: {errors}");
                    }
                }
                else
                {
                    // Reset existing SuperAdmin password and settings
                    superAdmin = await userManager.FindByEmailAsync("superadmin@village.com");
                    if (superAdmin != null)
                    {
                        var token = await userManager.GeneratePasswordResetTokenAsync(superAdmin);
                        var result = await userManager.ResetPasswordAsync(superAdmin, token, "Admin@123");
                        
                        if (result.Succeeded)
                        {
                            superAdmin.IsEmailVerified = true;
                            superAdmin.IsActive = true;
                            await context.SaveChangesAsync();
                            logger.LogInformation("Existing SuperAdmin password reset and account activated.");
                        }
                        else
                        {
                            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                            logger.LogWarning($"Failed to reset SuperAdmin password: {errors}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while initializing SuperAdmin: {Message}", ex.Message);
                throw;
            }
        }
    }
} 