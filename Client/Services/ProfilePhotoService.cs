using System;

namespace BlazorApp1.Client.Services
{
    public class ProfilePhotoService
    {
        public event Action<string> OnProfilePhotoChange;
        private string _currentPhotoUrl;
        private DateTime _lastPhotoUpdate = DateTime.Now;

        public string CurrentPhotoUrl => _currentPhotoUrl;
        public DateTime LastPhotoUpdate => _lastPhotoUpdate;

        private void NotifyPhotoChanged(string newUrl)
        {
            OnProfilePhotoChange?.Invoke(newUrl);
        }

        public void UpdateProfilePhoto(string newUrl)
        {
            if (_currentPhotoUrl != newUrl)
            {
                _currentPhotoUrl = newUrl;
                _lastPhotoUpdate = DateTime.Now;
                NotifyPhotoChanged(newUrl);
            }
        }
    }
} 