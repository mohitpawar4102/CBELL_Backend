using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using System.Text;
using YourNamespace.Services;
using YourNamespace.Library.Database;
using YourNamespace.Models;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
// using Newtonsoft.Json; // Add this to ensure Newtonsoft.Json is loaded

var builder = WebApplication.CreateBuilder(args);

// Force Newtonsoft.Json to load
// JsonConvert.DefaultSettings = () => new JsonSerializerSettings();

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
                // Clean the token
                tokenFromCookie = tokenFromCookie.Trim();
                context.Token = tokenFromCookie;
                Console.WriteLine("Token extracted from cookie :"+ tokenFromCookie);
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
    throw new InvalidOperationException("MongoDB configuration is missing.");
}

var mongoClient = new MongoClient(mongoConnectionString);
var database = mongoClient.GetDatabase(databaseName);
builder.Services.AddSingleton(database);

// Register your services
builder.Services.AddScoped<EventService>();
builder.Services.AddSingleton<TaskService>();
builder.Services.AddScoped<EventTypeService>();
builder.Services.AddScoped<OrganizationService>();
builder.Services.AddScoped<DocumentService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<ChatThreadService>();
builder.Services.AddScoped<DocumentDetailsService>();
builder.Services.AddSingleton<MongoDbService>();

// Add controllers
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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// CORS middleware
app.UseCors("AllowSpecificOrigins");

// Add this middleware before authentication to copy cookie to header
app.Use(async (context, next) =>
{
    var tokenFromCookie = context.Request.Cookies["LocalAccessToken"];
    if (!string.IsNullOrEmpty(tokenFromCookie) && !context.Request.Headers.ContainsKey("Authorization"))
    {
        context.Request.Headers.Append("Authorization", $"Bearer {tokenFromCookie.Trim()}");
        Console.WriteLine("Added Authorization header from cookie");
    }
    await next();
});

app.UseAuthentication();

// Add this to enrich user context after authentication
app.Use(async (context, next) =>
{
    if (context.User.Identity.IsAuthenticated)
    {
        try
        {
            // Get MongoDB service
            var mongoDbService = context.RequestServices.GetRequiredService<MongoDbService>();
            
            // Get user ID from claims
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (!string.IsNullOrEmpty(userId))
            {
                // Fetch user from database
                var usersCollection = mongoDbService.GetDatabase().GetCollection<User>("Users");
                var user = await usersCollection.Find(u => u.Id == userId).FirstOrDefaultAsync();
                
                if (user != null)
                {
                    // Store user in HttpContext.Items
                    context.Items["CurrentUser"] = user;
                    
                    // Get roles if available
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
            Console.WriteLine($"Error fetching user data: {ex.Message}");
        }
    }
    
    await next();
});

app.UseAuthorization();

// Logging middleware
app.Use(async (context, next) =>
{
    if (context.User.Identity.IsAuthenticated)
    {
        Console.WriteLine("User is authenticated!");
    }
    else
    {
        Console.WriteLine("User is NOT authenticated");
    }
    
    await next();
});

app.MapControllers();
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