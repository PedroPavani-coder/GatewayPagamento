using PaymentGateway.Domain.Common;
using PaymentGateway.Domain.Exceptions;
using PaymentGateway.Domain.ValueObjects;

namespace PaymentGateway.Domain.Orders;

/// <summary>
/// Representa um item dentro de um Pedido (ex: "2x Camiseta Azul - R$ 50,00 cada").
///
/// Importante: OrderItem é uma Entidade filha dentro do Aggregate "Order".
/// Ela só existe DENTRO de um pedido — não faz sentido ter um OrderItem "solto".
/// Por isso o construtor é 'internal': só a própria camada Domain (na prática, a
/// classe Order) pode criar um OrderItem.
/// </summary>
public class OrderItem : Entity
{
    public string ProductName { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public Money UnitPrice { get; private set; } = null!;

    public Money Total => UnitPrice.Multiply(Quantity);

    // Exigido pelo EF Core
    private OrderItem() { }

    internal OrderItem(string productName, int quantity, Money unitPrice) : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(productName))
            throw new DomainException("O nome do produto é obrigatório.");

        if (quantity <= 0)
            throw new DomainException("A quantidade deve ser maior que zero.");

        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }
}
