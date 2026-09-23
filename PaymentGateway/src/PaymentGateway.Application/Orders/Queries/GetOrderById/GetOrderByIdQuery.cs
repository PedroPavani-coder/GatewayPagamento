using MediatR;
using PaymentGateway.Application.Orders.Dtos;

namespace PaymentGateway.Application.Orders.Queries.GetOrderById;

/// <summary>
/// Uma Query representa "quero LER alguma coisa", sem alterar nada no sistema.
/// Por isso ela é bem mais simples que um Command — só precisa do dado necessário
/// pra fazer a busca (aqui, o Id do pedido).
/// </summary>
public record GetOrderByIdQuery(Guid OrderId) : IRequest<OrderDto?>;
