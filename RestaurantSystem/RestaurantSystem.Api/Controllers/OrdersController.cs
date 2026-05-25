using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using RestaurantSystem.Api.Hubs;
using RestaurantSystem.Api.Models;
using RestaurantSystem.Core.Entities;
using RestaurantSystem.Core.Interfaces;

namespace RestaurantSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHubContext<OrderHub> _hubContext;

    public OrdersController(IUnitOfWork unitOfWork, IHubContext<OrderHub> hubContext)
    {
        _unitOfWork = unitOfWork;
        _hubContext = hubContext;
    }

    [HttpPost]
    public async Task<ActionResult<Order>> Create([FromBody] CreateOrderDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var order = new Order
        {
            UserId = userId,
            OrderDate = DateTime.UtcNow,
            Status = OrderStatus.Pending
        };

        decimal total = 0;
        foreach (var itemDto in dto.Items)
        {
            var menuItem = await _unitOfWork.MenuItems.GetByIdAsync(itemDto.MenuItemId);
            if (menuItem == null) return BadRequest($"Menu item {itemDto.MenuItemId} not found");

            var orderItem = new OrderItem
            {
                MenuItemId = itemDto.MenuItemId,
                Quantity = itemDto.Quantity,
                UnitPrice = menuItem.Price
            };
            order.OrderItems.Add(orderItem);
            total += orderItem.UnitPrice * orderItem.Quantity;
        }

        order.TotalAmount = total;

        await _unitOfWork.Orders.AddAsync(order);
        await _unitOfWork.CompleteAsync();

        await _hubContext.Clients.User(userId).SendAsync("OrderCreated", order);

        return Ok(order);
    }

    [HttpGet("my")]
    public async Task<ActionResult<IEnumerable<Order>>> GetMyOrders()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var orders = await _unitOfWork.Orders.GetUserOrdersAsync(userId);
        return Ok(orders);
    }
}
