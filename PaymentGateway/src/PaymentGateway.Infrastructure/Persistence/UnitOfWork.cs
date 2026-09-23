using Microsoft.EntityFrameworkCore;
using PaymentGateway.Application.Common.Interfaces;
using PaymentGateway.Domain.Common;

namespace PaymentGateway.Infrastructure.Persistence;

/// <summary>
/// Implementação do IUnitOfWork. Além de simplesmente chamar SaveChangesAsync,
/// essa classe também é responsável por publicar os Domain Events que os agregados
/// acumularam (ex: OrderCreatedEvent) — e faz isso SÓ depois que o banco confirmou
/// que salvou com sucesso. Isso evita publicar um evento sobre algo que, no final,
/// não foi realmente persistido.
///
/// Nota de aprendizado: essa abordagem é simples e funciona bem pra portfolio, mas não
/// é 100% à prova de falhas (ex: se o app cair bem entre o SaveChanges e o Publish, o
/// evento se perde). Em produção, times costumam usar o "Transactional Outbox Pattern"
/// pra garantir entrega. Fica como próximo passo de estudo depois que o projeto rodar.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private readonly IMessagePublisher _messagePublisher;

    public UnitOfWork(ApplicationDbContext context, IMessagePublisher messagePublisher)
    {
        _context = context;
        _messagePublisher = messagePublisher;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // 1. Antes de salvar, capturamos quais agregados (rastreados pelo EF Core nessa
        // unidade de trabalho) têm eventos de domínio pendentes.
        var aggregatesComEventos = _context.ChangeTracker
            .Entries<AggregateRoot>()
            .Select(entry => entry.Entity)
            .Where(aggregate => aggregate.DomainEvents.Any())
            .ToList();

        // 2. Agora sim, persistimos de verdade no banco.
        await _context.SaveChangesAsync(cancellationToken);

        // 3. Só depois do SaveChanges ter dado certo é que publicamos os eventos.
        foreach (var aggregate in aggregatesComEventos)
        {
            foreach (var domainEvent in aggregate.DomainEvents)
            {
                await _messagePublisher.PublishAsync(domainEvent, cancellationToken);
            }

            // Limpamos pra não publicar o mesmo evento de novo numa próxima chamada.
            aggregate.ClearDomainEvents();
        }
    }
}
