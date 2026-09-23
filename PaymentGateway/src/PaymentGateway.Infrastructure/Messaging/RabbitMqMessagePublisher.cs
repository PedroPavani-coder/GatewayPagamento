using System.Text.Json;
using Microsoft.Extensions.Options;
using PaymentGateway.Application.Common.Interfaces;
using PaymentGateway.Domain.Common;
using RabbitMQ.Client;

namespace PaymentGateway.Infrastructure.Messaging;

/// <summary>
/// Implementação real do IMessagePublisher, usando RabbitMQ.
///
/// Conceitos do RabbitMQ que aparecem aqui:
/// - Connection: a conexão TCP com o servidor RabbitMQ
/// - Channel: um "canal" dentro da conexão (mais leve que abrir uma conexão nova a cada mensagem)
/// - Exchange: pra onde a mensagem é publicada. Usamos o tipo "Fanout", que significa
///   "manda essa mensagem pra TODAS as filas conectadas nesse exchange" — útil porque,
///   no futuro, tanto o Worker de pagamento quanto, digamos, um serviço de e-mail
///   poderiam "ouvir" o mesmo evento sem a gente mudar nada aqui.
/// </summary>
public class RabbitMqMessagePublisher : IMessagePublisher, IDisposable
{
    private readonly RabbitMqSettings _settings;
    private readonly IConnection _connection;
    private readonly IModel _channel;

    public RabbitMqMessagePublisher(IOptions<RabbitMqSettings> options)
    {
        _settings = options.Value;

        var factory = new ConnectionFactory
        {
            HostName = _settings.HostName,
            Port = _settings.Port,
            UserName = _settings.UserName,
            Password = _settings.Password
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        // "durable: true" garante que o exchange sobrevive se o RabbitMQ reiniciar.
        _channel.ExchangeDeclare(_settings.ExchangeName, ExchangeType.Fanout, durable: true);
    }

    public Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        // Usamos o nome da classe do evento (ex: "OrderCreatedEvent") como "routing key" —
        // isso ajuda bastante na hora de debugar, olhando os logs do RabbitMQ.
        var eventName = domainEvent.GetType().Name;
        var body = JsonSerializer.SerializeToUtf8Bytes(domainEvent, domainEvent.GetType());

        var properties = _channel.CreateBasicProperties();
        properties.Persistent = true; // mensagem sobrevive a um restart do RabbitMQ
        properties.Type = eventName;
        properties.ContentType = "application/json";

        _channel.BasicPublish(
            exchange: _settings.ExchangeName,
            routingKey: eventName,
            basicProperties: properties,
            body: body);

        // O cliente do RabbitMQ que estamos usando é síncrono por baixo dos panos,
        // então devolvemos um Task já concluído pra respeitar a interface assíncrona.
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _channel.Close();
        _connection.Close();
    }
}
