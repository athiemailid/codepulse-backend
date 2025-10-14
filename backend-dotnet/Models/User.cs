using System.Collections.Generic;

namespace backend_dotnet.Models
{
    public class User
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
    }
}
