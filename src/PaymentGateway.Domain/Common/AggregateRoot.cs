namespace PaymentGateway.Domain.Common;

/// <summary>
/// Um Agregado (Aggregate Root) é uma Entidade especial que serve como "porta de entrada"
/// para um conjunto de objetos relacionados (o "Aggregate").
///
/// Regra de ouro do DDD: código de fora SÓ pode modificar o estado através da raiz do agregado.
/// Exemplo: você nunca vai fazer "pedido.Itens.Add(novoItem)" diretamente de fora.
/// Você vai chamar "pedido.AdicionarItem(produto, quantidade)", e é o próprio Pedido
/// que garante que a operação é válida (ex: não pode adicionar item em pedido já pago).
///
/// Além disso, o AggregateRoot é responsável por acumular Domain Events, que serão
/// disparados depois que a operação for salva com sucesso no banco.
/// </summary>
public abstract class AggregateRoot : Entity
{
    private readonly List<IDomainEvent> _domainEvents = new();

    /// <summary>
    /// Lista somente-leitura dos eventos ainda não processados deste agregado.
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected AggregateRoot(Guid id) : base(id)
    {
    }

    protected AggregateRoot()
    {
    }

    /// <summary>
    /// Chamado internamente pelas próprias regras de negócio da entidade
    /// (ex: dentro do método Order.MarcarComoPago()) para registrar que algo aconteceu.
    /// </summary>
    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Chamado pela camada de Infraestrutura depois de salvar/publicar os eventos,
    /// para limpar a lista e evitar disparar o mesmo evento duas vezes.
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
