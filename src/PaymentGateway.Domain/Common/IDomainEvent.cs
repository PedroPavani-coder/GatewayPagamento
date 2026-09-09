namespace PaymentGateway.Domain.Common;

/// <summary>
/// Marca uma classe como um Evento de Domínio.
/// Eventos de domínio representam "algo importante que aconteceu" dentro do negócio
/// (ex: "Pedido foi criado", "Pagamento foi aprovado").
/// Eles serão disparados pelos Agregados e tratados depois (ex: publicando na fila RabbitMQ).
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// Momento em que o evento ocorreu. Útil para auditoria e ordenação.
    /// </summary>
    DateTime OccurredOn { get; }
}
