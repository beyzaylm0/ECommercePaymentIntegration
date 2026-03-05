using Microsoft.Extensions.DependencyInjection;

namespace ECommercePaymentIntegration.Application.Common;

public class Mediator : IMediator
{
    private readonly IServiceProvider _serviceProvider;

    public Mediator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        var requestType = request.GetType();
        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, typeof(TResponse));

        var handler = _serviceProvider.GetService(handlerType)
            ?? throw new InvalidOperationException($"No handler registered for {requestType.Name}");

        var behaviorType = typeof(IPipelineBehavior<,>).MakeGenericType(requestType, typeof(TResponse));
        var behaviors = _serviceProvider.GetServices(behaviorType).Cast<object>().ToList();

        RequestHandlerDelegate<TResponse> handlerDelegate = () =>
        {
            var handleMethod = handlerType.GetMethod("Handle")!;
            return (Task<TResponse>)handleMethod.Invoke(handler, new object[] { request, cancellationToken })!;
        };

        for (var i = behaviors.Count - 1; i >= 0; i--)
        {
            var behavior = behaviors[i];
            var next = handlerDelegate;
            var handleMethod = behaviorType.GetMethod("Handle")!;
            handlerDelegate = () => (Task<TResponse>)handleMethod.Invoke(behavior, new object[] { request, next, cancellationToken })!;
        }

        return handlerDelegate();
    }
}
