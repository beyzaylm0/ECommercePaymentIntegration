using ECommercePaymentIntegration.Application.DTOs.Requests;
using FluentValidation;

namespace ECommercePaymentIntegration.Application.Orders.Commands.CreateOrder;

public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.Items)
            .NotNull().WithMessage("Items cannot be null.")
            .NotEmpty().WithMessage("Order must contain at least one item.")
            .Must(items => items == null || items.Count <= 50)
                .WithMessage("Order cannot contain more than 50 items.");

        RuleFor(x => x.Items)
            .Must(items => items == null || items.Select(i => i.ProductId).Distinct().Count() == items.Count)
            .WithMessage("Duplicate product IDs found. Combine quantities for the same product.")
            .When(x => x.Items != null && x.Items.Count > 0);

        RuleForEach(x => x.Items).SetValidator(new OrderItemRequestValidator());
    }
}

public class OrderItemRequestValidator : AbstractValidator<OrderItemRequest>
{
    public OrderItemRequestValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Product ID is required.")
            .MaximumLength(100).WithMessage("Product ID must not exceed 100 characters.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than zero.")
            .LessThanOrEqualTo(100).WithMessage("Quantity must not exceed 100 per item.");
    }
}
