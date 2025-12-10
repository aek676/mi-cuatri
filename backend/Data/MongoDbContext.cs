using MongoDB.Driver;
using backend.Models;

namespace backend.Data
{
    public class MongoDbContext
    {
        private readonly IMongoDatabase _database;
        public MongoDbContext(IConfiguration configuration)
        {
            var client = new MongoClient(configuration.GetConnectionString("MongoDB"));
            _database = client.GetDatabase("MiCuatriDatabase");
        }
        public IMongoCollection<Product> Products => _database.GetCollection<Product>("Products");
    }
}