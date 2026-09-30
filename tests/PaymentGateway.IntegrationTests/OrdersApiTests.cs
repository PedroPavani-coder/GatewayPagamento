using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace PaymentGateway.IntegrationTests;

/// <summary>
/// "IClassFixture&lt;CustomWebApplicationFactory&gt;" faz o xUnit reaproveitar a MESMA
/// instância da fábrica (e, portanto, o mesmo container de banco) entre todos os
/// testes desta classe — subir um container novo pra cada teste individual deixaria
/// a suíte de testes bem mais lenta.
/// </summary>
public class OrdersApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public OrdersApiTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<HttpClient> ClienteAutenticadoAsync()
    {
        var token = await AuthHelper.ObterTokenAsync(_client);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return _client;
    }

    [Fact]
    public async Task Post_SemToken_DeveRetornarUnauthorized()
    {
        // Arrange: um cliente HTTP sem nenhum header de autenticação
        var request = new
        {
            customerEmail = "sem-login@teste.com",
            items = new[] { new { productName = "Produto", quantity = 1, unitPrice = 10m } }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Post_ComDadosValidos_DeveCriarPedidoERetornar201()
    {
        // Arrange
        var client = await ClienteAutenticadoAsync();
        var request = new
        {
            customerEmail = "cliente@teste.com",
            items = new[]
            {
                new { productName = "Camiseta", quantity = 2, unitPrice = 50m },
                new { productName = "Boné", quantity = 1, unitPrice = 30m }
            }
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/orders", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<CreateOrderResultDto>();
        body!.TotalAmount.Should().Be(130m);
        body.Status.Should().Be("Confirmed");

        // O header Location deve apontar pro GET desse mesmo pedido — boa prática REST
        response.Headers.Location.Should().NotBeNull();
    }

    [Fact]
    public async Task Post_SemItens_DeveRetornar400ComMensagemDoDominio()
    {
        // Arrange: esse é o caso que prova que o Middleware de exceção (Capítulo 10
        // do guia de estudo) está funcionando de ponta a ponta, junto com a regra de
        // negócio real do Domain.
        var client = await ClienteAutenticadoAsync();
        var request = new { customerEmail = "cliente@teste.com", items = Array.Empty<object>() };

        // Act
        var response = await client.PostAsJsonAsync("/api/orders", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Get_DepoisDeCriarPedido_DeveRetornarOsDadosCorretos()
    {
        // Arrange: primeiro cria um pedido de verdade...
        var client = await ClienteAutenticadoAsync();
        var createRequest = new
        {
            customerEmail = "consulta@teste.com",
            items = new[] { new { productName = "Produto Único", quantity = 3, unitPrice = 20m } }
        };
        var createResponse = await client.PostAsJsonAsync("/api/orders", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateOrderResultDto>();

        // Act: ...depois consulta ele pelo Id devolvido
        var getResponse = await client.GetAsync($"/api/orders/{created!.OrderId}");

        // Assert
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var order = await getResponse.Content.ReadFromJsonAsync<OrderDto>();
        order!.CustomerEmail.Should().Be("consulta@teste.com");
        order.TotalAmount.Should().Be(60m);
        order.Items.Should().ContainSingle(i => i.ProductName == "Produto Único");
    }

    [Fact]
    public async Task Get_ComIdInexistente_DeveRetornar404()
    {
        // Arrange
        var client = await ClienteAutenticadoAsync();
        var idQueNaoExiste = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/orders/{idQueNaoExiste}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // DTOs simples só pra desserializar as respostas JSON nos testes acima.
    private record CreateOrderResultDto(Guid OrderId, string Status, decimal TotalAmount, string Currency);
    private record OrderItemResponseDto(string ProductName, int Quantity, decimal UnitPrice, decimal Total);
    private record OrderDto(
        Guid Id, string CustomerEmail, string Status, decimal TotalAmount, string Currency,
        DateTime CreatedAt, DateTime? ProcessedAt, string? DeclinedReason,
        List<OrderItemResponseDto> Items);
}
