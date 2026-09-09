using FluentAssertions;
using Moq;
using PaymentGateway.Application.Common.Interfaces;
using PaymentGateway.Application.Orders.Commands.CreateOrder;
using PaymentGateway.Application.Orders.Dtos;
using PaymentGateway.Domain.Exceptions;
using PaymentGateway.Domain.Orders;
using Xunit;

namespace PaymentGateway.Application.Tests.Orders.Commands;

public class CreateOrderCommandHandlerTests
{
    // Esses "Mocks" são versões falsas das interfaces IOrderRepository e IUnitOfWork.
    // Eles não tocam em banco de dados nenhum — só registram "o que foi chamado",
    // pra gente conseguir verificar depois.
    private readonly Mock<IOrderRepository> _orderRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    private CreateOrderCommandHandler CriarHandler() =>
        new(_orderRepositoryMock.Object, _unitOfWorkMock.Object);

    [Fact]
    public async Task Handle_ComDadosValidos_DeveCriarPedidoESalvarNoRepositorio()
    {
        // Arrange
        var handler = CriarHandler();
        var command = new CreateOrderCommand(
            "cliente@teste.com",
            new List<OrderItemDto>
            {
                new("Camiseta", 2, 50m),
                new("Boné", 1, 30m)
            });

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert: conferimos o resultado devolvido...
        result.TotalAmount.Should().Be(130m);
        result.Status.Should().Be(OrderStatus.Confirmed.ToString());

        // ...e conferimos que o Handler REALMENTE chamou o repositório e o UnitOfWork.
        // É isso que garante que o pedido de fato seria salvo, mesmo sem um banco real.
        _orderRepositoryMock.Verify(
            r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()),
            Times.Once);

        _unitOfWorkMock.Verify(
            u => u.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_SemItens_DeveLancarDomainExceptionENaoSalvar()
    {
        // Arrange: comando sem nenhum item — o Domain deve barrar isso no ConfirmCheckout
        var handler = CriarHandler();
        var command = new CreateOrderCommand("cliente@teste.com", new List<OrderItemDto>());

        // Act
        Func<Task> act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainException>();

        // Importante: como a exceção acontece ANTES do AddAsync, o repositório
        // nunca deveria ter sido chamado. Isso prova que não salvamos "meio pedido".
        _orderRepositoryMock.Verify(
            r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ComEmailInvalido_DeveLancarDomainException()
    {
        // Arrange
        var handler = CriarHandler();
        var command = new CreateOrderCommand(
            "",
            new List<OrderItemDto> { new("Produto", 1, 10m) });

        // Act
        Func<Task> act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainException>();
    }
}
