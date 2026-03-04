namespace ECommercePaymentIntegration.Domain.Entities;

public class Product
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = "TRY";
    public string Category { get; set; } = string.Empty;
    public int Stock { get; set; }

    public bool IsInStock(int quantity) => Stock >= quantity;
}
