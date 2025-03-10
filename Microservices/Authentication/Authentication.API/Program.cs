using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using YourNamespace.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<TokenService>(); // Ensure TokenService is registered

// Enable CORS (needed for frontend API calls with cookies)
// builder.Services.AddCors(options =>
// {
//     options.AddPolicy("AllowFrontend",
//         policy => policy
//             .WithOrigins("https://your-frontend-url.com") // Change this to match your frontend
//             .AllowCredentials()
//             .AllowAnyHeader()
//             .AllowAnyMethod());
// });

builder.Services.Configure<CookiePolicyOptions>(options =>
        {
            // This lambda determines whether user consent for non-essential cookies is needed for a given request.
            options.CheckConsentNeeded = context => true;
            options.MinimumSameSitePolicy = SameSiteMode.None;
        });


// Add Authentication services
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var key = Encoding.UTF8.GetBytes(builder.Configuration["Authentication:Jwt:Secret"]);

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
}).AddCookie(options =>
            {
                options.Cookie.IsEssential = true;
            })
.AddGoogle(options =>
{
    options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme; // Ensure sign-in works
    options.ClientId = builder.Configuration["Google:ClientId"];
    options.ClientSecret = builder.Configuration["Google:ClientSecret"];
    // options.CallbackPath = ""; // Remove spaces
});

builder.Services.AddAuthorization();
builder.Services.AddControllers();

var app = builder.Build();

app.UseCors("AllowFrontend"); // Enable CORS before authentication

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
