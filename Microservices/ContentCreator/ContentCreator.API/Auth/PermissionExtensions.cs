using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Json;
using YourNamespace.Models;
using Microsoft.AspNetCore.Http;

namespace YourApiMicroservice.Auth
{
    public static class PermissionExtensions
    {
        public static bool HasPermission(this ClaimsPrincipal user, string module, string feature, string action)
        {
            // Get the permissions claim
            var permissionsClaim = user.FindFirst("permissions");
            if (permissionsClaim == null)
                return false;
                
            try
            {
                var permissions = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, List<string>>>>(
                    permissionsClaim.Value);
                    
                return permissions != null && 
                       permissions.ContainsKey(module) && 
                       permissions[module].ContainsKey(feature) && 
                       permissions[module][feature].Contains(action);
            }
            catch
            {
                return false;
            }
        }

        // Add utility method to get current user from HttpContext
        public static User GetCurrentUser(this HttpContext context)
        {
            if (context.Items.TryGetValue("CurrentUser", out var userObj) && userObj is User user)
            {
                return user;
            }
            return null;
        }

        // Add utility method to get user roles from HttpContext
        public static List<Role> GetUserRoles(this HttpContext context)
        {
            if (context.Items.TryGetValue("UserRoles", out var rolesObj) && rolesObj is List<Role> roles)
            {
                return roles;
            }
            return new List<Role>();
        }
    }
}