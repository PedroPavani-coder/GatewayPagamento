namespace PaymentGateway.Api.Contracts;

/// <summary>
/// Formato exato do JSON que o cliente da API precisa enviar pra criar um pedido.
///
/// Por que não usar direto o CreateOrderCommand da Application aqui? Porque assim a
/// gente separa "o formato da minha API" de "o formato do meu caso de uso interno".
/// Se um dia eu quiser mudar como o Command é estruturado por dentro, não preciso
/// quebrar o contrato público da API (e vice-versa).
/// </summary>
public record CreateOrderRequest(
    string CustomerEmail,
    List<CreateOrderItemRequest> Items
);

public record CreateOrderItemRequest(
    string ProductName,
    int Quantity,
    decimal UnitPrice
);
