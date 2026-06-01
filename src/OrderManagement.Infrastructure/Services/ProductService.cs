using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrderManagement.Application.DTOs.Products;
using OrderManagement.Domain.Entities;
using OrderManagement.Infrastructure.Data;

namespace OrderManagement.Application.Services
{
    public class ProductService(AppDbContext _context, ILogger<ProductService> _logger) : IProductService
    {
        public async Task<List<Product>> GetAllAsync() =>
            await _context.Products.AsNoTracking().Include(p => p.Category).ToListAsync();

        public async Task<Product> GetByIdAsync(int id) =>
            await _context.Products.AsNoTracking()
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id)
                ?? throw new KeyNotFoundException($"Product {id} not found");

        public async Task<Product> CreateAsync(CreateProductDTOs newProduct)
        {
            var product = new Product
            {
                Name = newProduct.Name,
                Description = newProduct.Description,
                Price = newProduct.Price,
                Stock = newProduct.Stock,
                CategoryId = newProduct.CategoryId,
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Produit créé — productId {ProductId} | nom {Name}", product.Id, product.Name);

            return product;
        }

        public async Task<Product> UpdateAsync(UpdateProductDTOs updatedProduct, int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id)
                ?? throw new KeyNotFoundException($"Product {id} not found");

            product.Name = updatedProduct.Name;
            product.Description = updatedProduct.Description;
            product.Price = updatedProduct.Price;
            product.Stock = updatedProduct.Stock;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Produit modifié — productId {ProductId} | nouveau stock {Stock}",
            product.Id, product.Stock);

            return product;
        }

        public async Task DeleteAsync(int id)
        {
            var product = await _context.Products.FindAsync(id)
                ?? throw new KeyNotFoundException($"Product {id} not found");

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            _logger.LogWarning("Produit supprimé — productId {ProductId} | nom {Name}",
            id, product.Name);
        }
    }
}
