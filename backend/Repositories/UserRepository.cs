using MongoDB.Driver;
using backend.Data;
using backend.Models;

namespace backend.Repositories
{
    /// <summary>
    /// Repository implementation for user persistence.
    /// </summary>
    public class UserRepository : IUserRepository
    {
        private readonly MongoDbContext _context;

        /// <summary>
        /// Initializes a new instance of the UserRepository class and ensures the username index.
        /// </summary>
        /// <param name="context">The MongoDB context.</param>
        public UserRepository(MongoDbContext context)
        {
            _context = context;

            // Ensure unique index on Username to avoid duplicates
            var indexKeys = Builders<User>.IndexKeys.Ascending(u => u.Username);
            var indexOptions = new CreateIndexOptions { Unique = true };
            var indexModel = new CreateIndexModel<User>(indexKeys, indexOptions);
            _context.Users.Indexes.CreateOne(indexModel);
        }

        /// <summary>
        /// Upserts a document that contains only the username.
        /// </summary>
        /// <param name="username">The username to upsert.</param>
        public Task UpsertByUsernameAsync(string username)
        {
            var filter = Builders<User>.Filter.Eq(u => u.Username, username);
            var update = Builders<User>.Update.Set(u => u.Username, username);
            var options = new UpdateOptions { IsUpsert = true };
            return _context.Users.UpdateOneAsync(filter, update, options);
        }
    }
}
