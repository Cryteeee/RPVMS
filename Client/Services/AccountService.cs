using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using BlazorApp1.Shared.Models;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Blazored.LocalStorage;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components.Forms;
using System.IO;
using System.IdentityModel.Tokens.Jwt;
using BlazorApp1.Shared.Constants;
using BlazorApp1.Shared;
using System.Text.RegularExpressions;

namespace BlazorApp1.Client.Services
{
    public class AccountService : IAccountService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly ILogger<AccountService>? _logger;
        private readonly ILocalStorageService _localStorage;

        public AccountService(IHttpClientFactory httpClientFactory, ILogger<AccountService>? logger, ILocalStorageService localStorage)
        {
            _httpClientFactory = httpClientFactory;
            _localStorage = localStorage;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Converters = { new JsonStringEnumConverter() }
            };
            _logger = logger;
        }

        private async Task<HttpClient> CreateClientAsync()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("ManagementSystem");
                var token = await _localStorage.GetItemAsync<string>("jwt-access-token");
                
                if (string.IsNullOrEmpty(token))
                {
                    _logger?.LogWarning("No JWT token found in local storage");
                    throw new UnauthorizedAccessException("No authentication token found. Please log in again.");
                }

                // Validate token format
                try
                {
                    var handler = new JwtSecurityTokenHandler();
                    var jsonToken = handler.ReadToken(token) as JwtSecurityToken;
                    
                    if (jsonToken.ValidTo < DateTime.UtcNow)
                    {
                        _logger?.LogWarning("JWT token has expired");
                        await _localStorage.RemoveItemAsync("jwt-access-token");
                        throw new UnauthorizedAccessException("Your session has expired. Please log in again.");
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error validating JWT token");
                    await _localStorage.RemoveItemAsync("jwt-access-token");
                    throw new UnauthorizedAccessException("Invalid authentication token. Please log in again.");
                }

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                return client;
            }
            catch (Exception ex) when (!(ex is UnauthorizedAccessException))
            {
                _logger?.LogError(ex, "Error in CreateClientAsync");
                throw new Exception("Failed to create HTTP client", ex);
            }
        }

        private HttpClient CreatePublicClient()
        {
            return _httpClientFactory.CreateClient("ManagementSystem");
        }

        public async Task<BlazorApp1.Shared.Response<string>> LoginAsync(LoginVm loginModel)
        {
            try
            {
                _logger?.LogInformation($"Attempting login for email: {loginModel.Email}");
                
                var httpClient = _httpClientFactory.CreateClient("ManagementSystem");
                var response = await httpClient.PostAsJsonAsync("api/UserAccount/Login", loginModel, _jsonOptions);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger?.LogWarning($"Login failed with status code: {response.StatusCode}, Error: {errorContent}");
                    
                    try
                    {
                        var errorResponse = JsonSerializer.Deserialize<BlazorApp1.Shared.Response<string>>(errorContent, _jsonOptions);
                        if (errorResponse != null)
                        {
                            return errorResponse;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Failed to deserialize error response");
                    }
                    
                    return new BlazorApp1.Shared.Response<string>
                    {
                        IsSuccess = false,
                        Message = response.StatusCode == System.Net.HttpStatusCode.BadRequest 
                            ? "Invalid email or password" 
                            : "Login failed. Please try again later."
                    };
                }

                var responseContent = await response.Content.ReadFromJsonAsync<BlazorApp1.Shared.Response<string>>(_jsonOptions);
                
                if (responseContent == null)
                {
                    _logger?.LogError("Failed to parse server response");
                    return new BlazorApp1.Shared.Response<string>
                    {
                        IsSuccess = false,
                        Message = "Failed to parse server response"
                    };
                }

                if (responseContent.IsSuccess && !string.IsNullOrEmpty(responseContent.Data))
                {
                    await _localStorage.SetItemAsync("jwt-access-token", responseContent.Data);
                }

                _logger?.LogInformation("Login successful");
                return responseContent;
            }
            catch (HttpRequestException ex)
            {
                _logger?.LogError(ex, "Network error during login");
                return new BlazorApp1.Shared.Response<string>
                {
                    IsSuccess = false,
                    Message = "Cannot connect to the server. Please check your connection."
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during login");
                return new BlazorApp1.Shared.Response<string>
                {
                    IsSuccess = false,
                    Message = "An error occurred during login. Please try again later."
                };
            }
        }

        public async Task<BlazorApp1.Shared.Response> AddUpdate(RegistrationView entity)
        {
            try
            {
                var httpClient = await CreateClientAsync();
                var response = await httpClient.PutAsJsonAsync("api/Account/AddUpdate", entity, _jsonOptions);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger?.LogWarning($"Update failed with status code: {response.StatusCode}, Error: {errorContent}");
                    return new BlazorApp1.Shared.Response { IsSuccess = false, Message = $"Update failed: {errorContent}" };
                }

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<BlazorApp1.Shared.Response>(content, _jsonOptions) ?? 
                    new BlazorApp1.Shared.Response { IsSuccess = false, Message = "Failed to deserialize response" };
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Error updating user details");
                return new BlazorApp1.Shared.Response { IsSuccess = false, Message = e.Message };
            }
        }

        public async Task<RegistrationView> GetById(int id)
        {
            try
            {
                var httpClient = await CreateClientAsync();
                _logger?.LogInformation($"Sending request to GetById/{id}");
                
                var response = await httpClient.GetAsync($"api/UserAccount/GetById/{id}");
                
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _logger?.LogWarning("Unauthorized access. Token might be invalid or expired.");
                    await _localStorage.RemoveItemAsync("jwt-access-token");
                    throw new UnauthorizedAccessException("Your session has expired. Please log in again.");
                }

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger?.LogError($"GetById failed with status code: {response.StatusCode}, Error: {errorContent}");
                    throw new Exception($"Failed to get user details: {response.StatusCode}. {errorContent}");
                }

                var result = await response.Content.ReadFromJsonAsync<Response<RegistrationView>>(_jsonOptions);
                if (result == null)
                {
                    _logger?.LogError("Failed to deserialize response");
                    throw new Exception("Failed to parse server response");
                }

                if (!result.IsSuccess)
                {
                    _logger?.LogError($"Server returned error: {result.Message}");
                    throw new Exception(result.Message ?? "Failed to get user details");
                }

                if (result.Data == null)
                {
                    _logger?.LogError("Server returned null data");
                    throw new Exception("Server returned no user details");
                }

                return result.Data;
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (HttpRequestException ex)
            {
                _logger?.LogError(ex, "Network error while retrieving user details");
                throw new Exception("Failed to connect to the server. Please check your connection.");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving user details");
                throw new Exception($"Error retrieving user data: {ex.Message}");
            }
        }

        public async Task<BlazorApp1.Shared.Response> UpdatePassword(int id, string currentPassword, string newPassword, string? securityKey = null)
        {
            try
            {
                var dto = new UpdatePasswordDto
                {
                    UserId = id,
                    CurrentPassword = currentPassword,
                    NewPassword = newPassword,
                    SecurityKey = securityKey
                };

                var httpClient = await CreateClientAsync();
                var response = await httpClient.PutAsJsonAsync("api/Account/UpdatePassword", dto, _jsonOptions);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger?.LogWarning($"Password update failed with status code: {response.StatusCode}, Error: {errorContent}");
                    return new BlazorApp1.Shared.Response { IsSuccess = false, Message = $"Password update failed: {errorContent}" };
                }

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<BlazorApp1.Shared.Response>(content, _jsonOptions) ?? 
                    new BlazorApp1.Shared.Response { IsSuccess = false, Message = "Failed to deserialize response" };
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Error updating password");
                return new BlazorApp1.Shared.Response { IsSuccess = false, Message = e.Message };
            }
        }

        public async Task<BlazorApp1.Shared.Response<UserDetailsDto>> UpdateUserDetailsAsync(UserDetailsDto userDetails)
        {
            try
            {
                var httpClient = await CreateClientAsync();
                var response = await httpClient.PutAsJsonAsync("api/UserAccount/UpdateUserDetails", userDetails, _jsonOptions);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger?.LogWarning($"User details update failed with status code: {response.StatusCode}, Error: {errorContent}");
                    return new BlazorApp1.Shared.Response<UserDetailsDto>
                    {
                        IsSuccess = false,
                        Message = $"Failed to update user details: {errorContent}"
                    };
                }

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<BlazorApp1.Shared.Response<UserDetailsDto>>(content, _jsonOptions) ?? 
                    new BlazorApp1.Shared.Response<UserDetailsDto>
                    {
                        IsSuccess = false,
                        Message = "Failed to deserialize response"
                    };
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Error updating user details");
                return new BlazorApp1.Shared.Response<UserDetailsDto>
                {
                    IsSuccess = false,
                    Message = $"Error: {e.Message}"
                };
            }
        }

        public async Task<BlazorApp1.Shared.Response<UserDetailsDto>> GetUserDetailsAsync(int userId)
        {
            try
            {
                var httpClient = await CreateClientAsync();
                var response = await httpClient.GetAsync($"api/UserAccount/GetUserDetails/{userId}");
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger?.LogWarning($"Get user details failed with status code: {response.StatusCode}, Error: {errorContent}");
                    return new BlazorApp1.Shared.Response<UserDetailsDto>
                    {
                        IsSuccess = false,
                        Message = $"Failed to get user details: {errorContent}"
                    };
                }

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<BlazorApp1.Shared.Response<UserDetailsDto>>(content, _jsonOptions) ?? 
                    new BlazorApp1.Shared.Response<UserDetailsDto>
                    {
                        IsSuccess = false,
                        Message = "Failed to deserialize response",
                        Data = new UserDetailsDto { UserId = userId }
                    };
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Error retrieving user details");
                return new BlazorApp1.Shared.Response<UserDetailsDto>
                {
                    IsSuccess = false,
                    Message = $"Error: {e.Message}"
                };
            }
        }

        public async Task<BlazorApp1.Shared.Response> SendVerificationEmailAsync(int userId)
        {
            try
            {
                var httpClient = await CreateClientAsync();
                var request = new SendVerificationEmailRequest { UserId = userId };
                var response = await httpClient.PostAsJsonAsync("api/Account/SendVerificationEmail", request);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger?.LogWarning($"Failed to send verification email: {errorContent}");
                    
                    try
                    {
                        var errorResponse = JsonSerializer.Deserialize<BlazorApp1.Shared.Response>(errorContent, _jsonOptions);
                        return errorResponse ?? new BlazorApp1.Shared.Response
                        {
                            IsSuccess = false,
                            Message = "Failed to send verification email"
                        };
                    }
                    catch
                    {
                        return new BlazorApp1.Shared.Response
                        {
                            IsSuccess = false,
                            Message = errorContent
                        };
                    }
                }

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<BlazorApp1.Shared.Response>(content, _jsonOptions) ?? 
                    new BlazorApp1.Shared.Response { IsSuccess = false, Message = "Failed to deserialize response" };
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Error sending verification email");
                return new BlazorApp1.Shared.Response { IsSuccess = false, Message = e.Message };
            }
        }

        public async Task<BlazorApp1.Shared.Response> VerifyEmailAsync(string token)
        {
            try
            {
                var httpClient = await CreateClientAsync();
                var response = await httpClient.GetAsync($"api/Account/VerifyEmail?token={token}");
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger?.LogWarning($"Failed to verify email: {errorContent}");
                    return new BlazorApp1.Shared.Response
                    {
                        IsSuccess = false,
                        Message = $"Failed to verify email: {errorContent}"
                    };
                }

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<BlazorApp1.Shared.Response>(content, _jsonOptions) ?? 
                    new BlazorApp1.Shared.Response { IsSuccess = false, Message = "Failed to deserialize response" };
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Error verifying email");
                return new BlazorApp1.Shared.Response { IsSuccess = false, Message = $"Error: {e.Message}" };
            }
        }

        public async Task<BlazorApp1.Shared.Response> RequestPasswordResetAsync(string email)
        {
            try
            {
                var httpClient = CreatePublicClient();
                var response = await httpClient.PostAsJsonAsync("api/Account/RequestPasswordReset", email, _jsonOptions);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger?.LogWarning($"Failed to request password reset: {errorContent}");
                    return new BlazorApp1.Shared.Response { IsSuccess = false, Message = errorContent };
                }

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<BlazorApp1.Shared.Response>(content, _jsonOptions) ?? 
                    new BlazorApp1.Shared.Response { IsSuccess = false, Message = "Failed to deserialize response" };
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Error requesting password reset");
                return new BlazorApp1.Shared.Response { IsSuccess = false, Message = e.Message };
            }
        }

        public async Task<BlazorApp1.Shared.Response> ResetPasswordAsync(string token, string newPassword)
        {
            try
            {
                var httpClient = CreatePublicClient();
                var response = await httpClient.PostAsJsonAsync("api/Account/ResetPassword", new { Token = token, NewPassword = newPassword }, _jsonOptions);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger?.LogWarning($"Failed to reset password: {errorContent}");
                    return new BlazorApp1.Shared.Response { IsSuccess = false, Message = errorContent };
                }

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<BlazorApp1.Shared.Response>(content, _jsonOptions) ?? 
                    new BlazorApp1.Shared.Response { IsSuccess = false, Message = "Failed to deserialize response" };
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Error resetting password");
                return new BlazorApp1.Shared.Response { IsSuccess = false, Message = e.Message };
            }
        }

        public async Task<BlazorApp1.Shared.Response> CheckEmailAvailability(string email)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient("ManagementSystem");
                var response = await httpClient.GetAsync($"api/Account/check-email?email={Uri.EscapeDataString(email)}");
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger?.LogWarning($"Email check failed with status code: {response.StatusCode}, Error: {errorContent}");
                    return new BlazorApp1.Shared.Response { IsSuccess = false, Message = "Failed to check email availability" };
                }

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<BlazorApp1.Shared.Response>(content, _jsonOptions) ?? 
                    new BlazorApp1.Shared.Response { IsSuccess = false, Message = "Failed to deserialize response" };
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Error checking email availability");
                return new BlazorApp1.Shared.Response { IsSuccess = false, Message = e.Message };
            }
        }

        public async Task<BlazorApp1.Shared.Response> CheckContactNumberAvailability(string contactNumber)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient("ManagementSystem");
                var response = await httpClient.GetAsync($"api/Account/check-contact-number?contactNumber={Uri.EscapeDataString(contactNumber)}");
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger?.LogWarning($"Contact number check failed with status code: {response.StatusCode}, Error: {errorContent}");
                    return new BlazorApp1.Shared.Response { IsSuccess = false, Message = "Failed to check contact number availability" };
                }

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<BlazorApp1.Shared.Response>(content, _jsonOptions) ?? 
                    new BlazorApp1.Shared.Response { IsSuccess = false, Message = "Failed to deserialize response" };
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Error checking contact number availability");
                return new BlazorApp1.Shared.Response { IsSuccess = false, Message = e.Message };
            }
        }

        public async Task<BlazorApp1.Shared.Response> Register(UserRegistrationDto userRegistration)
        {
            try
            {
                _logger?.LogInformation("Starting registration process");
                var httpClient = _httpClientFactory.CreateClient("ManagementSystem");
                
                // First validate the model on client side
                if (string.IsNullOrWhiteSpace(userRegistration.Username) ||
                    string.IsNullOrWhiteSpace(userRegistration.Email) ||
                    string.IsNullOrWhiteSpace(userRegistration.Password) ||
                    string.IsNullOrWhiteSpace(userRegistration.ContactNumber))
                {
                    return new BlazorApp1.Shared.Response { IsSuccess = false, Message = "Please fill in all required fields" };
                }

                // Validate password requirements
                if (!Regex.IsMatch(userRegistration.Password, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$"))
                {
                    return new BlazorApp1.Shared.Response 
                    { 
                        IsSuccess = false, 
                        Message = "Password must contain at least one uppercase letter, one lowercase letter, one number, and be at least 8 characters long" 
                    };
                }

                // Validate contact number format
                if (!Regex.IsMatch(userRegistration.ContactNumber, @"^09\d{9}$"))
                {
                    return new BlazorApp1.Shared.Response 
                    { 
                        IsSuccess = false, 
                        Message = "Contact number must start with '09' and contain exactly 11 digits" 
                    };
                }

                _logger?.LogInformation("Sending registration request");
                var response = await httpClient.PostAsJsonAsync("api/UserAccount/register", userRegistration, _jsonOptions);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger?.LogWarning($"Registration failed with status code: {response.StatusCode}, Error: {errorContent}");
                    
                    try
                    {
                        var errorResponse = JsonSerializer.Deserialize<BlazorApp1.Shared.Response>(errorContent, _jsonOptions);
                        return errorResponse ?? new BlazorApp1.Shared.Response { IsSuccess = false, Message = "Registration failed: " + errorContent };
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Error deserializing error response");
                        return new BlazorApp1.Shared.Response { IsSuccess = false, Message = errorContent };
                    }
                }

                var content = await response.Content.ReadAsStringAsync();
                _logger?.LogInformation("Registration successful");
                return JsonSerializer.Deserialize<BlazorApp1.Shared.Response>(content, _jsonOptions) ?? 
                    new BlazorApp1.Shared.Response { IsSuccess = false, Message = "Failed to deserialize response" };
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Error during registration");
                return new BlazorApp1.Shared.Response { IsSuccess = false, Message = "An unexpected error occurred during registration. Please try again." };
            }
        }

        // User Management Methods Implementation
        public async Task<BlazorApp1.Shared.Response<List<UserDto>>> GetAllUsersAsync()
        {
            try
            {
                _logger?.LogInformation("Creating HTTP client for GetAllUsersAsync");
                var httpClient = await CreateClientAsync();
                
                _logger?.LogInformation("Sending request to api/UserAccount/GetClientProfiles");
                var response = await httpClient.GetAsync("api/UserAccount/GetClientProfiles");
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger?.LogWarning("Failed to get users. Status code: {StatusCode}, Error: {Error}", 
                        response.StatusCode, errorContent);
                    return new BlazorApp1.Shared.Response<List<UserDto>>
                    {
                        IsSuccess = false,
                        Message = $"Failed to retrieve users. Status code: {response.StatusCode}"
                    };
                }

                var result = await response.Content.ReadFromJsonAsync<BlazorApp1.Shared.Response<List<UserDto>>>();
                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in GetAllUsersAsync");
                return new BlazorApp1.Shared.Response<List<UserDto>>
                {
                    IsSuccess = false,
                    Message = $"An error occurred: {ex.Message}"
                };
            }
        }

        public async Task<BlazorApp1.Shared.Response<UserDto>> GetUserByIdAsync(int id)
        {
            try
            {
                var httpClient = await CreateClientAsync();
                var response = await httpClient.GetAsync($"api/Account/GetUserById/{id}");
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger?.LogWarning($"Failed to get user: {errorContent}");
                    return new BlazorApp1.Shared.Response<UserDto>
                    {
                        IsSuccess = false,
                        Message = "Failed to retrieve user"
                    };
                }

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<BlazorApp1.Shared.Response<UserDto>>(content, _jsonOptions) ?? 
                    new BlazorApp1.Shared.Response<UserDto> { IsSuccess = false, Message = "Failed to deserialize response" };
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Error retrieving user");
                return new BlazorApp1.Shared.Response<UserDto> { IsSuccess = false, Message = e.Message };
            }
        }

        public async Task<BlazorApp1.Shared.Response<bool>> UpdateUserRoleAsync(int userId, string newRole)
        {
            try
            {
                _logger?.LogInformation($"Attempting to update role for user {userId} to {newRole}");
                var httpClient = await CreateClientAsync();
                var response = await httpClient.PutAsJsonAsync($"api/Account/role/{userId}", new { Role = newRole });
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger?.LogWarning($"Failed to update role. Status: {response.StatusCode}, Error: {errorContent}");
                    return new BlazorApp1.Shared.Response<bool> { IsSuccess = false, Message = $"Failed to update role: {errorContent}" };
                }

                var content = await response.Content.ReadAsStringAsync();
                _logger?.LogInformation($"Role update response: {content}");
                var result = JsonSerializer.Deserialize<BlazorApp1.Shared.Response<bool>>(content, _jsonOptions);
                return result ?? new BlazorApp1.Shared.Response<bool> { IsSuccess = false, Message = "Failed to deserialize response" };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating user role");
                return new BlazorApp1.Shared.Response<bool> { IsSuccess = false, Message = ex.Message };
            }
        }

        public async Task<BlazorApp1.Shared.Response> DeleteUserAsync(int userId)
        {
            try
            {
                var httpClient = await CreateClientAsync();
                var response = await httpClient.DeleteAsync($"api/Account/Delete/{userId}");
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger?.LogWarning($"Failed to delete user: {errorContent}");
                    return new BlazorApp1.Shared.Response { IsSuccess = false, Message = "Failed to delete user" };
                }

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<BlazorApp1.Shared.Response>(content, _jsonOptions) ?? 
                    new BlazorApp1.Shared.Response { IsSuccess = false, Message = "Failed to deserialize response" };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error deleting user");
                return new BlazorApp1.Shared.Response { IsSuccess = false, Message = ex.Message };
            }
        }

        public async Task<BlazorApp1.Shared.Response> DeactivateUserAsync(int userId)
        {
            try
            {
                var httpClient = await CreateClientAsync();
                var response = await httpClient.PutAsJsonAsync($"api/Account/deactivate/{userId}", new { });
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger?.LogWarning($"Failed to deactivate user: {errorContent}");
                    return new BlazorApp1.Shared.Response { IsSuccess = false, Message = "Failed to deactivate user" };
                }

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<BlazorApp1.Shared.Response>(content, _jsonOptions) ?? 
                    new BlazorApp1.Shared.Response { IsSuccess = false, Message = "Failed to deserialize response" };
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Error deactivating user");
                return new BlazorApp1.Shared.Response { IsSuccess = false, Message = e.Message };
            }
        }

        public async Task<Response> ActivateUserAsync(int userId)
        {
            try
            {
                var httpClient = await CreateClientAsync();
                var response = await httpClient.PutAsJsonAsync($"api/Account/activate/{userId}", new { });
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger?.LogWarning($"Failed to activate user: {errorContent}");
                    return new Response { IsSuccess = false, Message = "Failed to activate user" };
                }

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<Response>(content, _jsonOptions) ?? 
                    new Response { IsSuccess = false, Message = "Failed to deserialize response" };
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Error activating user");
                return new Response { IsSuccess = false, Message = e.Message };
            }
        }

        public async Task<Response> UpdateAccountAsync(UpdateAccountDto model)
        {
            try
            {
                var httpClient = await CreateClientAsync();
                var response = await httpClient.PutAsJsonAsync("api/Account/UpdateAccount", model);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger?.LogWarning($"Account update failed with status code: {response.StatusCode}, Error: {errorContent}");
                    try
                    {
                        var errorResponse = JsonSerializer.Deserialize<Response>(errorContent, _jsonOptions);
                        return errorResponse ?? new Response { IsSuccess = false, Message = "Failed to update account" };
                    }
                    catch
                    {
                        return new Response { IsSuccess = false, Message = errorContent };
                    }
                }

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<Response>(content, _jsonOptions) ?? 
                    new Response { IsSuccess = false, Message = "Failed to deserialize response" };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating account");
                return new Response { IsSuccess = false, Message = $"Error updating account: {ex.Message}" };
            }
        }

        public async Task<Response<string>> UploadProfilePhotoAsync(int userId, IBrowserFile file)
        {
            try
            {
                _logger?.LogInformation($"Starting profile photo upload for user {userId}");
                
                var maxFileSize = 1024 * 1024 * 5; // 5MB max file size
                if (file.Size > maxFileSize)
                {
                    return new Response<string>
                    {
                        IsSuccess = false,
                        Message = "File size must be less than 5MB"
                    };
                }

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var fileExtension = Path.GetExtension(file.Name).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    return new Response<string>
                    {
                        IsSuccess = false,
                        Message = "Only .jpg, .jpeg, .png, and .gif files are allowed"
                    };
                }

                using var ms = new MemoryStream();
                var stream = file.OpenReadStream(maxFileSize);
                await stream.CopyToAsync(ms);
                ms.Position = 0;

                var content = new MultipartFormDataContent();
                var fileContent = new ByteArrayContent(ms.ToArray());
                var contentType = fileExtension switch
                {
                    ".jpg" or ".jpeg" => "image/jpeg",
                    ".png" => "image/png",
                    ".gif" => "image/gif",
                    _ => "application/octet-stream"
                };
                
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
                content.Add(fileContent, "file", file.Name);

                var httpClient = await CreateClientAsync();
                
                // Log the request details
                _logger?.LogInformation($"Sending photo upload request to api/UserAccount/{userId}/photo");
                _logger?.LogInformation($"File details - Name: {file.Name}, Size: {file.Size}, Type: {contentType}");

                try
                {
                    var response = await httpClient.PostAsync($"api/UserAccount/{userId}/photo", content);
                    
                    // Log the response status
                    _logger?.LogInformation($"Upload response status: {response.StatusCode}");
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var result = await response.Content.ReadFromJsonAsync<Response<string>>();
                        if (result != null)
                        {
                            _logger?.LogInformation($"Upload successful. Photo URL: {result.Data}");
                            return result;
                        }
                        else
                        {
                            _logger?.LogError("Failed to deserialize successful response");
                            return new Response<string>
                            {
                                IsSuccess = false,
                                Message = "Failed to process server response"
                            };
                        }
                    }
                    
                    // If we get here, it's an error response
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger?.LogError($"Upload failed. Status: {response.StatusCode}, Error: {errorContent}");
                    
                    return new Response<string>
                    {
                        IsSuccess = false,
                        Message = $"Server error: {errorContent}"
                    };
                }
                catch (HttpRequestException ex)
                {
                    _logger?.LogError(ex, "HTTP request failed during photo upload");
                    return new Response<string>
                    {
                        IsSuccess = false,
                        Message = $"Connection error: {ex.Message}"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in UploadProfilePhotoAsync");
                return new Response<string>
                {
                    IsSuccess = false,
                    Message = $"An unexpected error occurred: {ex.Message}"
                };
            }
        }
    }
} 