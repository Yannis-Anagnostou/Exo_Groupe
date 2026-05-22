namespace OrderManagement.Application.Services.OrderService;

public interface IOrderService
{
    Task<OrderResponseDto> CreateOrderAsync(CreateOrderDto dto, int userId);
    Task<List<OrderResponseDto>> GetAllOrdersAsync(int userId, bool isAdmin);
    Task<OrderResponseDto> GetOrderByIdAsync(int id, int userId, bool Admin);
}