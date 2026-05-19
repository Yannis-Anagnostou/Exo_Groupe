using System.ComponentModel.DataAnnotations;

namespace OrderManagement.Application.DTOs.Categories;

public class UpdateCategoryDto
{
    [Required(ErrorMessage = "Le nom de la catégorie est obligatoire.")]
    [StringLength(150, ErrorMessage = "Le nom de la catégorie ne peut pas dépasser 150 caractères.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "La description ne peut pas dépasser 500 caractères.")]
    public string? Description { get; set; }
}
