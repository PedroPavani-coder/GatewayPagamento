namespace PaymentGateway.Application.Orders.Dtos;

/// <summary>
/// DTO (Data Transfer Object) usado só pra transportar dados entre a API e o Application.
/// Não é uma entidade de domínio — é um "envelope" simples de dados, sem regra de negócio.
/// </summary>
public record OrderItemDto(string ProductName, int Quantity, decimal UnitPrice);
