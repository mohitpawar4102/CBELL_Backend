using YourNamespace.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using System.Text;
using YourNamespace.Library.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.AspNetCore.CookiePolicy;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseKestrel(options => options.ListenAnyIP(80))
.UseUrls("http://+:80");

// Register MongoDB
var mongoConnectionString = builder.Configuration.GetConnectionString("MongoDb");
var databaseName = builder.Configuration["MongoDbSettings:DatabaseName"];

if (string.IsNullOrEmpty(mongoConnectionString) || string.IsNullOrEmpty(databaseName))
{
    throw new InvalidOperationException("MongoDB configuration is missing.");
}

var mongoClient = new MongoClient(mongoConnectionString);
var database = mongoClient.GetDatabase(databaseName);
builder.Services.AddSingleton(database);

// Register services in DI container
builder.Services.AddScoped<TokenService>(); // TokenService for JWT generation
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<AuthService>();  // Register AuthService for authentication logic
builder.Services.AddScoped<FeatureService>();
builder.Services.AddScoped<ModuleService>();
builder.Services.AddScoped<RoleService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<MongoDbService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder => builder
            .WithOrigins("http://localhost:5002", "http://localhost:3000") // Specific origins instead of AllowAnyOrigin
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()); // Required for cookies
});
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
            var tokenFromCookie = context.Request.Cookies["LocalAccessToken"];
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
    var clientId = builder.Configuration["Google:ClientId"];
    var clientSecret = builder.Configuration["Google:ClientSecret"];
    options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;

    if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
    {
        throw new InvalidOperationException("Missing required Google authentication configuration.");
    }

    options.ClientId = clientId;
    options.ClientSecret = clientSecret;

    // Request email and profile scopes (already included by default)
    options.Scope.Add("email");
    options.Scope.Add("profile");

    // Important: Request offline access to receive refresh tokens
    options.Scope.Add("https://www.googleapis.com/auth/userinfo.email");
    options.Scope.Add("https://www.googleapis.com/auth/userinfo.profile");
    options.Scope.Add("openid");

    options.SaveTokens = true; // Ensure tokens are saved
    options.AccessType = "offline"; // Requests a refresh token
    options.ClaimActions.MapJsonKey(ClaimTypes.Email, "email");
});

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = false;
});

builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.MinimumSameSitePolicy = SameSiteMode.Lax;
});


builder.Services.AddAuthorization();
builder.Services.AddControllers();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Configure the middleware pipeline
app.UseCors("AllowAll");
// app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await app.RunAsync();
