namespace ECommercePaymentIntegration.Application.DTOs.Requests;

public class CreateOrderRequest
{
    public List<OrderItemRequest> Items { get; set; } = new();
    public string? IdempotencyKey { get; set; }
}
