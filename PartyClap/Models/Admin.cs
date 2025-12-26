using System;

namespace PartyClap.Models
{
    public class Admin
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Email { get; set; }
        public string PasswordHash { get; set; }
    }
}
