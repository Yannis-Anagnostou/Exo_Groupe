using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OrderManagement.Application.DTOs.OrderDto;
using OrderManagement.Application.Exceptions;
using OrderManagement.Application.Services;
using OrderManagement.Application.Services.OrderService;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Enums;
using OrderManagement.Infrastructure.Data;
using OrderManagement.Infrastructure.Services;
using System.Security.Claims;

namespace OrderManagement.API.Controllers.OrderControllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrderController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly IClaimService _claimService;
    

    // Un seul constructeur avec tout dedans
    public OrderController(IOrderService orderService, IClaimService claimService)
    {
        _orderService = orderService;
        _claimService = claimService;

    }


    [HttpPost]
    [EnableRateLimiting(RateLimitPolicies.Add)]
    public async Task<ActionResult<OrderResponseDto>> CreateOrder([FromBody] CreateOrderDto dto)
    {
        
        Claim userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);

        if (userIdClaim == null)
            return Unauthorized(new { error = "Token invalide ou manquant." });

        int userId = int.Parse(userIdClaim.Value);

        

        var order = await _orderService.CreateOrderAsync(dto, userId);
        return CreatedAtAction(nameof(GetOrderById), new { id = order.Id }, order);
        
    }
    [HttpGet]
    [Authorize (Roles ="admin")]
    public async Task<ActionResult<List<OrderResponseDto>>> GetAllOrders()
    {
        
        int userId = _claimService.GetUserId(User);
        bool isAdmin = _claimService.IsAdmin(User);
        var orders = await _orderService.GetAllOrdersAsync(userId, isAdmin);
        return Ok(orders);
       
    }


        [HttpGet("{id}")]
        public async Task<ActionResult<OrderResponseDto>> GetOrderById(int id)
        {
        
        int userId = _claimService.GetUserId(User);
        bool isAdmin = _claimService.IsAdmin(User);
        var order = await _orderService.GetOrderByIdAsync(id, userId, isAdmin);
        return Ok(order);
       
    }
    } 



