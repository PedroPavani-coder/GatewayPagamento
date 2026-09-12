using Microsoft.EntityFrameworkCore;
using PaymentGateway.Application.Common.Interfaces;
using PaymentGateway.Domain.Orders;

namespace PaymentGateway.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação de verdade do IOrderRepository, usando EF Core.
/// Note que essa classe implementa uma interface que MORA na camada Application —
/// é exatamente essa inversão que permite trocar EF Core por outra tecnologia no
/// futuro sem mexer em nenhuma linha da Application ou do Domain.
/// </summary>
public class OrderRepository : IOrderRepository
{
    private readonly ApplicationDbContext _context;

    public OrderRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        // Isso só marca a entidade como "a ser inserida" no rastreador do EF Core.
        // Ela só vai realmente pro banco quando o SaveChangesAsync for chamado
        // (que acontece lá no UnitOfWork, depois que o Handler termina de orquestrar tudo).
        await _context.Orders.AddAsync(order, cancellationToken);
    }

    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Include("_items") é necessário porque, por padrão, o EF Core NÃO carrega
        // coleções relacionadas automaticamente (isso se chama "lazy loading desabilitado",
        // que é o padrão recomendado pra evitar armadilhas de performance).
        return await _context.Orders
            .Include("_items")
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }
}
