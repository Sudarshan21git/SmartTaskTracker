using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TaskTrackerAPI.Data;
using TaskTrackerAPI.Models;
using TaskTrackerAPI.Services;
using TaskTrackerAPI.Hubs;
using TaskTrackerAPI.BackgroundServices;
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// ==================== Add services ====================

// Add Controllers
builder.Services.AddControllers();

// Add DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Token validation settings
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
              Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)
)
        };

        // ✅ FIXED: Support JWT for SignalR connections
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var token = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                // Updated to match the hub path
                if (!string.IsNullOrEmpty(token) &&
                    (path.StartsWithSegments("/notificationHub") ||
                     path.StartsWithSegments("/hubs/taskNotification")))
                {
                    context.Token = token;
                }
                return Task.CompletedTask;
            }
        };
    });

// Add Authorization
builder.Services.AddAuthorization();

// ==================== FIXED: CORS for SignalR ====================
// ⚠️ CRITICAL: .AllowAnyOrigin() does NOT work with SignalR!
// SignalR requires .AllowCredentials() which cannot be used with AllowAnyOrigin
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
            "https://localhost:7099",  // Your frontend HTTPS port
            "https://localhost:7001",  // Alternative frontend port
            "http://localhost:7099",   // HTTP version
            "http://localhost:7001",   // HTTP version
            "https://localhost:5001",  // Another common port
            "http://localhost:5001"    // HTTP version
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()  // ✅ REQUIRED for SignalR - this is the key fix!
        .SetIsOriginAllowed(origin => true); // For development - allows dynamic origins
    });
});

// ==================== Register Custom Services ====================
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<IEmailService, EmailService>();
// ==================== SignalR ====================
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true; // Helpful for debugging
});

// ==================== Background Service ====================
builder.Services.AddHostedService<TaskDueNotificationService>();

// ==================== Build App ====================
var app = builder.Build();

// ==================== Middleware ====================
// ⚠️ CRITICAL: Order matters! CORS must come BEFORE Authentication

// Enable HTTPS Redirection
app.UseHttpsRedirection();

// ✅ FIXED: Enable CORS (with the correct policy name)
app.UseCors("AllowFrontend");  // Changed from "AllowAll" to "AllowFrontend"

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Map SignalR Hub - keeping your original path
app.MapHub<TaskNotificationHub>("/notificationHub");

// You can also map it to the alternative path if you want both to work:
app.MapHub<TaskNotificationHub>("/hubs/taskNotification");

// Map Controllers
app.MapControllers();

// ==================== Log Startup Info ====================
Console.WriteLine("========================================");
Console.WriteLine("✓ TaskTracker API Started Successfully");
Console.WriteLine($"✓ SignalR Hub: /notificationHub");
Console.WriteLine($"✓ SignalR Hub: /hubs/taskNotification");
Console.WriteLine($"✓ CORS Allowed: https://localhost:7099");
Console.WriteLine("========================================");

// ==================== Run App ====================
app.Run();