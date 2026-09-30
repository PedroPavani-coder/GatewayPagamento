using PaymentGateway.Application.Common.Interfaces;
using PaymentGateway.Domain.Common;

namespace PaymentGateway.IntegrationTests.Fakes;

/// <summary>
/// Substitui o RabbitMqMessagePublisher de verdade durante os testes de integração.
///
/// Por quê? Porque esse teste tem um objetivo específico: provar que a Api salva
/// corretamente no BANCO DE DADOS de ponta a ponta. Testar o RabbitMQ também exigiria
/// subir mais um container (RabbitMQ), o que deixaria o teste mais lento e menos
/// focado. Testar mensageria de verdade seria um próximo passo separado.
/// </summary>
public class FakeMessagePublisher : IMessagePublisher
{
    public List<IDomainEvent> PublishedEvents { get; } = new();

    public Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        // Só guarda o evento numa lista em memória, pra caso algum teste queira
        // verificar "esse evento foi publicado?" sem precisar de um RabbitMQ real.
        PublishedEvents.Add(domainEvent);
        return Task.CompletedTask;
    }
}
