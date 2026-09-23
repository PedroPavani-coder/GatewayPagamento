using MediatR;
using PaymentGateway.Application.Common.Interfaces;
using PaymentGateway.Domain.Orders;

namespace PaymentGateway.Application.Orders.Commands.CreateOrder;

/// <summary>
/// O Handler é quem realmente EXECUTA o Command. Repare no fluxo:
/// 1. Usa o Domain (Order.Create, order.AddItem, order.ConfirmCheckout) pra aplicar as regras
/// 2. Usa o Repository (uma interface, não sabe se é SQL Server, Postgres, etc.) pra salvar
/// 3. Usa o UnitOfWork pra confirmar a transação
/// 4. Devolve um resultado simples pra API
///
/// Se o Domain lançar uma DomainException (ex: pedido sem itens), essa exceção sobe
/// naturalmente e será tratada lá na Api (no próximo passo), virando um HTTP 400.
/// </summary>
public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, CreateOrderResult>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateOrderCommandHandler(IOrderRepository orderRepository, IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CreateOrderResult> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        // 1. Monta o agregado usando as regras do Domain
        var order = Order.Create(request.CustomerEmail);

        foreach (var item in request.Items)
        {
            order.AddItem(item.ProductName, item.Quantity, item.UnitPrice);
        }

        order.ConfirmCheckout();

        // 2. Persiste
        await _orderRepository.AddAsync(order, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // NOTA: os order.DomainEvents (ex: OrderCreatedEvent) já foram disparados pelo
        // ConfirmCheckout(). No Passo 4 (Infrastructure), vamos publicar esses eventos no
        // RabbitMQ logo depois do SaveChangesAsync, e então chamar order.ClearDomainEvents().

        return new CreateOrderResult(
            order.Id,
            order.Status.ToString(),
            order.TotalAmount.Amount,
            order.TotalAmount.Currency);
    }
}
