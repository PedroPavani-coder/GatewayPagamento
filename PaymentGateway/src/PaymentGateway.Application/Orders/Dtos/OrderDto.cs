using PaymentGateway.Domain.Orders;

namespace PaymentGateway.Application.Orders.Dtos;

/// <summary>
/// Representa o pedido do jeito que a API vai devolver pro cliente.
/// Note que isso é diferente da entidade Order do Domain: aqui é só dado "achatado",
/// sem métodos, sem regra de negócio — ideal pra virar JSON.
/// </summary>
public record OrderDto(
    Guid Id,
    string CustomerEmail,
    string Status,
    decimal TotalAmount,
    string Currency,
    DateTime CreatedAt,
    DateTime? ProcessedAt,
    string? DeclinedReason,
    List<OrderItemResponseDto> Items
)
{
    /// <summary>
    /// Método de mapeamento: converte a entidade de Domínio (Order) pro DTO de resposta.
    /// Fazemos esse mapeamento manual (sem AutoMapper) por enquanto — é mais código,
    /// mas fica bem explícito o que tá acontecendo, o que ajuda a entender o fluxo.
    /// </summary>
    public static OrderDto FromDomain(Order order)
    {
        var items = order.Items
            .Select(i => new OrderItemResponseDto(i.ProductName, i.Quantity, i.UnitPrice.Amount, i.Total.Amount))
            .ToList();

        return new OrderDto(
            order.Id,
            order.CustomerEmail,
            order.Status.ToString(),
            order.TotalAmount.Amount,
            order.TotalAmount.Currency,
            order.CreatedAt,
            order.ProcessedAt,
            order.DeclinedReason,
            items);
    }
}

public record OrderItemResponseDto(string ProductName, int Quantity, decimal UnitPrice, decimal Total);
