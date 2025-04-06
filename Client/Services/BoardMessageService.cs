using System.Net.Http.Json;
using BlazorApp1.Shared.Models;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using BlazorApp1.Client.Auth;
using BlazorApp1.Shared;

namespace BlazorApp1.Client.Services
{
    public class BoardMessageService : IBoardMessageService
    {
        private readonly HttpClient _httpClient;
        private readonly ILocalStorageService _localStorage;
        private readonly AuthenticationStateProvider _authStateProvider;
        private readonly string _baseUrl = "api/BoardMessage";

        public BoardMessageService(
            HttpClient httpClient,
            ILocalStorageService localStorage,
            AuthenticationStateProvider authStateProvider)
        {
            _httpClient = httpClient;
            _localStorage = localStorage;
            _authStateProvider = authStateProvider;
        }

        public async Task<Response<List<BoardMessageDto>>> GetMessagesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}");
                
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await HandleUnauthorized();
                    return new Response<List<BoardMessageDto>>
                    {
                        IsSuccess = false,
                        Message = "Your session has expired. Please log in again.",
                        Data = null
                    };
                }

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<Response<List<BoardMessageDto>>>();
                    return result ?? new Response<List<BoardMessageDto>>
                    {
                        IsSuccess = false,
                        Message = "Failed to retrieve messages",
                        Data = null
                    };
                }

                return new Response<List<BoardMessageDto>>
                {
                    IsSuccess = false,
                    Message = $"Error: {response.StatusCode}",
                    Data = null
                };
            }
            catch (Exception ex)
            {
                return new Response<List<BoardMessageDto>>
                {
                    IsSuccess = false,
                    Message = $"Error: {ex.Message}",
                    Data = null
                };
            }
        }

        public async Task<Response<BoardMessageDto>> CreateMessageAsync(BoardMessageDto message)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}", message);
                
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await HandleUnauthorized();
                    return new Response<BoardMessageDto>
                    {
                        IsSuccess = false,
                        Message = "Your session has expired. Please log in again.",
                        Data = null
                    };
                }

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<Response<BoardMessageDto>>();
                    return result ?? new Response<BoardMessageDto>
                    {
                        IsSuccess = false,
                        Message = "Failed to create message",
                        Data = null
                    };
                }

                return new Response<BoardMessageDto>
                {
                    IsSuccess = false,
                    Message = $"Error: {response.StatusCode}",
                    Data = null
                };
            }
            catch (Exception ex)
            {
                return new Response<BoardMessageDto>
                {
                    IsSuccess = false,
                    Message = $"Error: {ex.Message}",
                    Data = null
                };
            }
        }

        public async Task<Response> MarkMessageAsReadAsync(int messageId)
        {
            try
            {
                var response = await _httpClient.PutAsync($"{_baseUrl}/{messageId}/read", null);
                
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await HandleUnauthorized();
                    return new Response
                    {
                        IsSuccess = false,
                        Message = "Your session has expired. Please log in again."
                    };
                }

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<Response>();
                    return result ?? new Response
                    {
                        IsSuccess = false,
                        Message = "Failed to mark message as read"
                    };
                }

                return new Response
                {
                    IsSuccess = false,
                    Message = $"Error: {response.StatusCode}"
                };
            }
            catch (Exception ex)
            {
                return new Response
                {
                    IsSuccess = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }

        public async Task<Response> ClearAllMessagesAsync()
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{_baseUrl}/clear");
                
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await HandleUnauthorized();
                    return new Response
                    {
                        IsSuccess = false,
                        Message = "Your session has expired. Please log in again."
                    };
                }

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<Response>();
                    return result ?? new Response
                    {
                        IsSuccess = false,
                        Message = "Failed to clear messages"
                    };
                }

                return new Response
                {
                    IsSuccess = false,
                    Message = $"Error: {response.StatusCode}"
                };
            }
            catch (Exception ex)
            {
                return new Response
                {
                    IsSuccess = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }

        private async Task HandleUnauthorized()
        {
            await _localStorage.RemoveItemAsync("jwt-access-token");
            if (_authStateProvider is CustomAuthProvider authProvider)
            {
                await authProvider.NotifyUserAuthenticationAsync(null);
            }
        }
    }
} 