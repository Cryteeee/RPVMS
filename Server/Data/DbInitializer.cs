using BlazorApp1.Shared.Models;
using BlazorApp1.Shared.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace BlazorApp1.Server.Data
{
    public static class DbInitializer
    {
        public static void Initialize(ApplicationDbContext context)
        {
            context.Database.EnsureCreated();

            // Check if any users exist
            if (!context.Users.Any())
            {
                // Create default user
                var defaultUser = new User
                {
                    Email = "user@example.com",
                    UserName = "defaultuser",
                    PasswordHash = HashPassword("Password123!"), // This should be changed in production
                    IsEmailVerified = true,
                    UserDetails = new UserDetails
                    {
                        Gender = "Not Specified",
                        Nationality = "Not Specified"
                    }
                };

                context.Users.Add(defaultUser);
                context.SaveChanges();
            }
        }

        private static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }
    }
} 