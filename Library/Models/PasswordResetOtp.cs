using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace YourNamespace.Models
{
    public class PasswordResetOtp
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public string Email { get; set; }
        public string Otp { get; set; }
        public DateTime Expiry { get; set; }
        public bool Used { get; set; } = false;
    }
} 