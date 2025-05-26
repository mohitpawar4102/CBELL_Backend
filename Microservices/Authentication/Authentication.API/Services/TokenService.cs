using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;
using System;
using System.Text.Json;
using YourNamespace.Library.Database;
using MongoDB.Driver;
using YourNamespace.Models;

namespace YourNamespace.Services
{
    public class TokenService
    {
        private readonly IConfiguration _configuration;
        private readonly MongoDbService _mongoDbService; // Add this

        public TokenService(IConfiguration configuration, MongoDbService mongoDbService) // Inject MongoDbService
        {
            _configuration = configuration;
            _mongoDbService = mongoDbService;
        }

        public string GenerateAccessToken(User user)
        {
            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
        new Claim(ClaimTypes.NameIdentifier, user.Id),
        new Claim("org", user.OrganizationId)
    };

            // Get user's roles
            var rolesCollection = _mongoDbService.GetDatabase().GetCollection<Role>("Roles");
            var roles = rolesCollection.Find(r => user.RoleIds.Contains(r.Id) && r.IsActive).ToList();

            // Get module and feature collections
            var modulesCollection = _mongoDbService.GetDatabase().GetCollection<Module>("Modules");
            var featuresCollection = _mongoDbService.GetDatabase().GetCollection<Feature>("Features");
            var permTypesCollection = _mongoDbService.GetDatabase().GetCollection<PermissionType>("PermissionTypes");

            // Get all permission types for bit checking
            var permissionTypes = permTypesCollection.Find(pt => pt.IsActive).ToList();

            // Add roles to claims
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role.Name));
            }

            // Build permissions dictionary for JWT
            var permissions = new Dictionary<string, Dictionary<string, List<string>>>();

            foreach (var role in roles)
            {
                foreach (var permission in role.Permissions)
                {
                    // Get module and feature
                    var module = modulesCollection.Find(m => m.Id == permission.ModuleId).FirstOrDefault();
                    var feature = featuresCollection.Find(f => f.Id == permission.FeatureId).FirstOrDefault();

                    if (module == null || feature == null)
                        continue;

                    if (!permissions.ContainsKey(module.Name))
                    {
                        permissions[module.Name] = new Dictionary<string, List<string>>();
                    }

                    if (!permissions[module.Name].ContainsKey(feature.Name))
                    {
                        permissions[module.Name][feature.Name] = new List<string>();
                    }

                    // Check each permission type bit
                    foreach (var permType in permissionTypes)
                    {
                        bool hasPermission = (permission.PermissionValue & (1 << permType.BitPosition)) != 0;

                        if (hasPermission && !permissions[module.Name][feature.Name].Contains(permType.Name))
                        {
                            permissions[module.Name][feature.Name].Add(permType.Name);
                        }
                    }
                }
            }

            // Add permissions as a serialized claim
            claims.Add(new Claim("permissions", JsonSerializer.Serialize(permissions)));

            // Generate token
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Authentication:Jwt:Secret"]));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Authentication:Jwt:Issuer"],
                audience: _configuration["Authentication:Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(30),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateRefreshToken()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        }
    }
}
