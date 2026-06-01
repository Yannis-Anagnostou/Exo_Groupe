using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OrderManagement.Application.DTOs.OrderDto;
using OrderManagement.Application.Exceptions;
using OrderManagement.Application.Services.OrderService;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Enums;
using OrderManagement.Infrastructure.Data;
using System.Security.Claims;

namespace OrderManagement.API.Controllers.OrderControllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrderController : ControllerBase
{
    private readonly IOrderService _orderService;
    

    // Un seul constructeur avec tout dedans
    public OrderController(IOrderService orderService)
    {
        _orderService = orderService;
    
    }


    [HttpPost]
    [EnableRateLimiting("add")]
    public async Task<ActionResult<OrderResponseDto>> CreateOrder([FromBody] CreateOrderDto dto)
    {
        //try
        //{
        Claim userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);

        if (userIdClaim == null)
            return Unauthorized(new { error = "Token invalide ou manquant." });

        int userId = int.Parse(userIdClaim.Value);

        

        var order = await _orderService.CreateOrderAsync(dto, userId);
        return CreatedAtAction(nameof(GetOrderById), new { id = order.Id }, order);
        //}
        //catch (BadRequestException ex)
        //{
        //    return BadRequest(new { error = ex.Message });
        //}
        //catch (Exception ex)
        //{
        //    return StatusCode(500, new { error = "Une erreur est survenue lors de la création de la commande." });
        //} }
    }
    [HttpGet]
    //[Authorize (Roles ="admin")]
    public async Task<ActionResult<List<OrderResponseDto>>> GetAllOrders()
    {
        //try
        //{
            Claim? userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            Claim? roleClaim = HttpContext.User.FindFirst(ClaimTypes.Role);

            if (userIdClaim == null || roleClaim == null)
                return Unauthorized(new { error = "Token invalide ou manquant." });

            int userId = int.Parse(userIdClaim.Value);
            bool isAdmin = roleClaim.Value == "Admin";

       

        var orders = await _orderService.GetAllOrdersAsync(userId, isAdmin);
            return Ok(orders);
        //}
        //catch (BadRequestException ex)
        //{
        //    return BadRequest(new { error = ex.Message });
        //}
        //catch (Exception ex)
        //{
        //    return StatusCode(500, new { error = "Une erreur est survenue lors de la récupération des commandes." });
        //} 
    }




        [HttpGet("{id}")]
        public async Task<ActionResult<OrderResponseDto>> GetOrderById(int id)
        {
            //try
            //{
                Claim? userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
                Claim? roleClaim = HttpContext.User.FindFirst(ClaimTypes.Role);

                if (userIdClaim == null || roleClaim == null)
                    return Unauthorized(new { error = "Token invalide ou manquant." });

                int userId = int.Parse(userIdClaim.Value);
                bool isAdmin = roleClaim.Value == "Admin";

           

            var order = await _orderService.GetOrderByIdAsync(id, userId, isAdmin);
                return Ok(order);
            //}
            //catch (BadRequestException ex)
            //{
            //    return BadRequest(new { error = ex.Message });
            //}
            //catch (NotFoundException ex)
            //{
            //    return NotFound(new { error = ex.Message });
            //}
            //catch (Exception ex)
            //{
            //    return StatusCode(500, new { error = "Une erreur est survenue lors de la récupération de la commande." });
            //}
        }
    } 



