namespace PaymentGateway.Domain.Common;

/// <summary>
/// Classe base para todas as Entidades do domínio.
///
/// No DDD, uma Entidade é definida pela sua IDENTIDADE (o Id), não pelos seus atributos.
/// Ou seja: dois pedidos com os mesmos dados, mas Ids diferentes, são pedidos DIFERENTES.
/// Já dois pedidos com o mesmo Id são o MESMO pedido, mesmo que os dados tenham mudado.
///
/// Isso é diferente de um Value Object (que veremos em ValueObject.cs), onde a igualdade
/// é baseada nos valores, não em um identificador.
/// </summary>
public abstract class Entity
{
    public Guid Id { get; protected set; }

    protected Entity(Guid id)
    {
        Id = id;
    }

    // Construtor protegido sem parâmetros, exigido pelo Entity Framework Core
    // para conseguir "materializar" (reconstruir) o objeto vindo do banco de dados.
    protected Entity()
    {
    }

    public override bool Equals(object? obj)
    {
        if (obj is not Entity other)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        if (GetType() != other.GetType())
            return false;

        return Id == other.Id;
    }

    public static bool operator ==(Entity? left, Entity? right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    public static bool operator !=(Entity? left, Entity? right) => !(left == right);

    public override int GetHashCode() => (GetType().ToString() + Id).GetHashCode();
}
