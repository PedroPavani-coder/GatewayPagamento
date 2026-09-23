using PaymentGateway.Domain.Common;

namespace PaymentGateway.Application.Common.Interfaces;

/// <summary>
/// Porta de saída para publicar Domain Events numa fila de mensagens.
///
/// Ainda não usamos essa interface nos handlers deste passo — vou introduzir o uso dela
/// quando chegarmos na Infrastructure (Passo 4), onde a implementação de verdade vai
/// publicar no RabbitMQ. Por enquanto ela só existe pra deixar claro qual vai ser o
/// próximo "encaixe" do sistema.
/// </summary>
public interface IMessagePublisher
{
    Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);
}
