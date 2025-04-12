using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BlazorApp1.Server.Data;
using BlazorApp1.Shared;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.FileProviders;
using BlazorApp1.Client.Services;
using BlazorApp1.Server.Services;
using Blazored.LocalStorage;
using System.Security.Claims;
using BlazorApp1.Server.Hubs;
using BlazorApp1.Shared.Constants;
using Microsoft.AspNetCore.Identity;
using BlazorApp1.Shared.Models;
using BlazorApp1.Server.Utilities;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Configure JSON options
var jsonOptions = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
    PropertyNamingPolicy = null,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
    ReferenceHandler = ReferenceHandler.IgnoreCycles
};
jsonOptions.Converters.Add(new JsonStringEnumConverter());

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault;
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Add memory cache for IP tracking
builder.Services.AddMemoryCache();

builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
    options.SerializerOptions.PropertyNamingPolicy = null;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault;
    options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpClient();
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault;
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddSwaggerGen();
builder.Services.AddScoped(http => new HttpClient { BaseAddress = new Uri(builder.Configuration.GetSection("BaseUri").Value!) });

// Configure Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure()));

// Add Identity configuration
builder.Services.AddIdentity<User, IdentityRole<int>>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 8;
    
    // User settings
    options.User.RequireUniqueEmail = true;
    
    // SignIn settings
    options.SignIn.RequireConfirmedEmail = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Add HttpClient configuration
builder.Services.AddHttpClient("ManagementSystem", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["BaseUri"] ?? "https://main.d3445jgtnjwhm9.amplifyapp.com/");
    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
});

// Configure JSON options
builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// Make database migration conditional
if (builder.Environment.IsDevelopment())
{
    using (var scope = builder.Services.BuildServiceProvider().CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        dbContext.Database.Migrate();
    }
}

// Add UserService for validation
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<BlazorApp1.Server.Services.IContactService, BlazorApp1.Server.Services.ContactService>();
builder.Services.AddScoped<BlazorApp1.Server.Services.IEventService, BlazorApp1.Server.Services.EventService>();

// Add NotificationService
builder.Services.AddScoped<BlazorApp1.Server.Services.INotificationService, BlazorApp1.Server.Services.NotificationService>();

// Add StaffService
builder.Services.AddScoped<IStaffService, StaffService>();

// Configure Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var jwtKey = builder.Configuration["JwtSettings:SecretKey"] ?? "your-256-bit-secret";
    var issuer = builder.Configuration["JwtSettings:Issuer"] ?? "https://main.d3445jgtnjwhm9.amplifyapp.com";
    var audience = builder.Configuration["JwtSettings:Audience"] ?? "https://main.d3445jgtnjwhm9.amplifyapp.com";

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = issuer,
        ValidAudience = audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            
            if (!string.IsNullOrEmpty(accessToken) && 
                (path.StartsWithSegments("/userhub") || 
                 path.StartsWithSegments("/notificationHub") || 
                 path.StartsWithSegments("/boardMessageHub")))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SuperAdmin", policy =>
        policy.RequireRole(UserRoles.SuperAdmin));
        
    options.AddPolicy("Admin", policy =>
        policy.RequireRole(UserRoles.SuperAdmin, UserRoles.Admin));
        
    options.AddPolicy("Client", policy =>
        policy.RequireRole(UserRoles.SuperAdmin, UserRoles.Admin, UserRoles.Client));
        
    options.AddPolicy("User", policy =>
        policy.RequireRole(UserRoles.SuperAdmin, UserRoles.Admin, UserRoles.User, UserRoles.Client));

    // Default fallback policy
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder
                .SetIsOriginAllowed(_ => true) // Be careful with this in production
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        });
});

builder.Services.AddRazorPages();
builder.Services.AddSignalR();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseBlazorFrameworkFiles();

// Serve static files from wwwroot
app.UseStaticFiles();

app.UseRouting();

// Add cache control middleware
app.Use(async (context, next) =>
{
    context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
    context.Response.Headers["Pragma"] = "no-cache";
    context.Response.Headers["Expires"] = "0";
    await next();
});

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

// Map controllers and hubs without requiring authorization by default
app.MapControllers();
app.MapRazorPages();
app.MapHub<UserHub>("/userhub");
app.MapHub<NotificationHub>("/notificationHub");
app.MapHub<BoardMessageHub>("/boardMessageHub");

// This should be last
app.MapFallbackToFile("index.html");

// Initialize database and SuperAdmin only in Development
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<User>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<int>>>();
        var logger = services.GetRequiredService<ILogger<Program>>();

        try
        {
            logger.LogInformation("Checking database...");
            if (!await context.Database.CanConnectAsync())
            {
                logger.LogInformation("Creating database and applying migrations...");
                await context.Database.MigrateAsync();
                logger.LogInformation("Database created and migrations applied successfully.");
            }

            // Initialize roles and SuperAdmin
            logger.LogInformation("Starting Super Admin initialization...");
            
            // Ensure roles exist
            var roles = new[] { UserRoles.SuperAdmin, UserRoles.Admin, UserRoles.User, UserRoles.Client };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole<int> { Name = role });
                    logger.LogInformation($"Created role: {role}");
                }
            }

            // Check if SuperAdmin exists
            var superAdminEmail = "superadmin@village.com";
            var superAdmin = await userManager.FindByEmailAsync(superAdminEmail);

            if (superAdmin == null)
            {
                // Create SuperAdmin user
                superAdmin = new User
                {
                    UserName = "SuperAdmin",
                    Email = superAdminEmail,
                    EmailConfirmed = true,
                    IsEmailVerified = true,
                    IsActive = true,
                    Role = UserRoles.SuperAdmin,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(superAdmin, "Admin@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(superAdmin, UserRoles.SuperAdmin);
                    logger.LogInformation("SuperAdmin user created successfully");
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during database/SuperAdmin initialization");
            // Log error but don't throw in development
        }
    }
}

app.Run();
