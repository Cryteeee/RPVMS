using System.Collections.ObjectModel;
using BlazorApp1.Shared.Models;
using BlazorApp1.Shared.Enums;
using Microsoft.AspNetCore.Components;
using Blazored.LocalStorage;
using System.Text.Json;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace BlazorApp1.Client.Services
{
    public interface INotificationService
    {
        event Action OnNotificationsChanged;
        int UnreadCount { get; }
        ObservableCollection<BoardMessageDto> UnreadMessages { get; }
        Task InitializeAsync();
        Task AddNotification(BoardMessageDto message);
        Task MarkAsRead(int messageId);
        Task ClearNotifications();
        Task NavigateToMessage(int messageId);
        Task ClearUserNotificationsAsync();
        Task NavigateToMessageWithoutMarkingRead(int messageId);
        Task StartAsync();
        Task StopAsync();
    }

    public class NotificationService : INotificationService
    {
        private readonly NavigationManager _navigationManager;
        private readonly ILocalStorageService _localStorage;
        private readonly HttpClient _httpClient;
        private readonly HubConnection _hubConnection;
        private readonly AuthenticationStateProvider _authenticationStateProvider;
        private ObservableCollection<BoardMessageDto> _unreadMessages = new();
        private const string STORAGE_KEY = "unread_messages";

        public event Action OnNotificationsChanged;

        public NotificationService(
            NavigationManager navigationManager,
            ILocalStorageService localStorage,
            HttpClient httpClient,
            AuthenticationStateProvider authenticationStateProvider)
        {
            _navigationManager = navigationManager;
            _localStorage = localStorage;
            _httpClient = httpClient;
            _authenticationStateProvider = authenticationStateProvider;

            Console.WriteLine("[NotificationService] Initializing...");

            // Initialize SignalR connection
            _hubConnection = new HubConnectionBuilder()
                .WithUrl(_navigationManager.ToAbsoluteUri("/notificationHub"), options =>
                {
                    options.AccessTokenProvider = async () =>
                    {
                        var token = await _localStorage.GetItemAsync<string>("jwt-access-token");
                        Console.WriteLine($"[SignalR] Token for connection: {(string.IsNullOrEmpty(token) ? "Not found" : $"Found (length: {token.Length})")}");
                        return token;
                    };
                    options.Headers = new Dictionary<string, string>
                    {
                        { "X-Requested-With", "XMLHttpRequest" }
                    };
                })
                .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10) })
                .Build();

            // Set up notification handler with detailed logging
            _hubConnection.On<BoardMessageDto>("ReceiveNotification", async (message) =>
            {
                Console.WriteLine($"[SignalR] Received notification: Type={message.Type}, Content={message.Content}, MessageId={message.MessageId}");
                await AddNotification(message);
            });

            // Add connection state change logging
            _hubConnection.Closed += async (error) =>
            {
                Console.WriteLine($"[SignalR] Connection closed. Error: {error?.Message}");
                await Task.Delay(5000);
                await StartAsync();
            };

            _hubConnection.Reconnecting += error =>
            {
                Console.WriteLine($"[SignalR] Attempting to reconnect... Error: {error?.Message}");
                return Task.CompletedTask;
            };

            _hubConnection.Reconnected += connectionId =>
            {
                Console.WriteLine($"[SignalR] Reconnected with ID: {connectionId}");
                return Task.CompletedTask;
            };

            Console.WriteLine("[NotificationService] Initialization complete");
        }

        public int UnreadCount => _unreadMessages.Count;
        public ObservableCollection<BoardMessageDto> UnreadMessages => _unreadMessages;

        public async Task StartAsync()
        {
            try
            {
                if (_hubConnection.State == HubConnectionState.Disconnected)
                {
                    Console.WriteLine("[SignalR] Starting connection...");
                    
                    // Get user roles before connecting
                    var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
                    var user = authState.User;
                    var roles = user.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value);
                    Console.WriteLine($"[SignalR] User roles before connection: {string.Join(", ", roles)}");

                    await _hubConnection.StartAsync();
                    Console.WriteLine($"[SignalR] Connected successfully! State: {_hubConnection.State}");
                    Console.WriteLine($"[SignalR] Connection ID: {_hubConnection.ConnectionId}");

                    // Log user claims after connection
                    Console.WriteLine($"[SignalR] User claims after connection:");
                    foreach (var claim in user.Claims)
                    {
                        Console.WriteLine($"[SignalR] Claim: {claim.Type} = {claim.Value}");
                    }

                    // Join the Admins group if user is Admin or SuperAdmin
                    if (user.IsInRole("Admin") || user.IsInRole("SuperAdmin"))
                    {
                        await _hubConnection.InvokeAsync("JoinAdminGroup");
                        Console.WriteLine("[SignalR] Joined Admins group");
                    }
                }
                else
                {
                    Console.WriteLine($"[SignalR] Connection already in state: {_hubConnection.State}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SignalR] Connection error: {ex.Message}");
                Console.WriteLine($"[SignalR] Stack trace: {ex.StackTrace}");
                // Try to reconnect after a delay
                await Task.Delay(5000);
                await StartAsync();
            }
        }

        public async Task StopAsync()
        {
            if (_hubConnection != null)
            {
                await _hubConnection.StopAsync();
                await _hubConnection.DisposeAsync();
            }
        }

        public async Task InitializeAsync()
        {
            try
            {
                Console.WriteLine("[NotificationService] Initializing...");
                
                // Load stored messages
                var storedMessages = await _localStorage.GetItemAsync<List<BoardMessageDto>>(STORAGE_KEY);
                if (storedMessages != null)
                {
                    _unreadMessages = new ObservableCollection<BoardMessageDto>(storedMessages);
                    OnNotificationsChanged?.Invoke();
                    Console.WriteLine($"[NotificationService] Loaded {storedMessages.Count} stored messages");
                }

                // Start SignalR connection
                if (_hubConnection.State != HubConnectionState.Connected)
                {
                    Console.WriteLine("[NotificationService] Starting SignalR connection...");
                    await StartAsync();
                }

                Console.WriteLine("[NotificationService] Initialization complete");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NotificationService] Error initializing: {ex.Message}");
                // If there's an error reading from storage, start with empty collection
                _unreadMessages = new ObservableCollection<BoardMessageDto>();
            }
        }

        public async Task ClearUserNotificationsAsync()
        {
            _unreadMessages.Clear();
            await _localStorage.RemoveItemAsync(STORAGE_KEY);
            OnNotificationsChanged?.Invoke();
        }

        private async Task SaveToStorageAsync()
        {
            try
            {
                await _localStorage.SetItemAsync(STORAGE_KEY, _unreadMessages.ToList());
            }
            catch
            {
                // Handle storage errors silently
            }
        }

        public async Task AddNotification(BoardMessageDto message)
        {
            try
            {
                Console.WriteLine($"[Notification] Processing notification: Type={message.Type}, Content={message.Content}, MessageId={message.MessageId}");

                // Don't add if already exists
                if (_unreadMessages.Any(m => m.MessageId == message.MessageId))
                {
                    Console.WriteLine($"[Notification] Already exists: {message.MessageId}");
                    return;
                }

                // For user registration notifications, only add if user is SuperAdmin or Admin
                if (message.Type == "UserRegistration")
                {
                    var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
                    var user = authState.User;
                    var isAdmin = user.IsInRole("Admin");
                    var isSuperAdmin = user.IsInRole("SuperAdmin");
                    
                    Console.WriteLine($"[Notification] UserRegistration - User roles: IsAdmin={isAdmin}, IsSuperAdmin={isSuperAdmin}");
                    Console.WriteLine($"[Notification] User claims: {string.Join(", ", user.Claims.Select(c => $"{c.Type}={c.Value}"))}");
                    
                    if (!isAdmin && !isSuperAdmin)
                    {
                        Console.WriteLine("[Notification] User not authorized for UserRegistration notifications");
                        return;
                    }
                    
                    Console.WriteLine("[Notification] User authorized for UserRegistration notifications");
                }

                // Add the notification
                _unreadMessages.Add(message);
                Console.WriteLine($"[Notification] Added to unread messages. New count: {_unreadMessages.Count}");
                
                OnNotificationsChanged?.Invoke();
                Console.WriteLine("[Notification] Invoked OnNotificationsChanged");
                
                await SaveToStorageAsync();
                Console.WriteLine("[Notification] Saved to storage");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Notification] Error: {ex.Message}");
                Console.WriteLine($"[Notification] Stack trace: {ex.StackTrace}");
            }
        }

        public async Task MarkAsRead(int messageId)
        {
            try
            {
                // First update the database
                var response = await _httpClient.PutAsJsonAsync($"api/BoardMessage/{messageId}/read", new {});
                if (response.IsSuccessStatusCode)
                {
                    // Then update local state
                    var message = _unreadMessages.FirstOrDefault(m => m.MessageId == messageId);
                    if (message != null)
                    {
                        _unreadMessages.Remove(message);
                        OnNotificationsChanged?.Invoke();
                        await SaveToStorageAsync();
                    }
                }
            }
            catch
            {
                // If API call fails, still update local state to prevent UI inconsistency
                var message = _unreadMessages.FirstOrDefault(m => m.MessageId == messageId);
                if (message != null)
                {
                    _unreadMessages.Remove(message);
                    OnNotificationsChanged?.Invoke();
                    await SaveToStorageAsync();
                }
            }
        }

        public async Task ClearNotifications()
        {
            try
            {
                // Mark all notifications as read on the server
                foreach (var message in _unreadMessages.ToList())
                {
                    await MarkAsRead(message.MessageId);
                }
                
                _unreadMessages.Clear();
                await _localStorage.RemoveItemAsync(STORAGE_KEY);
                OnNotificationsChanged?.Invoke();
            }
            catch
            {
                // If there's an error, still clear local state to prevent UI inconsistency
                _unreadMessages.Clear();
                await _localStorage.RemoveItemAsync(STORAGE_KEY);
                OnNotificationsChanged?.Invoke();
            }
        }

        public async Task NavigateToMessage(int messageId)
        {
            var message = _unreadMessages.FirstOrDefault(m => m.MessageId == messageId);
            if (message != null)
            {
                // Mark the message as read before navigation
                await MarkAsRead(messageId);
                
                switch (message.Type)
                {
                    case "UserRegistration":
                        _navigationManager.NavigateTo("/admin/users");
                        break;
                    case "RequestSubmission":
                    case "ConcernSubmission":
                    case "SuggestionSubmission":
                        _navigationManager.NavigateTo("/admin/concerns");
                        break;
                    case "StatusUpdate":
                        _navigationManager.NavigateTo("/client/my-submissions");
                        break;
                    default:
                        _navigationManager.NavigateTo($"/board-messages?highlight={messageId}");
                        break;
                }
            }
        }

        public async Task NavigateToMessageWithoutMarkingRead(int messageId)
        {
            var message = _unreadMessages.FirstOrDefault(m => m.MessageId == messageId);
            if (message != null)
            {
                switch (message.Type)
                {
                    case "UserRegistration":
                        _navigationManager.NavigateTo("/admin/users");
                        break;
                    case "RequestSubmission":
                    case "ConcernSubmission":
                    case "SuggestionSubmission":
                        _navigationManager.NavigateTo("/admin/concerns");
                        break;
                    case "StatusUpdate":
                        _navigationManager.NavigateTo("/client/my-submissions");
                        break;
                    default:
                        _navigationManager.NavigateTo($"/board-messages?highlight={messageId}");
                        break;
                }
            }
        }
    }
} 