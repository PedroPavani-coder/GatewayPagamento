using FluentAssertions;
using PaymentGateway.Domain.Exceptions;
using PaymentGateway.Domain.Orders;
using PaymentGateway.Domain.Orders.Events;
using Xunit;

namespace PaymentGateway.Domain.Tests.Orders;

public class OrderTests
{
    // Método auxiliar para não repetir a criação de um pedido válido em todo teste
    private static Order CriarPedidoComUmItem()
    {
        var order = Order.Create("cliente@teste.com");
        order.AddItem("Produto Teste", 2, 50m);
        return order;
    }

    [Fact]
    public void Create_ComEmailValido_DeveCriarPedidoPendente()
    {
        // Act
        var order = Order.Create("cliente@teste.com");

        // Assert
        order.CustomerEmail.Should().Be("cliente@teste.com");
        order.Status.Should().Be(OrderStatus.PendingPayment);
        order.Items.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_ComEmailInvalido_DeveLancarDomainException(string? emailInvalido)
    {
        // Act
        Action act = () => Order.Create(emailInvalido!);

        // Assert
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void AddItem_DeveAdicionarItemESomarNoTotal()
    {
        // Arrange
        var order = Order.Create("cliente@teste.com");

        // Act
        order.AddItem("Camiseta", 2, 50m);
        order.AddItem("Boné", 1, 30m);

        // Assert
        order.Items.Should().HaveCount(2);
        order.TotalAmount.Amount.Should().Be(130m); // (2*50) + (1*30)
    }

    [Fact]
    public void ConfirmCheckout_SemItens_DeveLancarDomainException()
    {
        // Arrange: pedido criado mas sem nenhum item adicionado
        var order = Order.Create("cliente@teste.com");

        // Act
        Action act = () => order.ConfirmCheckout();

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*sem itens*");
    }

    [Fact]
    public void ConfirmCheckout_ComItens_DeveDispararOrderCreatedEvent()
    {
        // Arrange
        var order = CriarPedidoComUmItem();

        // Act
        order.ConfirmCheckout();

        // Assert: verificamos que o evento de domínio foi registrado corretamente,
        // pois é ele que a Infraestrutura vai usar para publicar no RabbitMQ depois.
        order.DomainEvents.Should().ContainSingle(e => e is OrderCreatedEvent);

        var evento = (OrderCreatedEvent)order.DomainEvents.Single();
        evento.OrderId.Should().Be(order.Id);
        evento.TotalAmount.Should().Be(100m); // 2 * 50
    }

    [Fact]
    public void AddItem_DepoisDeConfirmarCheckout_DeveLancarDomainException()
    {
        // Arrange: pedido já confirmado não pode mais ser editado
        var order = CriarPedidoComUmItem();
        order.ConfirmCheckout();

        // Act
        Action act = () => order.AddItem("Outro produto", 1, 10m);

        // Assert
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void StartProcessing_APartirDePendingPayment_DeveMudarStatusParaProcessing()
    {
        // Arrange
        var order = CriarPedidoComUmItem();
        order.ConfirmCheckout();

        // Act
        order.StartProcessing();

        // Assert
        order.Status.Should().Be(OrderStatus.Processing);
    }

    [Fact]
    public void StartProcessing_QuandoNaoEstaPendente_DeveLancarDomainException()
    {
        // Arrange: pedido já está processando, tentamos iniciar de novo
        var order = CriarPedidoComUmItem();
        order.ConfirmCheckout();
        order.StartProcessing();

        // Act
        Action act = () => order.StartProcessing();

        // Assert
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void ApprovePayment_APartirDeProcessing_DeveAprovarEDispararEvento()
    {
        // Arrange
        var order = CriarPedidoComUmItem();
        order.ConfirmCheckout();
        order.StartProcessing();
        order.ClearDomainEvents(); // limpamos para isolar o evento deste teste

        // Act
        order.ApprovePayment();

        // Assert
        order.Status.Should().Be(OrderStatus.Paid);
        order.ProcessedAt.Should().NotBeNull();
        order.DomainEvents.Should().ContainSingle(e => e is PaymentApprovedEvent);
    }

    [Fact]
    public void ApprovePayment_SemEstarProcessando_DeveLancarDomainException()
    {
        // Arrange: pedido ainda pendente, nunca foi para "Processing"
        var order = CriarPedidoComUmItem();

        // Act
        Action act = () => order.ApprovePayment();

        // Assert
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void DeclinePayment_APartirDeProcessing_DeveRecusarEDispararEvento()
    {
        // Arrange
        var order = CriarPedidoComUmItem();
        order.ConfirmCheckout();
        order.StartProcessing();
        order.ClearDomainEvents();

        // Act
        order.DeclinePayment("Cartão sem limite");

        // Assert
        order.Status.Should().Be(OrderStatus.Declined);
        order.DeclinedReason.Should().Be("Cartão sem limite");
        order.DomainEvents.Should().ContainSingle(e => e is PaymentDeclinedEvent);
    }

    [Fact]
    public void Cancel_QuandoPendente_DeveCancelarComSucesso()
    {
        // Arrange
        var order = CriarPedidoComUmItem();

        // Act
        order.Cancel();

        // Assert
        order.Status.Should().Be(OrderStatus.Cancelled);
    }

    [Fact]
    public void Cancel_QuandoJaPago_DeveLancarDomainException()
    {
        // Arrange: pedido já finalizado como pago
        var order = CriarPedidoComUmItem();
        order.ConfirmCheckout();
        order.StartProcessing();
        order.ApprovePayment();

        // Act
        Action act = () => order.Cancel();

        // Assert: não faz sentido cancelar algo que já foi pago
        act.Should().Throw<DomainException>();
    }
}
