using PaymentGateway.Domain.Common;

namespace PaymentGateway.Domain.Orders.Events;

/// <summary>
/// Disparado quando o Worker confirma que o pagamento de um pedido foi aprovado.
/// </summary>
public sealed class PaymentApprovedEvent : IDomainEvent
{
    public Guid OrderId { get; }
    public DateTime OccurredOn { get; }

    public PaymentApprovedEvent(Guid orderId)
    {
        OrderId = orderId;
        OccurredOn = DateTime.UtcNow;
    }
}
