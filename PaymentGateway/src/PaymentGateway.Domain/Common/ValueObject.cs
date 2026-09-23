namespace PaymentGateway.Domain.Common;

/// <summary>
/// Classe base para Value Objects (Objetos de Valor).
///
/// Diferente de uma Entidade, um Value Object NÃO tem identidade própria.
/// Ele é definido inteiramente pelos seus valores.
///
/// Exemplo: "R$ 100,00" e "R$ 100,00" são o MESMO valor monetário, não importa
/// "qual instância" é qual. Já dois Pedidos com o mesmo valor total continuam
/// sendo pedidos diferentes (porque Pedido é uma Entidade, tem Id).
///
/// Value Objects também costumam ser IMUTÁVEIS: uma vez criados, não mudam.
/// Se precisar de um valor diferente, você cria um novo objeto.
/// </summary>
public abstract class ValueObject
{
    /// <summary>
    /// Cada classe filha informa quais campos compõem sua igualdade.
    /// Ex: Money vai retornar [Valor, Moeda].
    /// </summary>
    protected abstract IEnumerable<object?> GetEqualityComponents();

    public override bool Equals(object? obj)
    {
        if (obj is not ValueObject other)
            return false;

        if (GetType() != other.GetType())
            return false;

        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Select(x => x?.GetHashCode() ?? 0)
            .Aggregate((x, y) => x ^ y);
    }

    public static bool operator ==(ValueObject? left, ValueObject? right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    public static bool operator !=(ValueObject? left, ValueObject? right) => !(left == right);
}
