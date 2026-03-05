using ECommercePaymentIntegration.Application.Common;
using ECommercePaymentIntegration.Application.DTOs.Requests;
using ECommercePaymentIntegration.Application.DTOs.Responses;
using ECommercePaymentIntegration.Application.Orders.Commands.CancelOrder;
using ECommercePaymentIntegration.Application.Orders.Commands.CompleteOrder;
using ECommercePaymentIntegration.Application.Orders.Commands.CreateOrder;
using ECommercePaymentIntegration.Application.Orders.Queries.GetAllOrders;
using ECommercePaymentIntegration.Application.Orders.Queries.GetOrder;
using Microsoft.AspNetCore.Mvc;

namespace ECommercePaymentIntegration.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrdersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Create a new order with product list and reserve funds.
    /// </summary>
    /// <param name="request">Order creation request with product items.</param>
    /// <returns>The created order with reserved status.</returns>
    /// <response code="201">Order created and funds reserved successfully</response>
    /// <response code="400">Validation error or insufficient balance</response>
    /// <response code="404">Product not found</response>
    /// <response code="502">Balance Management service is unavailable</response>
    [HttpPost("create")]
    [ProducesResponseType(typeof(ApiResponse<OrderResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        var command = new CreateOrderCommand
        {
            Items = request.Items,
            IdempotencyKey = Request.Headers.TryGetValue("Idempotency-Key", out var header)
                ? header.ToString()
                : request.IdempotencyKey
        };

        var (order, isExisting) = await _mediator.Send<(OrderResponse, bool)>(command);

        if (isExisting)
            return Ok(ApiResponse<OrderResponse>.SuccessResponse(order, "Order already exists for this idempotency key."));

        return StatusCode(StatusCodes.Status201Created,
            ApiResponse<OrderResponse>.SuccessResponse(order, "Order created and funds reserved successfully."));
    }

    /// <summary>
    /// Complete an order and finalize payment.
    /// </summary>
    /// <param name="id">The order ID to complete.</param>
    /// <returns>The completed order.</returns>
    /// <response code="200">Order completed successfully</response>
    /// <response code="400">Order is not in a valid state for completion</response>
    /// <response code="404">Order not found</response>
    /// <response code="502">Balance Management service is unavailable</response>
    [HttpPost("{id}/complete")]
    [ProducesResponseType(typeof(ApiResponse<OrderResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> CompleteOrder(string id)
    {
        var order = await _mediator.Send<OrderResponse>(new CompleteOrderCommand { OrderId = id });
        return Ok(ApiResponse<OrderResponse>.SuccessResponse(order, "Order completed successfully."));
    }

    /// <summary>
    /// Cancel an order and release reserved funds.
    /// </summary>
    /// <param name="id">The order ID to cancel.</param>
    /// <returns>The cancelled order.</returns>
    /// <response code="200">Order cancelled successfully</response>
    /// <response code="400">Order is not in a valid state for cancellation</response>
    /// <response code="404">Order not found</response>
    /// <response code="502">Balance Management service is unavailable</response>
    [HttpPost("{id}/cancel")]
    [ProducesResponseType(typeof(ApiResponse<OrderResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> CancelOrder(string id)
    {
        var order = await _mediator.Send<OrderResponse>(new CancelOrderCommand { OrderId = id });
        return Ok(ApiResponse<OrderResponse>.SuccessResponse(order, "Order cancelled successfully."));
    }

    /// <summary>
    /// Get a specific order by ID.
    /// </summary>
    /// <param name="id">The order ID.</param>
    /// <returns>The order details.</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<OrderResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrder(string id)
    {
        var order = await _mediator.Send<OrderResponse?>(new GetOrderQuery { OrderId = id });
        if (order is null)
            return NotFound(ApiResponse<object>.ErrorResponse($"Order with ID '{id}' not found."));

        return Ok(ApiResponse<OrderResponse>.SuccessResponse(order));
    }

    /// <summary>
    /// Get all orders.
    /// </summary>
    /// <returns>List of all orders.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<OrderResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllOrders()
    {
        var orders = await _mediator.Send<IEnumerable<OrderResponse>>(new GetAllOrdersQuery());
        return Ok(ApiResponse<IEnumerable<OrderResponse>>.SuccessResponse(orders));
    }
}
