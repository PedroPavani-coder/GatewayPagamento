using MediatR;

namespace PaymentGateway.Application.Orders.Commands.ProcessOrderPayment;

/// <summary>
/// Representa "processe o pagamento desse pedido". Quem vai disparar esse Command é
/// o Worker, depois de consumir a mensagem OrderCreatedEvent da fila.
///
/// Repare que esse Command não devolve nada (não implementa IRequest&lt;T&gt;, só
/// IRequest simples) — porque quem chamou (o Worker) não precisa de uma resposta
/// imediata, só precisa saber que a operação foi disparada.
/// </summary>
public record ProcessOrderPaymentCommand(Guid OrderId) : IRequest;
