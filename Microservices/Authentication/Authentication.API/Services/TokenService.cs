using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace YourNamespace.Services
{
    public class TokenService
    {
        private const string SecretKey = "d8636f4959a825abbf7a5d5d0be8088126f965a086b9d425485bd70737d06b6636ee3be7ae52f942e074b381f8e2c6c6cbf22578195416f80fe7f9540754a0326d854b55852f283aa706b4209cdee0cb848309e509779cd5e74ab5cee3e1c22b0d57efdd36773e17c87ca10fa9ecec47d85aa3588a283101b7c352c89121b89c"; // Replace with a strong secret key
        private readonly string _issuer = "localhost";
        private readonly string _audience = "localhost";

        public string GenerateToken(string username)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iss, _issuer), // Explicitly set issuer
                new Claim(JwtRegisteredClaimNames.Aud, _audience) // Explicitly set audience
            };

            var token = new JwtSecurityToken(
                _issuer,
                _audience,
                claims,
                expires: DateTime.UtcNow.AddHours(1), // Token expiration time
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
