using PaymentGateway.Domain.Common;
using PaymentGateway.Domain.Exceptions;

namespace PaymentGateway.Domain.ValueObjects;

/// <summary>
/// Value Object que representa um valor monetário.
///
/// Por que não usar simplesmente um "decimal" no Pedido?
/// Porque um decimal sozinho não sabe se é Real, Dólar ou Euro, e não impede
/// valores negativos. Encapsulando essa regra aqui, ela fica centralizada
/// em UM lugar só, em vez de espalhada por vários "if" pelo sistema.
/// </summary>
public sealed class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    /// <summary>
    /// Factory method: forma controlada de criar um Money válido.
    /// Preferimos isso a um construtor público para poder validar antes de existir o objeto.
    /// </summary>
    public static Money Create(decimal amount, string currency = "BRL")
    {
        if (amount < 0)
            throw new DomainException("O valor monetário não pode ser negativo.");

        if (string.IsNullOrWhiteSpace(currency))
            throw new DomainException("A moeda é obrigatória.");

        return new Money(amount, currency.ToUpperInvariant());
    }

    public static Money Zero(string currency = "BRL") => new(0, currency);

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Multiply(int factor)
    {
        return new Money(Amount * factor, Currency);
    }

    private void EnsureSameCurrency(Money other)
    {
        if (Currency != other.Currency)
            throw new DomainException($"Não é possível operar valores em moedas diferentes ({Currency} e {other.Currency}).");
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    public override string ToString() => $"{Currency} {Amount:N2}";
}
