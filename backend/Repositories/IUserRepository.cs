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

        /// <summary>
        /// Retrieves all events for a user.
        /// </summary>
        /// <param name="username">The username of the user.</param>
        Task<List<Event>> GetUserEventsAsync(string username);

        /// <summary>
        /// Retrieves a specific event by ID for a user.
        /// </summary>
        /// <param name="username">The username of the user.</param>
        /// <param name="eventId">The ID of the event.</param>
        Task<Event?> GetEventByIdAsync(string username, string eventId);

        /// <summary>
        /// Adds a new event to a user's events list.
        /// </summary>
        /// <param name="username">The username of the user.</param>
        /// <param name="evt">The event to add.</param>
        Task<Event> AddEventAsync(string username, Event evt);

        /// <summary>
        /// Updates an existing event in a user's events list.
        /// </summary>
        /// <param name="username">The username of the user.</param>
        /// <param name="evt">The event with updated information.</param>
        /// <returns>True if the event was updated, false if not found.</returns>
        Task<bool> UpdateEventAsync(string username, Event evt);

        /// <summary>
        /// Deletes an event from a user's events list.
        /// </summary>
        /// <param name="username">The username of the user.</param>
        /// <param name="eventId">The ID of the event to delete.</param>
        Task DeleteEventAsync(string username, string eventId);
    }
}
