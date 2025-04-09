using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using BlazorApp1.Shared.Models;
using BlazorApp1.Shared.Constants;
using Microsoft.AspNetCore.Components.Forms;
using Azure;
using BlazorApp1.Shared;

namespace BlazorApp1.Client.Services
{
    public interface IAccountService
    {
        Task<BlazorApp1.Shared.Response> AddUpdate(RegistrationView entity);
        Task<RegistrationView> GetById(int id);
        Task<BlazorApp1.Shared.Response> UpdatePassword(int id, string currentPassword, string newPassword, string? securityKey = null);
        Task<BlazorApp1.Shared.Response<string>> LoginAsync(LoginVm loginModel);
        Task<BlazorApp1.Shared.Response<UserDetailsDto>> UpdateUserDetailsAsync(UserDetailsDto userDetails);
        Task<BlazorApp1.Shared.Response<UserDetailsDto>> GetUserDetailsAsync(int userId);
        Task<BlazorApp1.Shared.Response> SendVerificationEmailAsync(int userId);
        Task<BlazorApp1.Shared.Response> VerifyEmailAsync(string token);
        Task<BlazorApp1.Shared.Response> RequestPasswordResetAsync(string email);
        Task<BlazorApp1.Shared.Response> ResetPasswordAsync(string token, string newPassword);
        Task<BlazorApp1.Shared.Response> CheckEmailAvailability(string email);
        Task<BlazorApp1.Shared.Response> CheckContactNumberAvailability(string contactNumber);
        Task<BlazorApp1.Shared.Response> Register(UserRegistrationDto userRegistration);
        
        // New User Management Methods
        Task<BlazorApp1.Shared.Response<List<UserDto>>> GetAllUsersAsync();
        Task<BlazorApp1.Shared.Response<List<UserDto>>> GetClientProfilesAsync();
        Task<BlazorApp1.Shared.Response<UserDto>> GetUserByIdAsync(int id);
        Task<BlazorApp1.Shared.Response> DeleteUserAsync(int id);
        Task<BlazorApp1.Shared.Response<bool>> UpdateUserRoleAsync(int userId, string newRole);
        Task<BlazorApp1.Shared.Response> DeactivateUserAsync(int userId);
        Task<BlazorApp1.Shared.Response> ActivateUserAsync(int userId);
        Task<BlazorApp1.Shared.Response> UpdateAccountAsync(UpdateAccountDto model);

        Task<BlazorApp1.Shared.Response<string>> UploadProfilePhotoAsync(int userId, IBrowserFile file);
    }
}
