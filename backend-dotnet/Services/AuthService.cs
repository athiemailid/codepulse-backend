using backend_dotnet.Models;
using Microsoft.Azure.Cosmos;
using System.Threading.Tasks;
using System.Linq;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System;
using System.Collections.Generic;
using BCrypt.Net;

namespace backend_dotnet.Services
{
    public class AuthService
    {
        private readonly Container _container;
        private readonly string _jwtSecret;
        private readonly int _jwtExpiryMinutes;

        public AuthService(CosmosClient cosmosClient, string dbName, string containerName, string jwtSecret, int jwtExpiryMinutes = 60)
        {
            _container = cosmosClient.GetContainer(dbName, containerName);
            _jwtSecret = jwtSecret;
            _jwtExpiryMinutes = jwtExpiryMinutes;
        }

        public async Task<backend_dotnet.Models.User> RegisterAsync(string username, string password, List<string> roles)
        {
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
            var user = new backend_dotnet.Models.User
            {
                Id = Guid.NewGuid().ToString(),
                Username = username,
                PasswordHash = passwordHash,
                Roles = roles
            };
            await _container.CreateItemAsync(user, new PartitionKey(user.Id));
            return user;
        }

        public async Task<backend_dotnet.Models.User> GetByUsernameAsync(string username)
        {
            var query = _container.GetItemQueryIterator<backend_dotnet.Models.User>($"SELECT * FROM c WHERE c.Username = '{username}'");
            while (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();
                var user = response.FirstOrDefault();
                if (user != null) return user;
            }
            return null;
        }

        public async Task<string> LoginAsync(string username, string password)
        {
            var user = await GetByUsernameAsync(username);
            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                return null;
            return GenerateJwtToken(user);
        }

        public string GenerateJwtToken(backend_dotnet.Models.User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id ?? string.Empty),
                new Claim(ClaimTypes.Name, user.Username ?? string.Empty)
            };
            foreach (var role in user.Roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwtExpiryMinutes),
                signingCredentials: creds
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
