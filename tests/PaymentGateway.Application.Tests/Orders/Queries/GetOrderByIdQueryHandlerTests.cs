using FluentAssertions;
using Moq;
using PaymentGateway.Application.Common.Interfaces;
using PaymentGateway.Application.Orders.Queries.GetOrderById;
using PaymentGateway.Domain.Orders;
using Xunit;

namespace PaymentGateway.Application.Tests.Orders.Queries;

public class GetOrderByIdQueryHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepositoryMock = new();

    private GetOrderByIdQueryHandler CriarHandler() => new(_orderRepositoryMock.Object);

    // Método auxiliar pra criar um pedido de exemplo, já confirmado
    private static Order CriarPedidoDeExemplo()
    {
        var order = Order.Create("cliente@teste.com");
        order.AddItem("Produto Teste", 1, 99.90m);
        order.ConfirmCheckout();
        return order;
    }

    [Fact]
    public async Task Handle_QuandoPedidoExiste_DeveRetornarOrderDtoPreenchido()
    {
        // Arrange: configuramos o mock pra "fingir" que encontrou o pedido no banco
        var order = CriarPedidoDeExemplo();

        _orderRepositoryMock
            .Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var handler = CriarHandler();
        var query = new GetOrderByIdQuery(order.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(order.Id);
        result.CustomerEmail.Should().Be("cliente@teste.com");
        result.TotalAmount.Should().Be(99.90m);
        result.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_QuandoPedidoNaoExiste_DeveRetornarNull()
    {
        // Arrange: configuramos o mock pra simular "não achei nada no banco"
        var idQueNaoExiste = Guid.NewGuid();

        _orderRepositoryMock
            .Setup(r => r.GetByIdAsync(idQueNaoExiste, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        var handler = CriarHandler();
        var query = new GetOrderByIdQuery(idQueNaoExiste);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert: é responsabilidade da Api transformar esse null num HTTP 404 depois
        result.Should().BeNull();
    }
}
