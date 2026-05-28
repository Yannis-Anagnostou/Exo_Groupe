using System.ComponentModel.DataAnnotations;

namespace OrderManagement.Application.DTOs.OrderDto;

public class CreateOrderDto
{
    [Required]
    [MinLength(1)]
    public List<OrderItemDto> Items { get; set; } = new();
}

public class OrderItemDto
{

    [Required]
    [Range(1, int.MaxValue)]
    public int ProductId { get; set; }

    [Required]
    [Range(1, 500, ErrorMessage = " La quantité doit être comprise entre 1 et 500.")]
    public int Quantity { get; set; }
}