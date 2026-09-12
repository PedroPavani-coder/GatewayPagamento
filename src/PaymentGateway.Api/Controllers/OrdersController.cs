using MediatR;
using Microsoft.AspNetCore.Mvc;
using PaymentGateway.Api.Contracts;
using PaymentGateway.Application.Orders.Commands.CreateOrder;
using PaymentGateway.Application.Orders.Dtos;
using PaymentGateway.Application.Orders.Queries.GetOrderById;

namespace PaymentGateway.Api.Controllers;

/// <summary>
/// Repare que o Controller é BEM enxuto: ele só traduz HTTP para MediatR e volta.
/// Nenhuma regra de negócio mora aqui — isso já está no Domain (validações) e na
/// Application (orquestração). O Controller só faz o "encaixe" com o mundo HTTP.
/// </summary>
[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrdersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Cria um novo pedido e já confirma o checkout, disparando o evento que
    /// (no futuro, quando ligarmos o Worker) vai iniciar o processamento do pagamento.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateOrderResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        // Convertendo o contrato da Api (CreateOrderRequest) pro DTO da Application
        // (OrderItemDto). É um pouco repetitivo, mas mantém as camadas desacopladas.
        var items = request.Items
            .Select(i => new OrderItemDto(i.ProductName, i.Quantity, i.UnitPrice))
            .ToList();

        var command = new CreateOrderCommand(request.CustomerEmail, items);

        var result = await _mediator.Send(command, cancellationToken);

        // 201 Created + o Location header apontando pro GET que busca esse pedido.
        // Isso é uma boa prática REST: quem criou o recurso já sabe onde consultá-lo depois.
        return CreatedAtAction(nameof(GetById), new { id = result.OrderId }, result);
    }

    /// <summary>
    /// Consulta o status atual de um pedido. É assim que o cliente do checkout
    /// assíncrono vai descobrir se o pagamento foi aprovado ou recusado.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetOrderByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }
}
