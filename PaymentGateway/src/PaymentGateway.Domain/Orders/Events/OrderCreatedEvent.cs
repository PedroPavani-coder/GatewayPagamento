using PaymentGateway.Domain.Common;

namespace PaymentGateway.Domain.Orders.Events;

/// <summary>
/// Disparado quando um novo Pedido é criado e está pronto para ser enviado
/// à fila de processamento assíncrono de pagamento.
///
/// Quem vai "ouvir" esse evento, futuramente, será um Handler na camada de
/// Application/Infrastructure, responsável por publicar a mensagem no RabbitMQ.
/// </summary>
public sealed class OrderCreatedEvent : IDomainEvent
{
    public Guid OrderId { get; }
    public decimal TotalAmount { get; }
    public string Currency { get; }
    public DateTime OccurredOn { get; }

    public OrderCreatedEvent(Guid orderId, decimal totalAmount, string currency)
    {
        OrderId = orderId;
        TotalAmount = totalAmount;
        Currency = currency;
        OccurredOn = DateTime.UtcNow;
    }
}
