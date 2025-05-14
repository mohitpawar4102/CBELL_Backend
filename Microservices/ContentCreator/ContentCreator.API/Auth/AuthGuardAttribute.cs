using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace YourApiMicroservice.Auth
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class AuthGuardAttribute : Attribute, IAsyncAuthorizationFilter
    {
        private readonly string _module;
        private readonly string _feature;
        private readonly string _action;
        
        public AuthGuardAttribute(string module, string feature, string action)
        {
            _module = module;
            _feature = feature;
            _action = action;
        }
        
        public Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            // Skip for anonymous actions
            if (context.ActionDescriptor.EndpointMetadata.Any(em => em is AllowAnonymousAttribute))
            {
                return Task.CompletedTask;
            }
            
            // Check if user is authenticated
            if (!context.HttpContext.User.Identity.IsAuthenticated)
            {
                context.Result = new JsonResult(new { 
                    success = false, 
                    message = "You must be logged in to access this resource." 
                })
                { 
                    StatusCode = 401 
                };
                return Task.CompletedTask;
            }
            
            // Get user info for better error messages
            var userEmail = context.HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var userName = context.HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
            
            // Use the HasPermission extension method that correctly parses JWT permissions
            bool hasPermission = context.HttpContext.User.HasPermission(_module, _feature, _action);
            
            // For debugging
            Console.WriteLine($"Checking permission: {_module}.{_feature}.{_action} = {hasPermission}");
            
            if (!hasPermission)
            {
                context.Result = new JsonResult(new { 
                    success = false, 
                    message = $"Access denied. You don't have the required '{_action}' permission for {_module}.{_feature}.",
                    details = $"User {userName ?? userEmail ?? "Unknown"} attempted to access a protected resource requiring {_module}.{_feature}.{_action} permission."
                })
                { 
                    StatusCode = 403 
                };
            }
            
            return Task.CompletedTask;
        }
    }
}