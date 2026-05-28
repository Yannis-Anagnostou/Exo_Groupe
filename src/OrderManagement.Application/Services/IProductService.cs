using OrderManagement.Application.DTOs.Products;
using OrderManagement.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Application.Services
{
    public interface IProductService
    {
        Task<List<Product>> GetAllAsync();

        Task<Product> GetByIdAsync(int id);

        Task<Product> CreateAsync(CreateProductDTOs newProduct);

        Task<Product> UpdateAsync(UpdateProductDTOs updatedProduct, int id);

        Task Delete(int id);
    }
}
