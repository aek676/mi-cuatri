using System;
using MongoDB.Bson.Serialization.Attributes;

namespace backend.Models
{
    /// <summary>
    /// Represents a Google account linked to a user.
    /// Stored as a subdocument inside the User document.
    /// </summary>
    public class GoogleAccount
    {
        [BsonElement("googleId")]
        public string? GoogleId { get; set; }

        [BsonElement("email")]
        public string? Email { get; set; }

        [BsonElement("refreshToken")]
        public string? RefreshToken { get; set; }

        [BsonElement("accessToken")]
        public string? AccessToken { get; set; }

        [BsonElement("accessTokenExpiry")]
        public DateTime? AccessTokenExpiry { get; set; }

        [BsonElement("scopes")]
        public string[]? Scopes { get; set; }
    }
}
