using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.DTOs.Products;
using OrderManagement.Domain.Entities;
using OrderManagement.Infrastructure.Data;

namespace OrderManagement.Application.Services
{
    public class ProductService(AppDbContext _context) : IProductService
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
            return product;
        }

        public async Task DeleteAsync(int id)
        {
            var product = await _context.Products.FindAsync(id)
                ?? throw new KeyNotFoundException($"Product {id} not found");

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
        }
    }
}
