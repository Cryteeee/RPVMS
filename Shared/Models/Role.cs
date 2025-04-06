using Microsoft.AspNetCore.Identity;

namespace BlazorApp1.Shared.Models
{
    public class Role : IdentityRole<int>
    {
        public Role() : base()
        {
        }

        public Role(string roleName) : base(roleName)
        {
        }

        // Add parameterless constructor required by EF Core
        public Role(string roleName, string? description = null) : base(roleName)
        {
            Description = description;
        }

        // Optional: Add additional properties
        public string? Description { get; set; }
    }
} 