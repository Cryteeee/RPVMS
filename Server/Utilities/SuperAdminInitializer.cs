using BlazorApp1.Server.Data;
using Microsoft.Extensions.Logging;

namespace BlazorApp1.Server.Utilities
{
    public static class SuperAdminInitializer
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

            try
            {
                logger.LogInformation("Starting SuperAdmin initialization...");
                await DatabaseSeeder.SeedSuperAdmin(serviceProvider);
                logger.LogInformation("SuperAdmin initialization completed successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while initializing SuperAdmin: {Message}", ex.Message);
                throw;
            }
        }
    }
} 