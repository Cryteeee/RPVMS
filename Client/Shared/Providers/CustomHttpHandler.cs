using Blazored.LocalStorage;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace BlazorApp1.Client.Shared.Providers
{
    public class CustomHttpHandler : DelegatingHandler
    {
        private readonly ILocalStorageService _localStorageService;

        public CustomHttpHandler(ILocalStorageService localStorageService)
        {
            _localStorageService = localStorageService;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Add CORS headers for all requests
            if (!request.Headers.Contains("Origin"))
            {
                var origin = request.RequestUri.Host.Contains("localhost")
                    ? "https://localhost:7052"
                    : "https://main.d3445jgtnjwhm9.amplifyapp.com";
                request.Headers.Add("Origin", origin);
            }

            // Handle authentication
            if (request.RequestUri.AbsolutePath.ToLower().Contains("login") ||
                request.RequestUri.AbsolutePath.ToLower().Contains("register") ||
                request.RequestUri.AbsolutePath.ToLower().Contains("check-email"))
            {
                return await base.SendAsync(request, cancellationToken);
            }

            var token = await _localStorageService.GetItemAsync<string>("jwt-access-token");
            if (!string.IsNullOrEmpty(token))
            {
                if (!request.Headers.Contains("Authorization"))
                {
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                }
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
