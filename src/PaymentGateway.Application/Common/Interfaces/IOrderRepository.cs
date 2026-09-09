using PaymentGateway.Domain.Orders;

namespace PaymentGateway.Application.Common.Interfaces;

/// <summary>
/// Contrato para persistência do agregado Order.
///
/// Repare que essa interface mora na camada Application, mas quem vai IMPLEMENTAR ela de
/// verdade é a camada Infrastructure (usando EF Core, no próximo passo). Isso é o
/// princípio de "Inversão de Dependência" do DDD: a camada de mais alto nível (Application)
/// dita a regra, e a camada de mais baixo nível (Infrastructure) obedece.
///
/// Vantagem prática: pra testar o CreateOrderCommandHandler, eu não preciso de um banco
/// de dados de verdade — só preciso de uma implementação falsa (um "mock") dessa interface.
/// </summary>
public interface IOrderRepository
{
    Task AddAsync(Order order, CancellationToken cancellationToken = default);

    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
