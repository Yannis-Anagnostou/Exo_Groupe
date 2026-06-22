using OrderManagement.Application.DTOs.Categories;
using OrderManagement.Infrastructure.Services;
using Tests.Helpers;
using Xunit;


namespace Tests
{
    public class CategoryServiceTests
    {
        [Fact]
        public async Task CreateAsync_Should_Create_Category()
        {
            // Arrange
            await using var context = TestDbContextFactory.Create();
            var service = new CategoryService(context);

            var dto = new CreateCategoryDto
            {
                Name = "Informatique",
                Description = "Produits informatiques"
            };

            // Act
            var result = await service.CreateAsync(dto);

            // Assert
            Assert.NotEqual(0, result.Id);
            Assert.Equal("Informatique", result.Name);
            Assert.Equal("Produits informatiques", result.Description);
            Assert.Single(context.Categories);
        }
    }
}
