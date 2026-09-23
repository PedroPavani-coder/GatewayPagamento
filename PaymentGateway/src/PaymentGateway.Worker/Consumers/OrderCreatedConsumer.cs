using System.Text;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PaymentGateway.Application.Orders.Commands.ProcessOrderPayment;
using PaymentGateway.Infrastructure.Messaging;
using PaymentGateway.Worker.Messages;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace PaymentGateway.Worker.Consumers;

/// <summary>
/// Um BackgroundService roda em segundo plano durante toda a vida da aplicação —
/// é o jeito padrão do .NET de fazer "processos que ficam vivos escutando algo",
/// diferente de uma Api que só responde quando alguém chama um endpoint.
///
/// Fluxo desse Consumer:
/// 1. Conecta no RabbitMQ e declara uma FILA vinculada ao mesmo exchange que a Api usa
/// 2. Fica esperando mensagens chegarem
/// 3. Pra cada mensagem: desserializa, dispara o ProcessOrderPaymentCommand via MediatR,
///    e só confirma o recebimento (ACK) se der tudo certo
/// </summary>
public class OrderCreatedConsumer : BackgroundService
{
    private const string QueueName = "payment-processing.order-created";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMqSettings _settings;
    private readonly ILogger<OrderCreatedConsumer> _logger;

    private IConnection? _connection;
    private IModel? _channel;

    public OrderCreatedConsumer(
        IServiceScopeFactory scopeFactory,
        IOptions<RabbitMqSettings> options,
        ILogger<OrderCreatedConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _settings = options.Value;
        _logger = logger;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _settings.HostName,
            Port = _settings.Port,
            UserName = _settings.UserName,
            Password = _settings.Password
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        // Precisa declarar o exchange aqui também (mesmo nome/tipo que a Api usa),
        // porque não temos garantia de qual dos dois processos (Api ou Worker) sobe primeiro.
        _channel.ExchangeDeclare(_settings.ExchangeName, ExchangeType.Fanout, durable: true);

        // "durable: true" garante que a fila sobrevive a um restart do RabbitMQ, e as
        // mensagens que ainda não foram processadas não se perdem.
        _channel.QueueDeclare(QueueName, durable: true, exclusive: false, autoDelete: false);

        // Como o exchange é do tipo Fanout, a routing key é ignorada — a fila recebe
        // TODAS as mensagens publicadas nesse exchange.
        _channel.QueueBind(QueueName, _settings.ExchangeName, routingKey: string.Empty);

        _logger.LogInformation("Conectado ao RabbitMQ, aguardando mensagens na fila {QueueName}", QueueName);

        return base.StartAsync(cancellationToken);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumer = new EventingBasicConsumer(_channel);

        consumer.Received += async (_, eventArgs) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
                var message = JsonSerializer.Deserialize<OrderCreatedMessage>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (message is not null)
                {
                    _logger.LogInformation("Mensagem recebida: pedido {OrderId}", message.OrderId);

                    // O BackgroundService é Singleton, mas o MediatR/DbContext/Repository são
                    // Scoped. Por isso criamos um "escopo" novo pra cada mensagem — é o
                    // equivalente, aqui no Worker, a "uma requisição HTTP" lá na Api.
                    using var scope = _scopeFactory.CreateScope();
                    var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                    await mediator.Send(new ProcessOrderPaymentCommand(message.OrderId), stoppingToken);
                }

                // ACK = "recebi e processei com sucesso, pode remover da fila".
                _channel!.BasicAck(eventArgs.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar mensagem da fila");

                // NACK com requeue:true = "deu erro, devolve pra fila pra tentar de novo depois"
                // (em produção, o ideal seria ter um limite de tentativas + uma "dead-letter queue",
                // pra não ficar reprocessando pra sempre uma mensagem que sempre vai falhar).
                _channel!.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: true);
            }
        };

        _channel!.BasicConsume(queue: QueueName, autoAck: false, consumer: consumer);

        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        base.Dispose();
    }
}
