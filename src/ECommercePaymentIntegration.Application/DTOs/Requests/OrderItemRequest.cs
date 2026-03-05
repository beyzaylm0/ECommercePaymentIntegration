namespace ECommercePaymentIntegration.Application.DTOs.Requests;

public class OrderItemRequest
{
    public string ProductId { get; set; } = string.Empty;
    public int Quantity { get; set; }
}
