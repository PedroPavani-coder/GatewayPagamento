using FluentAssertions;
using PaymentGateway.Domain.Exceptions;
using PaymentGateway.Domain.ValueObjects;
using Xunit;

namespace PaymentGateway.Domain.Tests.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void Create_ComValorPositivo_DeveCriarMoneyValido()
    {
        // Arrange & Act
        var money = Money.Create(100, "BRL");

        // Assert
        money.Amount.Should().Be(100);
        money.Currency.Should().Be("BRL");
    }

    [Fact]
    public void Create_ComValorNegativo_DeveLancarDomainException()
    {
        // Arrange
        Action act = () => Money.Create(-10);

        // Act & Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*não pode ser negativo*");
    }

    [Fact]
    public void Add_ComMesmaMoeda_DeveSomarOsValores()
    {
        // Arrange
        var a = Money.Create(50, "BRL");
        var b = Money.Create(30, "BRL");

        // Act
        var resultado = a.Add(b);

        // Assert
        resultado.Amount.Should().Be(80);
    }

    [Fact]
    public void Add_ComMoedasDiferentes_DeveLancarDomainException()
    {
        // Arrange
        var reais = Money.Create(50, "BRL");
        var dolares = Money.Create(30, "USD");

        Action act = () => reais.Add(dolares);

        // Act & Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*moedas diferentes*");
    }

    [Fact]
    public void DoisMoneyComMesmoValorEMoeda_DevemSerIguais()
    {
        // Este teste prova a característica principal de um Value Object:
        // igualdade por VALOR, não por referência/identidade.
        var a = Money.Create(100, "BRL");
        var b = Money.Create(100, "BRL");

        (a == b).Should().BeTrue();
        a.Equals(b).Should().BeTrue();
    }
}
