namespace OrderManagement.Application.Services.OrderService;

public class ServiceOrder : IOrderService
{
    private readonly AppDbContext _context;

    public ServiceOrder(AppDbContext context)
    {
        _context = context;
    }
    public Task<OrderResponseDto> CreateOrderAsync(CreateOrderDto dto, int userId)
    {

        if (dto.Items == null || dto.Items.count==0)
        {
            throw new BadRequestException("Une commande doit contenir au moins un article.");
        

       Order o= new Order();
       {  
      UserId=userId,
      OrderDate=DateTime.Now,
      Status=OrderStatus.Pending,
      TotalAmount=0,
       }
       };
       decimal totalAmount = 0;
       foreach (var item in dto.Items)
       {
           Product product = await _context.Products.FindAsync(item.ProductId);
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

    public Task<List<OrderResponseDto>> GetAllOrdersAsync(int userId, bool isAdmin)
    {
        if (isAdmin)
        {
            Order[] orders = await _context.Orders.Include(o => o.User).ToListAsync();
            return orders.Select(o => new OrderResponseDto
            {
                Id = o.Id,
                OrderDate = o.OrderDate,
                Status = o.Status,
                TotalAmount = o.TotalAmount,
                UserId = o.UserId,
                UserName = o.User.Name
            }).ToList();
        }
        else
        {
            Order[] orders = await _context.Orders.Include(o => o.User).Where(o => o.UserId == userId).ToListAsync();
            return orders.Select(o => new OrderResponseDto
            {
                Id = o.Id,
                OrderDate = o.OrderDate,
                Status = o.Status,
                TotalAmount = o.TotalAmount,
                UserId = o.UserId,
                UserName = o.User.Name
            }).ToList();
        }   
    }

    public async Task<OrderResponseDto> GetOrderByIdAsync(int id, int userId, bool isAdmin)
    {
        if  (isAdmin || order.UserId == userId)
        Order order = await _context.Orders.Include(o => o.User).FirstOrDefaultAsync(o => o.Id == id);
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
            UserName = order.User.Name
        };
            
        throw new BadRequestException("Vous n'avez pas l'autorisation de voir cette commande.")
        


      
    }
}