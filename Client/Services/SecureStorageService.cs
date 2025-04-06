using Blazored.LocalStorage;
using System.Text.Json;

namespace BlazorApp1.Client.Services
{
    public interface ISecureStorageService
    {
        Task SetItemAsync<T>(string key, T value, TimeSpan? expiration = null);
        Task<T> GetItemAsync<T>(string key);
        Task RemoveItemAsync(string key);
        Task ClearUserDataAsync();
        Task ClearExpiredItemsAsync();
    }

    public class SecureStorageService : ISecureStorageService
    {
        private readonly ILocalStorageService _localStorage;
        private const string EXPIRATION_SUFFIX = "_expiration";
        private readonly Dictionary<string, TimeSpan> _defaultExpirations = new()
        {
            { "boardMessage_draft", TimeSpan.FromHours(24) }
        };

        public SecureStorageService(ILocalStorageService localStorage)
        {
            _localStorage = localStorage;
        }

        public async Task SetItemAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            // Set the actual value
            await _localStorage.SetItemAsync(key, value);

            // Set expiration if provided or if there's a default expiration for this key type
            TimeSpan? expirationToUse = expiration;
            if (!expirationToUse.HasValue)
            {
                var keyType = key.Split('_')[0];
                if (_defaultExpirations.ContainsKey(keyType))
                {
                    expirationToUse = _defaultExpirations[keyType];
                }
            }

            if (expirationToUse.HasValue)
            {
                var expirationTime = DateTime.UtcNow.Add(expirationToUse.Value);
                await _localStorage.SetItemAsync($"{key}{EXPIRATION_SUFFIX}", expirationTime);
            }
        }

        public async Task<T> GetItemAsync<T>(string key)
        {
            // Check expiration first
            var expirationKey = $"{key}{EXPIRATION_SUFFIX}";
            if (await _localStorage.ContainKeyAsync(expirationKey))
            {
                var expirationTime = await _localStorage.GetItemAsync<DateTime>(expirationKey);
                if (DateTime.UtcNow > expirationTime)
                {
                    await RemoveItemAsync(key);
                    return default;
                }
            }

            // If not expired or no expiration set, return the value
            return await _localStorage.GetItemAsync<T>(key);
        }

        public async Task RemoveItemAsync(string key)
        {
            await _localStorage.RemoveItemAsync(key);
            await _localStorage.RemoveItemAsync($"{key}{EXPIRATION_SUFFIX}");
        }

        public async Task ClearUserDataAsync()
        {
            // Get all keys
            var keys = await GetAllKeysAsync();

            // Remove user-specific data
            foreach (var key in keys)
            {
                if (key.StartsWith("boardMessage_") || 
                    key.StartsWith("draft_") || 
                    key.StartsWith("user_") ||
                    key == "theme")
                {
                    await RemoveItemAsync(key);
                }
            }
        }

        public async Task ClearExpiredItemsAsync()
        {
            var keys = await GetAllKeysAsync();
            foreach (var key in keys)
            {
                if (key.EndsWith(EXPIRATION_SUFFIX))
                {
                    var baseKey = key.Substring(0, key.Length - EXPIRATION_SUFFIX.Length);
                    var expirationTime = await _localStorage.GetItemAsync<DateTime>(key);
                    
                    if (DateTime.UtcNow > expirationTime)
                    {
                        await RemoveItemAsync(baseKey);
                    }
                }
            }
        }

        private async Task<IEnumerable<string>> GetAllKeysAsync()
        {
            var keys = new List<string>();
            for (int i = 0; i < await _localStorage.LengthAsync(); i++)
            {
                var key = await _localStorage.KeyAsync(i);
                if (!string.IsNullOrEmpty(key))
                {
                    keys.Add(key);
                }
            }
            return keys;
        }
    }
} 