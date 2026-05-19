namespace OrderManagement.Domain.Entities;

public class Product
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>
    /// Prix unitaire — doit être supérieur à 0.
    /// </summary>
    public decimal Price { get; set; }

    public int Stock { get; set; } = 0;

    // FK
    public int CategoryId { get; set; }

    // Navigation
    public Category Category { get; set; } = null!;

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
