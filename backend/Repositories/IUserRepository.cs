using backend.Models;

namespace backend.Repositories
{
    /// <summary>
    /// Interface for user repository operations.
    /// </summary>
    public interface IUserRepository
    {
        /// <summary>
        /// Upserts a user document containing the username and optional email.
        /// </summary>
        /// <param name="username">The username to persist or update.</param>
        /// <param name="email">Optional email to store for the user (from Blackboard).</param>
        Task UpsertByUsernameAsync(string username, string? email = null);

        /// <summary>
        /// Retrieves a user by username.
        /// </summary>
        Task<User?> GetByUsernameAsync(string username);

        /// <summary>
        /// Retrieves a user by email.
        /// </summary>
        Task<User?> GetByEmailAsync(string email);

        /// <summary>
        /// Upserts the Google account subdocument for the specified username.
        /// </summary>
        Task UpsertGoogleAccountAsync(string username, GoogleAccount account);

        /// <summary>
        /// Removes the Google account linkage for a user.
        /// </summary>
        Task RemoveGoogleAccountAsync(string username);
    }
}
