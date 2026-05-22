namespace OrderManagement.API.Controllers.OrderControllers;

[ApiController]
[Route("api/[controller]")]
public class OrderController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrderController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpPost]
    public async Task<ActionResult<OrderResponseDto>> CreateOrder([FromBody] CreateOrderDto dto, [FromServices] IJwtService jwtService)
    {
        try
        {
            // ✅ Obtenir le UserID depuis le token (authentification)
            var userId = jwtService.GetUserIdFromToken(HttpContext.Request.Headers["Authorization"]);

            // ✅ Créer la commande via le service
            var order = await _orderService.CreateOrderAsync(dto, userId);

            // 201 Created avec location vers la nouvelle ressource
            return CreatedAtAction(nameof(GetOrderById), new { id = order.Id }, order);
        }
        catch (BadRequestException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<ActionResult<List<OrderResponseDto>>> GetAllOrders([FromServices] IJwtService jwtService)
    {
        try
        {
            var userId = jwtService.GetUserIdFromToken(HttpContext.Request.Headers["Authorization"]);

            // Déterminer si c'est un admin
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return BadRequest(new { error = "Utilisateur non trouvé." });
            }

            var isAdmin = user.Role == UserRole.Admin;

            var orders = await _orderService.GetAllOrdersAsync(userId, isAdmin);

            return Ok(orders);
        }
        catch (BadRequestException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Une erreur est survenue lors de la récupération des commandes." });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<OrderResponseDto>> GetOrderById(int id, [FromServices] IJwtService jwtService)
    {
        try
        {
            var userId = jwtService.GetUserIdFromToken(HttpContext.Request.Headers["Authorization"]);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return BadRequest(new { error = "Utilisateur non trouvé." });
            }

            var isAdmin = user.Role == UserRole.Admin;

            var order = await _orderService.GetOrderByIdAsync(id, userId, isAdmin);

            return Ok(order);
        }
        catch (BadRequestException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }
}
