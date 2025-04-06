using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BlazorApp1.Client.Models;
using BlazorApp1.Server.Data;
using BlazorApp1.Shared.DTOs;
using System.Linq;

namespace BlazorApp1.Server.Services
{
    public interface IStaffService
    {
        Task<List<StaffDto>> GetAllStaffAsync();
        Task<StaffDto> GetStaffByIdAsync(int id);
        Task<StaffDto> CreateStaffAsync(CreateStaffDto dto);
        Task<StaffDto> UpdateStaffAsync(int id, UpdateStaffDto dto);
        Task DeleteStaffAsync(int id);
        Task<string> UpdateStaffImageAsync(int id, string imageUrl);
    }

    public class StaffService : IStaffService
    {
        private readonly ApplicationDbContext _context;

        public StaffService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<StaffDto>> GetAllStaffAsync()
        {
            return await _context.Staff
                .Select(s => new StaffDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Position = s.Position,
                    Bio = s.Bio,
                    ImageUrl = s.ImageUrl,
                    FacebookUrl = s.FacebookUrl,
                    InstagramUrl = s.InstagramUrl,
                    CreatedAt = s.CreatedAt,
                    UpdatedAt = s.UpdatedAt
                })
                .ToListAsync();
        }

        public async Task<StaffDto> GetStaffByIdAsync(int id)
        {
            var staff = await _context.Staff.FindAsync(id);
            if (staff == null)
                throw new KeyNotFoundException($"Staff with ID {id} not found.");

            return new StaffDto
            {
                Id = staff.Id,
                Name = staff.Name,
                Position = staff.Position,
                Bio = staff.Bio,
                ImageUrl = staff.ImageUrl,
                FacebookUrl = staff.FacebookUrl,
                InstagramUrl = staff.InstagramUrl,
                CreatedAt = staff.CreatedAt,
                UpdatedAt = staff.UpdatedAt
            };
        }

        public async Task<StaffDto> CreateStaffAsync(CreateStaffDto dto)
        {
            var staff = new Staff
            {
                Name = dto.Name,
                Position = dto.Position,
                Bio = dto.Bio,
                FacebookUrl = dto.FacebookUrl,
                InstagramUrl = dto.InstagramUrl,
                CreatedAt = DateTime.UtcNow
            };

            _context.Staff.Add(staff);
            await _context.SaveChangesAsync();

            return new StaffDto
            {
                Id = staff.Id,
                Name = staff.Name,
                Position = staff.Position,
                Bio = staff.Bio,
                ImageUrl = staff.ImageUrl,
                FacebookUrl = staff.FacebookUrl,
                InstagramUrl = staff.InstagramUrl,
                CreatedAt = staff.CreatedAt,
                UpdatedAt = staff.UpdatedAt
            };
        }

        public async Task<StaffDto> UpdateStaffAsync(int id, UpdateStaffDto dto)
        {
            var staff = await _context.Staff.FindAsync(id);
            if (staff == null)
                throw new KeyNotFoundException($"Staff with ID {id} not found.");

            staff.Name = dto.Name;
            staff.Position = dto.Position;
            staff.Bio = dto.Bio;
            staff.FacebookUrl = dto.FacebookUrl;
            staff.InstagramUrl = dto.InstagramUrl;
            staff.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new StaffDto
            {
                Id = staff.Id,
                Name = staff.Name,
                Position = staff.Position,
                Bio = staff.Bio,
                ImageUrl = staff.ImageUrl,
                FacebookUrl = staff.FacebookUrl,
                InstagramUrl = staff.InstagramUrl,
                CreatedAt = staff.CreatedAt,
                UpdatedAt = staff.UpdatedAt
            };
        }

        public async Task DeleteStaffAsync(int id)
        {
            var staff = await _context.Staff.FindAsync(id);
            if (staff == null)
                throw new KeyNotFoundException($"Staff with ID {id} not found.");

            _context.Staff.Remove(staff);
            await _context.SaveChangesAsync();
        }

        public async Task<string> UpdateStaffImageAsync(int id, string imageUrl)
        {
            var staff = await _context.Staff.FindAsync(id);
            if (staff == null)
                throw new KeyNotFoundException($"Staff with ID {id} not found.");

            staff.ImageUrl = imageUrl;
            staff.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return imageUrl;
        }
    }
} 