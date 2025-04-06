using System;

namespace BlazorApp1.Client.Services
{
    public class ProfileStateService
    {
        public event Action<string> OnProfilePhotoChange;

        public void UpdateProfilePhoto(string newPhotoUrl)
        {
            OnProfilePhotoChange?.Invoke(newPhotoUrl);
        }
    }
} 