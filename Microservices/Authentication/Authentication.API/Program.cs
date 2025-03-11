using YourNamespace.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Register services in DI container
builder.Services.AddScoped<TokenService>(); // TokenService for JWT generation
builder.Services.AddScoped<AuthService>();  // Register AuthService for authentication logic
builder.Services.AddHttpContextAccessor();  // Required for IHttpContextAccessor

// Authentication and Authorization configuration
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var secret = builder.Configuration["Authentication:Jwt:Secret"];
    if (secret == null)
    {
        throw new InvalidOperationException("JWT secret is not configured.");
    }

    var key = Encoding.UTF8.GetBytes(secret);
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Authentication:Jwt:Issuer"],
        ValidAudience = builder.Configuration["Authentication:Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };

    // Extract JWT from cookies if no Authorization header is found
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var tokenFromCookie = context.Request.Cookies["AuthToken"];
            if (!string.IsNullOrEmpty(tokenFromCookie))
            {
                context.Token = tokenFromCookie;
            }
            return Task.CompletedTask;
        }
    };
})
.AddCookie(options =>
{
    options.Cookie.IsEssential = true;
})
.AddGoogle(options =>
{
    var clientId = builder.Configuration.GetValue<string>("Google:ClientId");
    var clientSecret = builder.Configuration.GetValue<string>("Google:ClientSecret");
    options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
    {
        throw new InvalidOperationException("Missing required Google authentication configuration.");
    }

    options.ClientId = clientId;
    options.ClientSecret = clientSecret;
});

builder.Services.AddAuthorization();
builder.Services.AddControllers();

var app = builder.Build();

// Configure the middleware pipeline
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
