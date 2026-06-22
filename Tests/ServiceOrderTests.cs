using OrderManagement.Application.Exceptions;
using OrderManagement.Domain.Entities;
using OrderManagement.Infrastructure.Services;
using System;
using System.Collections.Generic;
using System.Text;
using Tests.Helpers;
using Xunit;

namespace Tests
{
    public class ServiceOrderTests
    {
        [Fact]
        public async Task DeleteAsync_Should_Throw_BadRequest_When_Category_Has_Products()
        {
            // Arrange
            await using var context = TestDbContextFactory.Create();

            var category = new Category
            {
                Name = "Livres",
                Description = "Catégorie avec produits"
            };

            context.Categories.Add(category);
            await context.SaveChangesAsync();

            var product = new Product
            {
                Name = "Clean Code",
                Description = "Livre de programmation",
                Price = 30m,
                Stock = 10,
                CategoryId = category.Id
            };

            context.Products.Add(product);
            await context.SaveChangesAsync();

            var service = new CategoryService(context);

            // Act + Assert
            await Assert.ThrowsAsync<BadRequestException>(() => service.DeleteAsync(category.Id));
        }
    }
}
