using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.DTOs.Categories;
using OrderManagement.Application.Exceptions;
using OrderManagement.Application.Services;
using OrderManagement.Domain.Entities;
using OrderManagement.Infrastructure.Data;

namespace OrderManagement.Infrastructure.Services;

public class CategoryService : ICategoryService
{
    private readonly AppDbContext _context;

    public CategoryService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<CategoryDto>> GetAllAsync()
    {
        return await _context.Categories
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description
            })
            .ToListAsync();
    }

    public async Task<CategoryDto> GetByIdAsync(int id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null)
        {
            throw new NotFoundException($"La catégorie avec l'ID {id} n'existe pas.");
        }

        return new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description
        };
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryDto dto)
    {
        var category = new Category
        {
            Name = dto.Name,
            Description = dto.Description
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        return new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description
        };
    }

    public async Task<CategoryDto> UpdateAsync(int id, UpdateCategoryDto dto)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null)
        {
            throw new NotFoundException($"La catégorie avec l'ID {id} n'existe pas.");
        }

        category.Name = dto.Name;
        category.Description = dto.Description;

        await _context.SaveChangesAsync();

        return new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description
        };
    }

    public async Task DeleteAsync(int id)
    {
        var category = await _context.Categories
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category == null)
        {
            throw new NotFoundException($"La catégorie avec l'ID {id} n'existe pas.");
        }

        // Règle métier : Ne peut pas être supprimée si elle contient encore des produits
        if (category.Products.Any())
        {
            throw new BadRequestException("Une catégorie ne peut pas être supprimée si elle contient encore des produits.");
        }

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();
    }
}
