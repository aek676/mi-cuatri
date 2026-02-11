using backend.Data;
using backend.Models;
using backend.Services;
using MongoDB.Driver;

namespace backend.Repositories
{
    /// <summary>
    /// Repository implementation for user persistence.
    /// </summary>
    public class UserRepository : IUserRepository
    {
        private readonly MongoDbContext _context;

        private readonly ITokenProtector _protector;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserRepository"/> class and ensures the username index.
        /// </summary>
        /// <param name="context">The MongoDB context.</param>
        /// <param name="protector">Token protector for encrypting/decrypting stored Google tokens.</param>
        public UserRepository(MongoDbContext context, ITokenProtector protector)
        {
            _context = context;
            _protector = protector;

            // Ensure unique index on Username to avoid duplicates
            var indexKeys = Builders<User>.IndexKeys.Ascending(u => u.Username);
            var indexOptions = new CreateIndexOptions { Unique = true };
            var indexModel = new CreateIndexModel<User>(indexKeys, indexOptions);
            _context.Users.Indexes.CreateOne(indexModel);
        }

        /// <summary>
        /// Upserts a document that contains the username and optional email.
        /// </summary>
        /// <param name="username">The username to upsert.</param>
        /// <param name="email">Optional email to associate with the username (from Blackboard).</param>
        public Task UpsertByUsernameAsync(string username, string? email = null)
        {
            var filter = Builders<User>.Filter.Eq(u => u.Username, username);
            var update = Builders<User>.Update.Set(u => u.Username, username);
            if (!string.IsNullOrEmpty(email))
            {
                update = update.Set(u => u.Email, email);
            }
            var options = new UpdateOptions { IsUpsert = true };
            return _context.Users.UpdateOneAsync(filter, update, options);
        }

        /// <summary>
        /// Finds a user document by username.
        /// </summary>
        /// <param name="username">Username to find.</param>
        /// <returns>User document or null if not found.</returns>
        public async Task<User?> GetByUsernameAsync(string username)
        {
            var filter = Builders<User>.Filter.Eq(u => u.Username, username);
            var user = await _context.Users.Find(filter).FirstOrDefaultAsync();
            if (user?.GoogleAccount != null)
            {
                user.GoogleAccount.RefreshToken = _protector.Unprotect(
                    user.GoogleAccount.RefreshToken
                );
                user.GoogleAccount.AccessToken = _protector.Unprotect(
                    user.GoogleAccount.AccessToken
                );
            }
            return user;
        }

        /// <summary>
        /// Finds a user document by email.
        /// </summary>
        /// <param name="email">Email address to look up.</param>
        /// <returns>User document or null if not found.</returns>
        public async Task<User?> GetByEmailAsync(string email)
        {
            var filter = Builders<User>.Filter.Eq(u => u.Email, email);
            var user = await _context.Users.Find(filter).FirstOrDefaultAsync();
            if (user?.GoogleAccount != null)
            {
                user.GoogleAccount.RefreshToken = _protector.Unprotect(
                    user.GoogleAccount.RefreshToken
                );
                user.GoogleAccount.AccessToken = _protector.Unprotect(
                    user.GoogleAccount.AccessToken
                );
            }
            return user;
        }

        /// <summary>
        /// Upserts the Google account subdocument for the specified username.
        /// </summary>
        /// <param name="username">Username whose Google account will be set.</param>
        /// <param name="account">GoogleAccount object to persist (tokens are protected before saving).</param>
        public Task UpsertGoogleAccountAsync(string username, GoogleAccount account)
        {
            GoogleAccount accountToSave = account;
            if (account != null)
            {
                accountToSave = new GoogleAccount
                {
                    GoogleId = account.GoogleId,
                    Email = account.Email,
                    RefreshToken = _protector.Protect(account.RefreshToken),
                    AccessToken = _protector.Protect(account.AccessToken),
                    AccessTokenExpiry = account.AccessTokenExpiry,
                    Scopes = account.Scopes,
                };
            }

            var filter = Builders<User>.Filter.Eq(u => u.Username, username);
            var update = Builders<User>.Update.Set(u => u.GoogleAccount, accountToSave);
            var options = new UpdateOptions { IsUpsert = false };
            return _context.Users.UpdateOneAsync(filter, update, options);
        }

        /// <summary>
        /// Removes the Google account linkage for a user.
        /// </summary>
        /// <param name="username">Username to remove the Google account for.</param>
        public Task RemoveGoogleAccountAsync(string username)
        {
            var filter = Builders<User>.Filter.Eq(u => u.Username, username);
            var update = Builders<User>.Update.Unset(u => u.GoogleAccount);
            return _context.Users.UpdateOneAsync(filter, update);
        }

        /// <summary>
        /// Retrieves all events for a user.
        /// </summary>
        /// <param name="username">The username of the user.</param>
        public async Task<List<Event>> GetUserEventsAsync(string username)
        {
            var filter = Builders<User>.Filter.Eq(u => u.Username, username);
            var user = await _context.Users.Find(filter).FirstOrDefaultAsync();
            return user?.Events ?? new List<Event>();
        }

        /// <summary>
        /// Retrieves a specific event by ID for a user.
        /// </summary>
        /// <param name="username">The username of the user.</param>
        /// <param name="eventId">The ID of the event.</param>
        public async Task<Event?> GetEventByIdAsync(string username, string eventId)
        {
            var filter = Builders<User>.Filter.Eq(u => u.Username, username);
            var user = await _context.Users.Find(filter).FirstOrDefaultAsync();
            return user?.Events?.FirstOrDefault(e => e.Id == eventId);
        }

        /// <summary>
        /// Adds a new event to a user's events list using $push.
        /// </summary>
        /// <param name="username">The username of the user.</param>
        /// <param name="evt">The event to add.</param>
        public async Task<Event> AddEventAsync(string username, Event evt)
        {
            var filter = Builders<User>.Filter.Eq(u => u.Username, username);
            var update = Builders<User>.Update.Push(u => u.Events, evt);
            await _context.Users.UpdateOneAsync(filter, update);
            return evt;
        }

        /// <summary>
        /// Updates an existing event in a user's events list using positional operator.
        /// </summary>
        /// <param name="username">The username of the user.</param>
        /// <param name="evt">The event with updated information.</param>
        public async Task<bool> UpdateEventAsync(string username, Event evt)
        {
            var filter = Builders<User>.Filter.And(
                Builders<User>.Filter.Eq(u => u.Username, username),
                Builders<User>.Filter.ElemMatch(u => u.Events, e => e.Id == evt.Id)
            );

            var update = Builders<User>
                .Update.Set("Events.$.Title", evt.Title)
                .Set("Events.$.Subject", evt.Subject)
                .Set("Events.$.Start", evt.Start)
                .Set("Events.$.End", evt.End)
                .Set("Events.$.Location", evt.Location)
                .Set("Events.$.Color", evt.Color);

            var result = await _context.Users.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }

        /// <summary>
        /// Deletes an event from a user's events list using $pull.
        /// </summary>
        /// <param name="username">The username of the user.</param>
        /// <param name="eventId">The ID of the event to delete.</param>
        public Task DeleteEventAsync(string username, string eventId)
        {
            var filter = Builders<User>.Filter.Eq(u => u.Username, username);
            var update = Builders<User>.Update.PullFilter(u => u.Events, e => e.Id == eventId);
            return _context.Users.UpdateOneAsync(filter, update);
        }
    }
}
