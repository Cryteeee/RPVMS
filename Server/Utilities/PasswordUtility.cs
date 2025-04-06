using Microsoft.AspNetCore.Identity;

namespace BlazorApp1.Server.Utilities
{
    public static class PasswordUtility
    {
        private static readonly PasswordHasher<IdentityUser> _hasher = new PasswordHasher<IdentityUser>();

        public static string HashPassword(string password)
        {
            return _hasher.HashPassword(null, password);
        }

        public static bool VerifyPassword(string password, string hash)
        {
            try
            {
                var result = _hasher.VerifyHashedPassword(null, hash, password);
                return result != PasswordVerificationResult.Failed;
            }
            catch
            {
                return false;
            }
        }
    }
}
