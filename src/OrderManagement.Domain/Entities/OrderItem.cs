namespace OrderManagement.Domain.Entities;

public class OrderItem
{
    public int Id { get; set; }

    // FK
    public int OrderId { get; set; }

    // FK
    public int ProductId { get; set; }

    /// <summary>
    /// Quantité commandée — doit être supérieure à 0.
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Prix unitaire au moment de la commande — copié depuis Product.Price par le backend.
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Total de la ligne = UnitPrice * Quantity — calculé par le backend.
    /// </summary>
    public decimal TotalPrice { get; set; }

    // Navigation
    public Order Order { get; set; } = null!;

    public Product Product { get; set; } = null!;
}
