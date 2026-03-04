using ECommercePaymentIntegration.Domain.Enums;
using ECommercePaymentIntegration.Domain.Exceptions;

namespace ECommercePaymentIntegration.Domain.Entities;

public class Order
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public List<OrderItem> Items { get; set; } = new();
    public decimal TotalAmount => Items.Sum(i => i.TotalPrice);
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? FailureReason { get; set; }
    public string? IdempotencyKey { get; set; }

    public void MarkAsReserved()
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOrderStateException(Id, Status, OrderStatus.Reserved);

        Status = OrderStatus.Reserved;
    }

    public void MarkAsCompleted()
    {
        if (Status != OrderStatus.Reserved)
            throw new InvalidOrderStateException(Id, Status, OrderStatus.Completed);

        Status = OrderStatus.Completed;
        CompletedAt = DateTime.UtcNow;
    }

    public void MarkAsCancelled()
    {
        if (Status != OrderStatus.Reserved)
            throw new InvalidOrderStateException(Id, Status, OrderStatus.Cancelled);

        Status = OrderStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
    }

    public void MarkAsFailed(string reason)
    {
        Status = OrderStatus.Failed;
        FailureReason = reason;
    }
}
