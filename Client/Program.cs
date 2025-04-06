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
    builder.Services.AddHttpClient("ManagementSystem", client =>
    {
        client.BaseAddress = new Uri("https://localhost:7052/");
        client.DefaultRequestHeaders.Add("Accept", "application/json");
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
