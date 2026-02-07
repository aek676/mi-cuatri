using MongoDB.Driver;
using backend.Data;
using backend.Models;
using backend.Services;

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
                user.GoogleAccount.RefreshToken = _protector.Unprotect(user.GoogleAccount.RefreshToken);
                user.GoogleAccount.AccessToken = _protector.Unprotect(user.GoogleAccount.AccessToken);
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
                user.GoogleAccount.RefreshToken = _protector.Unprotect(user.GoogleAccount.RefreshToken);
                user.GoogleAccount.AccessToken = _protector.Unprotect(user.GoogleAccount.AccessToken);
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
                    Scopes = account.Scopes
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
    }
}
