using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using System.Net.Http;
using BlazorApp1.Shared;
using BlazorApp1.Client;
using CurrieTechnologies.Razor.SweetAlert2;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using BlazorApp1.Client.Services;
using BlazorApp1.Client.Auth;
using BlazorApp1.Client.Shared.Providers;
using Microsoft.Extensions.DependencyInjection;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

try
{
    builder.RootComponents.Add<App>("#app");
    builder.RootComponents.Add<HeadOutlet>("head::after");

    // Register LocalStorage and Auth services first
    builder.Services.AddBlazoredLocalStorage();
    builder.Services.AddAuthorizationCore();
    builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthProvider>();
    builder.Services.AddScoped<ISecureStorageService, SecureStorageService>();
    builder.Services.AddScoped<ISubmissionService, SubmissionService>();

    // Configure HttpClient with custom handler
    builder.Services.AddScoped<CustomHttpHandler>();
    
    // Use the appropriate API endpoint based on environment
    var apiBaseAddress = builder.HostEnvironment.IsDevelopment() 
        ? "https://localhost:7052/"
        : "https://api.d3445jgtnjwhm9.amplifyapp.com/";

    builder.Services.AddHttpClient("ManagementSystem", client =>
    {
        client.BaseAddress = new Uri(apiBaseAddress);
        client.DefaultRequestHeaders.Add("Accept", "application/json");
        client.DefaultRequestHeaders.Add("Cache-Control", "no-cache, no-store, must-revalidate");
        client.DefaultRequestHeaders.Add("X-Client-Source", "BlazorWASM");
        
        // Set the correct origin based on the environment
        var origin = builder.HostEnvironment.IsDevelopment()
            ? "https://localhost:7052"
            : "https://main.d3445jgtnjwhm9.amplifyapp.com";
            
        // Don't add Origin header here, let the browser handle it
        client.Timeout = TimeSpan.FromSeconds(30);
    }).AddHttpMessageHandler<CustomHttpHandler>();

    // Register services that depend on HttpClient
    builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("ManagementSystem"));
    builder.Services.AddScoped<IAccountService, AccountService>();
    builder.Services.AddScoped<IBillingService, BillingService>();
    builder.Services.AddScoped<IBoardMessageService, BoardMessageService>();
    builder.Services.AddScoped<INotificationService, NotificationService>();
    builder.Services.AddScoped<IContactService, ContactService>();
    builder.Services.AddScoped<IEventService, EventService>();
    
    // Register other services
    builder.Services.AddScoped<ProfileStateService>();
    builder.Services.AddSingleton<ProfilePhotoService>();
    builder.Services.AddSweetAlert2();
    builder.Services.AddScoped<IPdfService, PdfService>();

    var app = builder.Build();
    await app.RunAsync();
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Application startup failed: {ex}");
    throw;
}
