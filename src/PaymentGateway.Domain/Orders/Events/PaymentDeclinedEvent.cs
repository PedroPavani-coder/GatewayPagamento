using PaymentGateway.Domain.Common;

namespace PaymentGateway.Domain.Orders.Events;

/// <summary>
/// Disparado quando o Worker recebe do provedor de pagamento uma recusa.
/// </summary>
public sealed class PaymentDeclinedEvent : IDomainEvent
{
    public Guid OrderId { get; }
    public string Reason { get; }
    public DateTime OccurredOn { get; }

    public PaymentDeclinedEvent(Guid orderId, string reason)
    {
        OrderId = orderId;
        Reason = reason;
        OccurredOn = DateTime.UtcNow;
    }
}
