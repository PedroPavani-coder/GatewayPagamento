namespace PaymentGateway.Worker.Messages;

/// <summary>
/// Representa o formato da mensagem que chega pela fila do RabbitMQ.
///
/// Por que não reaproveitar direto a classe OrderCreatedEvent do Domain aqui? Porque
/// "o que trafega entre sistemas" e "o que representa uma regra de negócio interna"
/// são coisas com propósitos diferentes, mesmo que pareçam iguais agora. Se um dia o
/// Domain Event mudar de formato, a gente não quer quebrar silenciosamente quem já
/// está consumindo mensagens antigas na fila.
/// </summary>
public class OrderCreatedMessage
{
    public Guid OrderId { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime OccurredOn { get; set; }
}
