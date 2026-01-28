namespace backend.Repositories
{
    /// <summary>
    /// Interface for user repository operations.
    /// </summary>
    public interface IUserRepository
    {
        /// <summary>
        /// Upserts a user document containing only the username.
        /// </summary>
        /// <param name="username">The username to persist or update.</param>
        Task UpsertByUsernameAsync(string username);
    }
}
