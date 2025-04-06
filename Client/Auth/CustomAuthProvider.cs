using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using System.Threading.Tasks;
using Blazored.LocalStorage;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using BlazorApp1.Client.Services;

namespace BlazorApp1.Client.Auth
{
    public class CustomAuthProvider : AuthenticationStateProvider
    {
        private readonly ILocalStorageService _localStorage;
        private readonly ISecureStorageService _secureStorage;
        private readonly JwtSecurityTokenHandler _tokenHandler;
        private readonly ClaimsPrincipal _anonymous = new ClaimsPrincipal(new ClaimsIdentity());

        public CustomAuthProvider(ILocalStorageService localStorage, ISecureStorageService secureStorage)
        {
            _localStorage = localStorage;
            _secureStorage = secureStorage;
            _tokenHandler = new JwtSecurityTokenHandler();
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                var savedToken = await _localStorage.GetItemAsync<string>("jwt-access-token");
                if (string.IsNullOrEmpty(savedToken))
                {
                    return await Task.FromResult(new AuthenticationState(_anonymous));
                }

                var tokenContent = _tokenHandler.ReadJwtToken(savedToken);
                var expiry = tokenContent.ValidTo;
                if (expiry < DateTime.UtcNow)
                {
                    await _localStorage.RemoveItemAsync("jwt-access-token");
                    await _secureStorage.ClearUserDataAsync();
                    return await Task.FromResult(new AuthenticationState(_anonymous));
                }

                // Extract claims from token
                var claims = new List<Claim>();
                claims.AddRange(tokenContent.Claims);

                // Ensure we have the minimum required claims
                var nameIdClaim = claims.FirstOrDefault(c => c.Type == "nameid" || c.Type == ClaimTypes.NameIdentifier);
                var emailClaim = claims.FirstOrDefault(c => c.Type == "email" || c.Type == ClaimTypes.Email);
                var roleClaim = claims.FirstOrDefault(c => c.Type == "role" || c.Type == ClaimTypes.Role);

                if (nameIdClaim == null || emailClaim == null)
                {
                    await _localStorage.RemoveItemAsync("jwt-access-token");
                    await _secureStorage.ClearUserDataAsync();
                    return await Task.FromResult(new AuthenticationState(_anonymous));
                }

                // Create standardized claims
                var standardClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, nameIdClaim.Value),
                    new Claim(ClaimTypes.Email, emailClaim.Value)
                };

                if (roleClaim != null)
                {
                    standardClaims.Add(new Claim(ClaimTypes.Role, roleClaim.Value));
                }

                var identity = new ClaimsIdentity(standardClaims, "jwt");
                var principal = new ClaimsPrincipal(identity);

                return await Task.FromResult(new AuthenticationState(principal));
            }
            catch (Exception)
            {
                await _secureStorage.ClearUserDataAsync();
                return await Task.FromResult(new AuthenticationState(_anonymous));
            }
        }

        public async Task NotifyUserAuthenticationAsync(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                await _localStorage.RemoveItemAsync("jwt-access-token");
                await _secureStorage.ClearUserDataAsync();
                NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_anonymous)));
                return;
            }

            try
            {
                await _localStorage.SetItemAsync("jwt-access-token", token);
                var tokenContent = _tokenHandler.ReadJwtToken(token);

                var claims = new List<Claim>();
                claims.AddRange(tokenContent.Claims);

                // Ensure we have the minimum required claims
                var nameIdClaim = claims.FirstOrDefault(c => c.Type == "nameid" || c.Type == ClaimTypes.NameIdentifier);
                var emailClaim = claims.FirstOrDefault(c => c.Type == "email" || c.Type == ClaimTypes.Email);
                var roleClaim = claims.FirstOrDefault(c => c.Type == "role" || c.Type == ClaimTypes.Role);

                if (nameIdClaim == null || emailClaim == null)
                {
                    await _localStorage.RemoveItemAsync("jwt-access-token");
                    await _secureStorage.ClearUserDataAsync();
                    NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_anonymous)));
                    return;
                }

                // Create standardized claims
                var standardClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, nameIdClaim.Value),
                    new Claim(ClaimTypes.Email, emailClaim.Value)
                };

                if (roleClaim != null)
                {
                    standardClaims.Add(new Claim(ClaimTypes.Role, roleClaim.Value));
                }

                var identity = new ClaimsIdentity(standardClaims, "jwt");
                var principal = new ClaimsPrincipal(identity);

                NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(principal)));
            }
            catch (Exception)
            {
                await _localStorage.RemoveItemAsync("jwt-access-token");
                await _secureStorage.ClearUserDataAsync();
                NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_anonymous)));
            }
        }

        public async Task Logout()
        {
            await _localStorage.RemoveItemAsync("jwt-access-token");
            await _secureStorage.ClearUserDataAsync();
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_anonymous)));
        }
    }
} 