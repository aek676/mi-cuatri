using MongoDB.Driver;
using backend.Models;

namespace backend.Data
{
    /// <summary>
    /// MongoDB database context for data access operations.
    /// </summary>
    public class MongoDbContext
    {
        private readonly IMongoDatabase _database;
        /// <summary>
        /// Initializes a new instance of the MongoDbContext class.
        /// </summary>
        /// <param name="configuration">The application configuration containing database connection strings.</param>
        public MongoDbContext(IConfiguration configuration)
        {
            var client = new MongoClient(configuration.GetConnectionString("MongoDB"));
            _database = client.GetDatabase("MiCuatriDatabase");
        }
        /// <summary>
        /// Gets the products collection from the MongoDB database.
        /// </summary>
        public IMongoCollection<Product> Products => _database.GetCollection<Product>("Products");
        /// <summary>
        /// Gets the users collection from the MongoDB database.
        /// </summary>
        public IMongoCollection<User> Users => _database.GetCollection<User>("Users");
    }
}