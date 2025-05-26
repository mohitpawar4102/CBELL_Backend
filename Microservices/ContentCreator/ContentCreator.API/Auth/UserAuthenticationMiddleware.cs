using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using System.Threading.Tasks;
using YourNamespace.Models;
using YourNamespace.Library.Database;
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace YourNamespace.Middleware
{
    public class UserAuthenticationMiddleware
    {
        private readonly RequestDelegate _next;

        public UserAuthenticationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, MongoDbService mongoDbService)
        {
            // Skip if not authenticated yet
            if (!context.User.Identity.IsAuthenticated)
            {
                await _next(context);
                return;
            }

            try
            {
                // Get user ID from the authenticated claims
                var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                
                if (string.IsNullOrEmpty(userId))
                {
                    Console.WriteLine("User ID claim not found in authenticated user");
                    await _next(context);
                    return;
                }

                // Fetch complete user details from database
                var usersCollection = mongoDbService.GetDatabase().GetCollection<User>("Users");
                var user = await usersCollection.Find(u => u.Id == userId).FirstOrDefaultAsync();

                if (user == null)
                {
                    Console.WriteLine($"User with ID {userId} not found in database");
                    await _next(context);
                    return;
                }

                // Get roles for the user
                var rolesCollection = mongoDbService.GetDatabase().GetCollection<Role>("Roles");
                var roles = await rolesCollection.Find(r => user.RoleIds.Contains(r.Id) && r.IsActive).ToListAsync();

                // Store complete user in HttpContext.Items for later use
                context.Items["CurrentUser"] = user;
                context.Items["UserRoles"] = roles;

                Console.WriteLine($"Added user {user.Email} to HttpContext.Items");
            }
            catch (Exception ex)
            {
                // Log error but continue
                Console.WriteLine($"Error enriching user context: {ex.Message}");
            }

            await _next(context);
        }
    }

    // Extension method for using the middleware
    public static class UserAuthenticationMiddlewareExtensions
    {
        public static IApplicationBuilder UseUserContext(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<UserAuthenticationMiddleware>();
        }
    }
}