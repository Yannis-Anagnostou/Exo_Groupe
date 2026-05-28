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

        if (dto.Items == null || dto.Items.Count == 0)
        {
            throw new BadRequestException("Une commande doit contenir au moins un article.");

        }
       Order o= new Order();

       {
                o.UserId = userId;
                o.OrderDate = DateTime.Now;
                o.Status = OrderStatus.Pending; 
                o.TotalAmount = 0;
       }
       
       decimal totalAmount = 0;
       foreach (var item in dto.Items)
       {
           Product? product = await _context.Products.FindAsync(item.ProductId);
           if (product == null)
           {
               throw new BadRequestException($"Le produit avec l'ID {item.ProductId} n'existe pas.");
           }
           totalAmount += product.Price * item.Quantity;
       }
       o.TotalAmount = totalAmount;
       _context.Orders.Add(o);
       await _context.SaveChangesAsync();
       return new OrderResponseDto
       {
           Id = o.Id,
           OrderDate = o.OrderDate,
           Status = o.Status,
           TotalAmount = o.TotalAmount,
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

        Order? order = await _context.Orders.Include(o => o.User).FirstOrDefaultAsync(o => o.Id == id);
        if  (isAdmin== true || order.UserId == userId)
        {
        if (order == null)
        {
            throw new BadRequestException($"La commande avec l'ID {id} n'existe pas.");
        }
            return new OrderResponseDto
            {
                Id = order.Id,
                OrderDate = order.OrderDate,
                Status = order.Status,
                TotalAmount = order.TotalAmount,
                UserId = order.UserId,
                
            };
        };

        throw new BadRequestException("Vous n'avez pas l'autorisation de voir cette commande.");
        


      
    }
}