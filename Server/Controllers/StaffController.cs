using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BlazorApp1.Server.Services;
using BlazorApp1.Shared.DTOs;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace BlazorApp1.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StaffController : ControllerBase
    {
        private readonly IStaffService _staffService;
        private readonly IWebHostEnvironment _environment;

        public StaffController(IStaffService staffService, IWebHostEnvironment environment)
        {
            _staffService = staffService;
            _environment = environment;
        }

        [HttpGet]
        public async Task<ActionResult<List<StaffDto>>> GetAll()
        {
            var staff = await _staffService.GetAllStaffAsync();
            return Ok(staff);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<StaffDto>> GetById(int id)
        {
            try
            {
                var staff = await _staffService.GetStaffByIdAsync(id);
                return Ok(staff);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpPost]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<ActionResult<StaffDto>> Create([FromBody] CreateStaffDto dto)
        {
            try
            {
                var staff = await _staffService.CreateStaffAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = staff.Id }, staff);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<ActionResult<StaffDto>> Update(int id, [FromBody] UpdateStaffDto dto)
        {
            try
            {
                var staff = await _staffService.UpdateStaffAsync(id, dto);
                return Ok(staff);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<ActionResult> Delete(int id)
        {
            try
            {
                await _staffService.DeleteStaffAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpPost("{id}/image")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<ActionResult<string>> UploadImage(int id, IFormFile image)
        {
            if (image == null || image.Length == 0)
                return BadRequest("No image file provided.");

            try
            {
                // Create uploads directory if it doesn't exist
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "staff");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                // Generate unique filename
                var uniqueFileName = $"{id}_{Guid.NewGuid()}{Path.GetExtension(image.FileName)}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Save the file
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(fileStream);
                }

                // Update the staff member's image URL
                var imageUrl = $"/uploads/staff/{uniqueFileName}";
                await _staffService.UpdateStaffImageAsync(id, imageUrl);

                return Ok(imageUrl);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
} 