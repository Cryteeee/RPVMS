using BlazorApp1.Server.Data;
using BlazorApp1.Shared;
using BlazorApp1.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using BlazorApp1.Shared.Constants;
using Microsoft.AspNetCore.Identity;

namespace BlazorApp1.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public UserController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> GetUser(int id)
        {
            var user = await _context.Users
                .Where(u => u.Id == id)
                .Select(u => new { u.Id, u.Email })
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return NotFound();
            }

            return Ok(user);
        }

        [HttpGet("check-email")]
        public async Task<ActionResult<Response>> CheckEmailAvailability([FromQuery] string email)
        {
            var emailExists = await _context.Users.AnyAsync(u => u.Email.ToLower() == email.ToLower());
            return Ok(new Response 
            { 
                IsSuccess = !emailExists,
                Message = emailExists ? "Email is already registered" : "Email is available"
            });
        }

        [HttpGet("role-by-email/{email}")]
        public async Task<IActionResult> GetRoleByEmail(string email)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

            if (user == null)
            {
                return NotFound("User");
            }

            return Ok(user.Role);
        }
    }
} 