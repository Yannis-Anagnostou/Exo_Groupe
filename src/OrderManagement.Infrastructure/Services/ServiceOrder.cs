using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.DTOs.OrderDto;
using OrderManagement.Application.Exceptions;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Enums;
using OrderManagement.Infrastructure.Data;



namespace OrderManagement.Application.Services.OrderService;

public class ServiceOrder : IOrderService
{
    private readonly AppDbContext _context;

    public ServiceOrder(AppDbContext context)
    {
        _context = context;
    }
    public async Task<OrderResponseDto> CreateOrderAsync(CreateOrderDto dto, int userId)
    {
        // ── Validation : commande non vide ────────────────────────────────────
        if (dto.Items == null || dto.Items.Count == 0)
            throw new BadRequestException("Une commande doit contenir au moins un article.");

        // ── Création de la commande ───────────────────────────────────────────
        Order order = new Order
        {
            UserId = userId,
            OrderDate = DateTime.UtcNow,
            Status = OrderStatus.Pending,
            TotalAmount = 0
        };

        _context.Orders.Add(order);

        // ── Traitement de chaque ligne de commande ────────────────────────────
        decimal totalAmount = 0;

        foreach (OrderItemDto item in dto.Items)
        {
            // 1. Vérifier que le produit existe
            Product? product = await _context.Products.FindAsync(item.ProductId);
            if (product == null)
                throw new BadRequestException($"Le produit avec l'ID {item.ProductId} n'existe pas.");

            // 2. Vérifier le stock disponible
            if (item.Quantity > product.Stock)
                throw new BadRequestException($"Stock insuffisant pour le produit '{product.Name}'. " +
                                              $"Disponible : {product.Stock}, demandé : {item.Quantity}.");

            // 3. Créer la ligne de commande
            OrderItem orderItem = new OrderItem
            {
                Order = order,
                ProductId = product.Id,
                Quantity = item.Quantity,
                UnitPrice = product.Price,
                TotalPrice = product.Price * item.Quantity
            };

            _context.OrderItems.Add(orderItem);

            // 4. Calculer le total
            totalAmount += orderItem.TotalPrice;

            // 5. Décrémenter le stock
            product.Stock -= item.Quantity;
        }

        // ── Mise à jour du total de la commande ───────────────────────────────
        order.TotalAmount = totalAmount;

        await _context.SaveChangesAsync();

        return new OrderResponseDto
        {
            Id = order.Id,
            OrderDate = order.OrderDate,
            Status = order.Status,
            TotalAmount = order.TotalAmount,
        };
    }
    public async Task<List<OrderResponseDto>> GetAllOrdersAsync(int userId, bool isAdmin)
    {
        if (isAdmin==true)
        {
            List<Order> orders = await _context.Orders.Include(o => o.User).ToListAsync();
            return orders.Select(o => new OrderResponseDto
            {
                Id = o.Id,
                OrderDate = o.OrderDate,
                Status = o.Status,
                TotalAmount = o.TotalAmount,
                UserId = o.UserId,
             

            }).ToList();
        }
        else
        {
            List<Order> orders = await _context.Orders.Include(o => o.User).Where(o => o.UserId == userId).ToListAsync();
            return orders.Select(o => new OrderResponseDto
            {
                Id = o.Id,
                OrderDate = o.OrderDate,
                Status = o.Status,
                TotalAmount = o.TotalAmount,
                UserId = o.UserId,
              
            }).ToList();
        }   
    }

    public async Task<OrderResponseDto> GetOrderByIdAsync(int id, int userId, bool isAdmin)
    {
        var order = await _context.Orders
            .Include(o => o.User)
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
        {
            throw new NotFoundException($"La commande avec l'ID {id} n'existe pas.");
        }

        if (!isAdmin && order.UserId != userId)
        {
            throw new BadRequestException("Vous n'avez pas l'autorisation de voir cette commande.");
        }

        OrderResponseDto responseDto = new OrderResponseDto
        {
            Id = order.Id,
            OrderDate = order.OrderDate,
            Status = order.Status,
            TotalAmount = order.TotalAmount,
        };

        return responseDto;

    }
}