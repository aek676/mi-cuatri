using System.ComponentModel.DataAnnotations;

namespace backend.Dtos
{
    /// <summary>
    /// Response returned by the Google connect endpoint containing the authorization URL and state token.
    /// </summary>
    public class GoogleConnectResponse
    {
        /// <summary>Authorization URL for the frontend to redirect the user to Google OAuth2 consent screen.</summary>
        [Required]
        public required Uri Url { get; set; }
        /// <summary>State token that identifies the originating session for the OAuth flow.</summary>
        [Required]
        public required string StateToken { get; set; }
    }
}