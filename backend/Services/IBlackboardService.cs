using backend.Dtos;

namespace backend.Services
{
    /// <summary>
    /// Interface for Blackboard authentication and data retrieval services.
    /// </summary>
    public interface IBlackboardService
    {
        /// <summary>
        /// Authenticates a user with Blackboard credentials.
        /// </summary>
        /// <param name="username">The user's username.</param>
        /// <param name="password">The user's password.</param>
        /// <returns>A login response containing authentication status and session cookie if successful.</returns>
        Task<LoginResponseDto> AuthenticateAsync(string username, string password);
        /// <summary>
        /// Retrieves user data from Blackboard using a session cookie.
        /// </summary>
        /// <param name="sessionCookie">The session cookie from a successful authentication.</param>
        /// <returns>A user response containing user information if successful.</returns>
        Task<UserResponseDto> GetUserDataAsync(string sessionCookie);
        /// <summary>
        /// Retrieves a proxied image response from Blackboard.
        /// </summary>
        /// <param name="sessionCookie">The session cookie for authentication.</param>
        /// <param name="imageUrl">The URL of the image to proxy.</param>
        /// <param name="acceptHeader">The optional Accept header for the image request.</param>
        /// <returns>The proxied HTTP response message or null if unsuccessful.</returns>
        Task<HttpResponseMessage?> GetProxiedImageResponseAsync(string sessionCookie, string imageUrl, string? acceptHeader = null);

        /// <summary>
        /// Retrieves calendar items within the calculated range using Blackboard public calendar API.
        /// </summary>
        /// <param name="currentDate">Reference date to calculate the start of month and 16-week window.</param>
        /// <param name="sessionCookie">The session cookie for authentication.</param>
        /// <returns>Collection of mapped calendar items.</returns>
        Task<IEnumerable<CalendarItemDto>> GetCalendarItemsAsync(DateTime currentDate, string sessionCookie);
    }
}