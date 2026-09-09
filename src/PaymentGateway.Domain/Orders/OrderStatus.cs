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
    /// <summary>Pedido criado, aguardando envio para a fila de processamento.</summary>
    PendingPayment = 0,

    /// <summary>Mensagem já foi consumida pelo Worker, pagamento sendo processado no gateway externo.</summary>
    Processing = 1,

    /// <summary>Pagamento aprovado com sucesso.</summary>
    Paid = 2,

    /// <summary>Pagamento recusado pelo provedor (cartão sem limite, fraude, etc).</summary>
    Declined = 3,

    /// <summary>Pedido cancelado antes de concluir o pagamento.</summary>
    Cancelled = 4
}
