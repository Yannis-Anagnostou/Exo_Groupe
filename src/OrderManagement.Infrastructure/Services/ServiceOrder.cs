using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrderManagement.Application.DTOs.OrderDto;
using OrderManagement.Application.Exceptions;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Enums;
using OrderManagement.Infrastructure.Data;



namespace OrderManagement.Application.Services.OrderService;

public class ServiceOrder : IOrderService
{
    private readonly AppDbContext _context;
    private readonly ILogger<ServiceOrder> _logger;

    public ServiceOrder(AppDbContext context, ILogger<ServiceOrder>logger)
    {
        _context = context;
        _logger = logger;
    }
    public async Task<OrderResponseDto> CreateOrderAsync(CreateOrderDto dto, int userId)
     
    {
        // ── Validation : commande non vide ────────────────────────────────────
        if (dto.Items == null || dto.Items.Count == 0)
        {
            _logger.LogWarning("Commande vide — userId {UserId}", userId);
        throw new BadRequestException("Une commande doit contenir au moins un article.");


        }
        // ── Création de la commande ───────────────────────────────────────────
        _logger.LogInformation("Création commande — userId {UserId}", userId);
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
            if (product == null) { 
                _logger.LogWarning("Produit introuvable — productId {ProductId}", item.ProductId);
            throw new BadRequestException($"Le produit avec l'ID {item.ProductId} n'existe pas.");
            }

            // 2. Vérifier le stock disponible
            if (item.Quantity > product.Stock)
            {
                _logger.LogWarning("Stock insuffisant — productId {ProductId} | stock {Stock} | demandé {Quantity}",
                  product.Id, product.Stock, item.Quantity);
                throw new BadRequestException($"Stock insuffisant pour le produit '{product.Name}'. " +
                                              $"Disponible : {product.Stock}, demandé : {item.Quantity}.");

            }

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

        _logger.LogInformation("Commande créée — orderId {OrderId} | total {TotalAmount}€", order.Id, order.TotalAmount);


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
       
        if (isAdmin)
        {
            _logger.LogInformation("[Admin] Récupération toutes les commandes");
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
            _logger.LogInformation("[User] Récupération commandes — userId {UserId}", userId);
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
            _logger.LogWarning("Commande introuvable — orderId {OrderId}", id);
            throw new NotFoundException($"La commande avec l'ID {id} n'existe pas.");
        }

        if (!isAdmin && order.UserId != userId)
        {
            _logger.LogWarning("[ACCÈS REFUSÉ] userId {ID} → orderId {OrderId}", userId, id);
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