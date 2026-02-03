using System;
using MongoDB.Bson.Serialization.Attributes;

namespace backend.Models
{
    /// <summary>
    /// Represents a Google account linked to a user.
    /// Stored as a subdocument inside the User document.
    /// </summary>
    /// <summary>
    /// Represents a Google account linked to a user.
    /// Stored as a subdocument inside the User document.
    /// </summary>
    public class GoogleAccount
    {
        /// <summary>The unique Google account identifier (sub).</summary>
        [BsonElement("googleId")]
        public string? GoogleId { get; set; }

        /// <summary>Email address associated with the Google account.</summary>
        [BsonElement("email")]
        public string? Email { get; set; }

        /// <summary>OAuth2 refresh token used to obtain new access tokens.</summary>
        [BsonElement("refreshToken")]
        public string? RefreshToken { get; set; }

        /// <summary>Currently cached access token if available.</summary>
        [BsonElement("accessToken")]
        public string? AccessToken { get; set; }

        /// <summary>Expiration time of the cached access token (UTC).</summary>
        [BsonElement("accessTokenExpiry")]
        public DateTime? AccessTokenExpiry { get; set; }

        /// <summary>OAuth scopes granted for this account.</summary>
        [BsonElement("scopes")]
        public string[]? Scopes { get; set; }
    }
}
