using ECommercePaymentIntegration.Application.DTOs.ExternalServices;
using ECommercePaymentIntegration.Application.DTOs.Responses;
using ECommercePaymentIntegration.Domain.Entities;

namespace ECommercePaymentIntegration.Application.Mappings;

public static class MappingExtensions
{
    public static ProductResponse ToResponse(this ProductDto product)
    {
        return new ProductResponse
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            Currency = product.Currency,
            Category = product.Category,
            Stock = product.Stock
        };
    }

    public static OrderResponse ToResponse(this Order order)
    {
        return new OrderResponse
        {
            Id = order.Id,
            Items = order.Items.Select(i => i.ToResponse()).ToList(),
            TotalAmount = order.TotalAmount,
            Status = order.Status.ToString(),
            CreatedAt = order.CreatedAt,
            CompletedAt = order.CompletedAt,
            CancelledAt = order.CancelledAt
        };
    }

    public static OrderItemResponse ToResponse(this OrderItem item)
    {
        return new OrderItemResponse
        {
            ProductId = item.ProductId,
            ProductName = item.ProductName,
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice,
            TotalPrice = item.TotalPrice
        };
    }
}
