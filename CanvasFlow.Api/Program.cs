using CanvasFlow.Api.Data;
using CanvasFlow.Api.Hubs;
using CanvasFlow.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// 1. Configure the Database Context (MS SQL)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString) // Pomelo ����������� ��������� ����� ���� MariaDB/MySQL
    ));

// 2. Configure JWT Authentication
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
    });

builder.Services.AddAuthorization();

// 3. Register Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IContentService, ContentService>();
// --- MODULE 3 REGISTRATIONS ---
builder.Services.AddScoped<IMessagingService, MessagingService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
// -----------------------------
// --- MODULE 4 REGISTRATIONS ---
builder.Services.AddScoped<IAuditService, AuditService>();
// -----------------------------

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 4. Configure SignalR
builder.Services.AddSignalR();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication(); // Must come before UseAuthorization
app.UseAuthorization();
app.MapControllers();

// 5. Map SignalR Hub
app.MapHub<NotificationHub>("/notificationhub");

app.Run();