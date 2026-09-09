namespace PaymentGateway.Application.Common.Interfaces;

/// <summary>
/// Representa "uma transação de banco de dados".
///
/// Por que separar isso do Repository? Porque um caso de uso pode mexer em MAIS de um
/// repositório (ex: salvar um Order E atualizar um Estoque) e precisamos garantir que
/// tudo seja salvo junto, ou nada seja salvo (atomicidade). O SaveChangesAsync é o
/// momento em que, de fato, "commitamos" tudo isso no banco.
///
/// Na prática, quando implementarmos com EF Core, isso vai ser basicamente um wrapper
/// em torno do DbContext.SaveChangesAsync().
/// </summary>
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
