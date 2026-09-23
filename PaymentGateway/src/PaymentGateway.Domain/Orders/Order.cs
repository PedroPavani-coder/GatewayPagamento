using PaymentGateway.Domain.Common;
using PaymentGateway.Domain.Exceptions;
using PaymentGateway.Domain.Orders.Events;
using PaymentGateway.Domain.ValueObjects;

namespace PaymentGateway.Domain.Orders;

/// <summary>
/// Aggregate Root do nosso domínio de checkout.
///
/// Todo o ciclo de vida de um pedido — desde a criação até ser pago/recusado/cancelado —
/// é controlado por métodos desta classe. Isso garante que é IMPOSSÍVEL colocar o pedido
/// num estado inválido (ex: "pagar" um pedido que já foi cancelado) em qualquer lugar
/// do sistema, porque toda mudança de estado passa por aqui.
/// </summary>
public class Order : AggregateRoot
{
    private readonly List<OrderItem> _items = new();

    public string CustomerEmail { get; private set; } = string.Empty;
    public OrderStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public string? DeclinedReason { get; private set; }

    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    public Money TotalAmount =>
        _items.Aggregate(Money.Zero(), (acc, item) => acc.Add(item.Total));

    // Exigido pelo EF Core
    private Order() { }

    private Order(Guid id, string customerEmail) : base(id)
    {
        CustomerEmail = customerEmail;
        Status = OrderStatus.PendingPayment;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Factory method para criar um novo pedido. Usamos "Create" estático em vez de
    /// deixar o construtor público para deixar explícito o ponto de entrada do agregado
    /// e garantir que ele nasce sempre em um estado válido.
    /// </summary>
    public static Order Create(string customerEmail)
    {
        if (string.IsNullOrWhiteSpace(customerEmail))
            throw new DomainException("O e-mail do cliente é obrigatório.");

        return new Order(Guid.NewGuid(), customerEmail);
    }

    /// <summary>
    /// Adiciona um item ao pedido. Só pode ser feito enquanto o pedido ainda
    /// não foi enviado para processamento de pagamento.
    /// </summary>
    public void AddItem(string productName, int quantity, decimal unitPrice, string currency = "BRL")
    {
        EnsureIsEditable();

        var money = Money.Create(unitPrice, currency);
        var item = new OrderItem(productName, quantity, money);
        _items.Add(item);
    }

    /// <summary>
    /// Confirma o checkout: valida que o pedido tem itens e dispara o evento
    /// que a Infraestrutura vai usar para publicar a mensagem no RabbitMQ.
    /// A partir daqui, o pedido não pode mais ser editado.
    /// </summary>
    public void ConfirmCheckout()
    {
        EnsureIsEditable();

        if (!_items.Any())
            throw new DomainException("Não é possível confirmar um pedido sem itens.");

        Status = OrderStatus.Confirmed;
        RaiseDomainEvent(new OrderCreatedEvent(Id, TotalAmount.Amount, TotalAmount.Currency));
    }

    /// <summary>
    /// Chamado pelo Worker quando ele pega a mensagem da fila e começa a
    /// conversar com o provedor de pagamento externo.
    /// </summary>
    public void StartProcessing()
    {
        if (Status != OrderStatus.Confirmed)
            throw new DomainException($"Só é possível iniciar o processamento de pedidos confirmados. Status atual: {Status}.");

        Status = OrderStatus.Processing;
    }

    /// <summary>
    /// Chamado pelo Worker quando o provedor de pagamento confirma a aprovação.
    /// </summary>
    public void ApprovePayment()
    {
        if (Status != OrderStatus.Processing)
            throw new DomainException($"Só é possível aprovar pedidos em processamento. Status atual: {Status}.");

        Status = OrderStatus.Paid;
        ProcessedAt = DateTime.UtcNow;
        RaiseDomainEvent(new PaymentApprovedEvent(Id));
    }

    /// <summary>
    /// Chamado pelo Worker quando o provedor de pagamento recusa a transação.
    /// </summary>
    public void DeclinePayment(string reason)
    {
        if (Status != OrderStatus.Processing)
            throw new DomainException($"Só é possível recusar pedidos em processamento. Status atual: {Status}.");

        Status = OrderStatus.Declined;
        ProcessedAt = DateTime.UtcNow;
        DeclinedReason = reason;
        RaiseDomainEvent(new PaymentDeclinedEvent(Id, reason));
    }

    /// <summary>
    /// Cancela o pedido, desde que ainda não tenha sido processado.
    /// </summary>
    public void Cancel()
    {
        if (Status is OrderStatus.Paid or OrderStatus.Declined)
            throw new DomainException("Não é possível cancelar um pedido que já foi processado.");

        Status = OrderStatus.Cancelled;
    }

    private void EnsureIsEditable()
    {
        if (Status != OrderStatus.PendingPayment)
            throw new DomainException("Não é possível modificar um pedido que já foi enviado para processamento.");
    }
}
