using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using System.Text;
using YourNamespace.Services;
using YourNamespace.Library.Database;
using YourNamespace.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Hangfire;
using Hangfire.Mongo;
using Hangfire.Mongo.Migration;
using Hangfire.Mongo.Migration.Strategies;
using Hangfire.Mongo.Migration.Strategies.Backup;

var builder = WebApplication.CreateBuilder(args);



// JWT Authentication configuration
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Authentication:Jwt:Issuer"],
        ValidAudience = builder.Configuration["Authentication:Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Authentication:Jwt:Secret"]))
    };
    
    // Custom token extraction
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            // Get token from cookie
            var tokenFromCookie = context.Request.Cookies["LocalAccessToken"];
            if (!string.IsNullOrEmpty(tokenFromCookie))
            {
                context.Token = tokenFromCookie.Trim();
            }
            return Task.CompletedTask;
        }
    };
});

// MongoDB setup
var mongoConnectionString = builder.Configuration.GetConnectionString("MongoDb");
var databaseName = builder.Configuration["MongoDbSettings:DatabaseName"];

if (string.IsNullOrEmpty(mongoConnectionString) || string.IsNullOrEmpty(databaseName))
{
    throw new InvalidOperationException("MongoDB connection string or database name is not configured properly in application settings.");
}

var mongoClient = new MongoClient(mongoConnectionString);
var database = mongoClient.GetDatabase(databaseName);
builder.Services.AddSingleton(database);

// Register services
builder.Services.AddScoped<EventService>();
builder.Services.AddSingleton<TaskService>();
builder.Services.AddScoped<EventTypeService>();
builder.Services.AddScoped<OrganizationService>();
builder.Services.AddScoped<DocumentService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<ChatThreadService>();
builder.Services.AddScoped<DocumentDetailsService>();
builder.Services.AddSingleton<MongoDbService>();
builder.Services.AddScoped<EmailService>();

// Add HttpClient for social media services
// builder.Services.AddHttpClient<SocialMediaService>();

// Add social media service
// builder.Services.AddScoped<SocialMediaService>();

// Add controllers and API documentation
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins",
        builder => builder
            .WithOrigins("http://localhost:5001", "http://localhost:3000")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());
});

// Hangfire configuration
builder.Services.AddHangfire(config =>
    config.UseMongoStorage(
        builder.Configuration.GetConnectionString("MongoDb"),
        "hangfire_jobs",
        new MongoStorageOptions
        {
            MigrationOptions = new MongoMigrationOptions
            {
                MigrationStrategy = new MigrateMongoMigrationStrategy(),
                BackupStrategy = new CollectionMongoBackupStrategy()
            }
        }
    )
);
builder.Services.AddHangfireServer();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// CORS middleware
app.UseCors("AllowSpecificOrigins");

// Copy JWT token from cookie to authorization header
app.Use(async (context, next) =>
{
    var tokenFromCookie = context.Request.Cookies["LocalAccessToken"];
    if (!string.IsNullOrEmpty(tokenFromCookie) && !context.Request.Headers.ContainsKey("Authorization"))
    {
        context.Request.Headers.Append("Authorization", $"Bearer {tokenFromCookie.Trim()}");
    }
    await next();
});

app.UseAuthentication();

// Enrich user context after authentication
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        try
        {
            var mongoDbService = context.RequestServices.GetRequiredService<MongoDbService>();
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (!string.IsNullOrEmpty(userId))
            {
                var usersCollection = mongoDbService.GetDatabase().GetCollection<User>("Users");
                var user = await usersCollection.Find(u => u.Id == userId).FirstOrDefaultAsync();
                
                if (user != null)
                {
                    context.Items["CurrentUser"] = user;
                    
                    if (user.RoleIds?.Any() == true)
                    {
                        var rolesCollection = mongoDbService.GetDatabase().GetCollection<Role>("Roles");
                        var roles = await rolesCollection.Find(r => user.RoleIds.Contains(r.Id)).ToListAsync();
                        context.Items["UserRoles"] = roles;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // Log the error or handle it according to your application's needs
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new 
            { 
                error = "Error while retrieving user information",
                details = app.Environment.IsDevelopment() ? ex.Message : null
            });
            return;
        }
    }
    
    await next();
});

app.UseAuthorization();
app.MapControllers();

app.UseHangfireDashboard(); // Optional: for dashboard

app.Run();

// Extension methods for easy access to user data
public static class HttpContextExtensions
{
    public static User GetCurrentUser(this HttpContext context)
    {
        if (context.Items.TryGetValue("CurrentUser", out var userObj) && userObj is User user)
        {
            return user;
        }
        return null;
    }
    
    public static List<Role> GetUserRoles(this HttpContext context)
    {
        if (context.Items.TryGetValue("UserRoles", out var rolesObj) && rolesObj is List<Role> roles)
        {
            return roles;
        }
        return new List<Role>();
    }
}