using CanvasFlow.Api.Data;
using CanvasFlow.Api.Hubs;
using CanvasFlow.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// 1. Configure the Database Context (MySQL/MariaDB)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString)
    ));

// 2. Configure JWT Authentication (З ПІДТРИМКОЮ SIGNALR)
var jwtKey = builder.Configuration["Jwt:Key"] ?? "ThisIsASuperSecretKeyForTesting123!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "CanvasFlow";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "CanvasFlowClients";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience
        };

        // КРИТИЧНО ДЛЯ SIGNALR: Навчаємо бекенд читати токен з URL (query string)
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                // Перевіряємо, чи запит йде до хабів
                if (!string.IsNullOrEmpty(accessToken) &&
                    (path.StartsWithSegments("/chathub") || path.StartsWithSegments("/notificationhub")))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// 2.5 Configure CORS (Максимально відкритий, сумісний з AllowCredentials)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.SetIsOriginAllowed(origin => true) // Динамічно дозволяє будь-який Origin (замінює AllowAnyOrigin)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();                // Дозволяє передачу кукі/токенів для SignalR
    });
});

// 3. Register Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IContentService, ContentService>();
builder.Services.AddMemoryCache();
// --- MODULE 3 REGISTRATIONS ---
builder.Services.AddScoped<IMessagingService, MessagingService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
// -----------------------------
// --- MODULE 4 REGISTRATIONS ---
builder.Services.AddScoped<IAuditService, AuditService>();
// -----------------------------

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Введіть 'Bearer' [пробіл] і далі ваш токен.\r\n\r\nНаприклад: \"Bearer eyJhbGciOiJIUzI1Ni...\""
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>() 
        }
    });
});

// 4. Configure SignalR
builder.Services.AddSignalR();
builder.Services.AddHttpClient();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

//app.UseHttpsRedirection();
app.UseStaticFiles();

// 5. Middleware Pipeline (Порядок має критичне значення!)
app.UseRouting(); // 1. Спочатку визначаємо маршрут

app.UseCors("AllowAll"); // 2. Потім застосовуємо CORS до цього маршруту

app.UseAuthentication(); // 3. Перевіряємо, хто прийшов
app.UseAuthorization();  // 4. Перевіряємо, чи має він права доступу

app.MapControllers();

// 6. Map SignalR Hubs
app.MapHub<NotificationHub>("/notificationhub");
app.MapHub<ChatHub>("/chathub");

app.Run();