using MediatR;
using PaymentGateway.Application.Common.Interfaces;
using PaymentGateway.Application.Orders.Dtos;

namespace PaymentGateway.Application.Orders.Queries.GetOrderById;

public class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, OrderDto?>
{
    private readonly IOrderRepository _orderRepository;

    public GetOrderByIdQueryHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<OrderDto?> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);

        // Se não achou, devolvemos null. Vai ser responsabilidade da Api (próximo passo)
        // transformar esse null em um HTTP 404.
        return order is null ? null : OrderDto.FromDomain(order);
    }
}
