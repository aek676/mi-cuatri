using MongoDB.Driver;
using backend.Models;
using backend.Data;

namespace backend.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly MongoDbContext _context;

        public ProductRepository(MongoDbContext context)
        {
            _context = context;
        }
        public async Task<IEnumerable<Product>> GetAllProductsAsync()
        {
            return await _context.Products.Find(_ => true).ToListAsync();
        }
        public async Task<Product> GetProductByIdAsync(string id)
        {
            return await _context.Products.Find(p => p.Id == id).FirstOrDefaultAsync();
        }

        public Task AddProductAsync(Product product)
        {
            return _context.Products.InsertOneAsync(product);
        }
        public Task UpdateProductAsync(Product product)
        {
            return _context.Products.ReplaceOneAsync(p => p.Id == product.Id, product);
        }
        public Task DeleteProductAsync(string id)
        {
            return _context.Products.DeleteOneAsync(p => p.Id == id);
        }
    }
}
