namespace PaymentGateway.Infrastructure.Messaging;

/// <summary>
/// Representa as configurações de conexão com o RabbitMQ. Os valores de verdade vão
/// vir do appsettings.json da Api (seção "RabbitMq"), usando o Options Pattern do .NET —
/// assim a gente não precisa hardcodar "localhost" no meio do código.
/// </summary>
public class RabbitMqSettings
{
    public const string SectionName = "RabbitMq";

    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";

    /// <summary>
    /// Nome do "exchange" (o ponto de entrada das mensagens no RabbitMQ) onde
    /// publicamos todos os eventos de domínio relacionados a pedidos.
    /// </summary>
    public string ExchangeName { get; set; } = "payment-gateway.orders";
}
