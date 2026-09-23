namespace PaymentGateway.Domain.Orders;

/// <summary>
/// Representa os possíveis estados de um Pedido ao longo do fluxo de checkout assíncrono.
///
/// Fluxo esperado:
/// PendingPayment -> Processing -> Paid   (caminho feliz)
/// PendingPayment -> Processing -> Declined (pagamento recusado)
/// PendingPayment -> Cancelled  (cliente ou sistema cancelou antes de processar)
/// </summary>
public enum OrderStatus
{
    /// <summary>Pedido criado, cliente ainda pode adicionar/remover itens.</summary>
    PendingPayment = 0,

    /// <summary>Checkout confirmado (não pode mais editar), aguardando o Worker pegar da fila.</summary>
    Confirmed = 1,

    /// <summary>Mensagem já foi consumida pelo Worker, pagamento sendo processado no gateway externo.</summary>
    Processing = 2,

    /// <summary>Pagamento aprovado com sucesso.</summary>
    Paid = 3,

    /// <summary>Pagamento recusado pelo provedor (cartão sem limite, fraude, etc).</summary>
    Declined = 4,

    /// <summary>Pedido cancelado antes de concluir o pagamento.</summary>
    Cancelled = 5
}
