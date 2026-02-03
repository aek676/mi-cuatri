using System.ComponentModel.DataAnnotations;

namespace backend.Dtos
{
    /// <summary>
    /// Represents whether a user has a Google account connected and optionally the email associated with it.
    /// </summary>
    public class GoogleStatusDto
    {
        /// <summary>Whether the user has a Google account connected.</summary>
        [Required]
        public required bool IsConnected { get; set; }
        /// <summary>Email address of the connected Google account, if available.</summary>
        public string? Email { get; set; }
    }
}
