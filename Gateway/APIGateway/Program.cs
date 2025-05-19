using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Cache.CacheManager;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

// Add Ocelot configuration files
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddJsonFile("ocelot.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();

// Add Ocelot services - keeping it simple
builder.Services
    .AddOcelot(builder.Configuration)
    .AddCacheManager(x => x.WithDictionaryHandle());

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins",
        builder => builder
            .WithOrigins("http://localhost:5001", "http://localhost:3000","https://camel-casual-wrongly.ngrok-free.app")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());
});

// Add JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer("Bearer", options =>
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
        
        // Extract token from cookie
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var tokenFromCookie = context.Request.Cookies["LocalAccessToken"];
                if (!string.IsNullOrEmpty(tokenFromCookie))
                {
                    context.Token = tokenFromCookie.Trim();
                }
                return Task.CompletedTask;
            },
            // Return clear authentication error
            OnChallenge = context =>
            {
                if (!context.Handled)
                {
                    context.Response.StatusCode = 401;
                    context.Response.ContentType = "application/json";
                    var result = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        StatusCode = 401,
                        Message = "You are not authorized to access this resource. Please log in."
                    });
                    context.Response.WriteAsync(result);
                    context.HandleResponse();
                }
                return Task.CompletedTask;
            },
            // Handle forbidden responses
            OnForbidden = context =>
            {
                context.Response.StatusCode = 403;
                context.Response.ContentType = "application/json";
                var result = System.Text.Json.JsonSerializer.Serialize(new
                {
                    StatusCode = 403,
                    Message = "You don't have permission to access this resource."
                });
                context.Response.WriteAsync(result);
                return Task.CompletedTask;
            }
        };
    });

// Add authorization
builder.Services.AddAuthorization();

var app = builder.Build();

// Simple status code handling for meaningful errors
app.UseStatusCodePages(async context =>
{
    var response = context.HttpContext.Response;
    
    if (response.StatusCode == 401)
    {
        response.ContentType = "application/json";
        await response.WriteAsJsonAsync(new
        {
            StatusCode = 401,
            Message = "You are not authorized to access this resource. Please log in."
        });
    }
    else if (response.StatusCode == 403)
    {
        response.ContentType = "application/json";
        await response.WriteAsJsonAsync(new
        {
            StatusCode = 403,
            Message = "You don't have permission to access this resource."
        });
    }
    else if (response.StatusCode == 404)
    {
        response.ContentType = "application/json";
        await response.WriteAsJsonAsync(new
        {
            StatusCode = 404,
            Message = "The requested resource was not found."
        });
    }
});

// CORS middleware
app.UseCors("AllowSpecificOrigins");

// Authentication and Authorization
app.UseAuthentication();
app.UseAuthorization();

// Forward JWT tokens from cookie to Authorization header
app.Use(async (context, next) =>
{
    var tokenFromCookie = context.Request.Cookies["LocalAccessToken"];
    if (!string.IsNullOrEmpty(tokenFromCookie) && !context.Request.Headers.ContainsKey("Authorization"))
    {
        context.Request.Headers.Append("Authorization", $"Bearer {tokenFromCookie.Trim()}");
    }
    await next();
});

// Use Ocelot middleware
await app.UseOcelot();

app.Run();