using Microsoft.AspNetCore.Mvc;
using backend.Models;
using backend.Repositories;
using backend.DTOs;

namespace backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly IProductRepository _productRepository;
        public ProductsController(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var products = await _productRepository.GetAllProductsAsync();

            var productDto = products.Select(p => new ProductDto(p.Id, p.Name, p.Price, p.Quantity));

            return Ok(productDto);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var product = await _productRepository.GetProductByIdAsync(id);
            if (product == null) return NotFound();

            var productDto = new ProductDto(product.Id, product.Name, product.Price, product.Quantity);

            return Ok(productDto);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateProductDto createDto)
        {
            var newProduct = new Product
            {
                Name = createDto.Name,
                Price = createDto.Price,
                Quantity = createDto.Quantity
            };

            await _productRepository.AddProductAsync(newProduct);

            var resultDto = new ProductDto(
                newProduct.Id,
                newProduct.Name,
                newProduct.Price,
                newProduct.Quantity
            );

            return CreatedAtAction(nameof(GetById), new { id = newProduct.Id }, resultDto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateProductDto updateDto)
        {
            var existingProduct = await _productRepository.GetProductByIdAsync(id);

            if (existingProduct == null) return NotFound();

            existingProduct.Name = updateDto.Name;
            existingProduct.Price = updateDto.Price;
            existingProduct.Quantity = updateDto.Quantity;

            await _productRepository.UpdateProductAsync(existingProduct);

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var existingProduct = await _productRepository.GetProductByIdAsync(id);

            if (existingProduct == null) return NotFound();

            await _productRepository.DeleteProductAsync(id);

            return NoContent();
        }
    }
}