using MediatR;
using PaymentGateway.Application.Orders.Dtos;

namespace PaymentGateway.Application.Orders.Commands.CreateOrder;

/// <summary>
/// Um Command representa "uma intenção de mudar alguma coisa no sistema".
/// Esse aqui representa: "quero criar um pedido com esses itens e confirmar o checkout".
///
/// Implementa IRequest&lt;CreateOrderResult&gt; do MediatR, que diz: "quando esse Command
/// for executado, o resultado vai ser um CreateOrderResult".
/// </summary>
public record CreateOrderCommand(
    string CustomerEmail,
    List<OrderItemDto> Items
) : IRequest<CreateOrderResult>;

/// <summary>
/// O que devolvemos pra API depois de criar o pedido com sucesso.
/// Repare que é um objeto simples — não devolvemos a entidade Order inteira,
/// pra não vazar detalhes internos do Domain pra fora da Application.
/// </summary>
public record CreateOrderResult(
    Guid OrderId,
    string Status,
    decimal TotalAmount,
    string Currency
);
