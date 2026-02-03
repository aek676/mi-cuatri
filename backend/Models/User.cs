using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace backend.Models
{
    /// <summary>
    /// Represents a user entity stored in MongoDB.
    /// </summary>
    public class User
    {
        /// <summary>
        /// The unique identifier of the user.
        /// </summary>
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        [Required]
        public string Id { get; set; } = default!;

        /// <summary>
        /// The username to persist.
        /// </summary>
        [BsonElement("username")]
        public required string Username { get; set; }

        /// <summary>
        /// Optional Blackboard email (queried after login) to help correlate sessions.
        /// </summary>
        [BsonElement("email")]
        public string? Email { get; set; }

        /// <summary>
        /// Optional linked Google account data (tokens, ids, scopes).
        /// </summary>
        [BsonElement("googleAccount")]
        public GoogleAccount? GoogleAccount { get; set; }
    }
}
