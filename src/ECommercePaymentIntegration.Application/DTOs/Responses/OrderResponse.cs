namespace ECommercePaymentIntegration.Application.DTOs.Responses;

public class OrderResponse
{
    public string Id { get; set; } = string.Empty;
    public List<OrderItemResponse> Items { get; set; } = new();
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
}
