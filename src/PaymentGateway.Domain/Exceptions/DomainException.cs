namespace PaymentGateway.Domain.Exceptions;

/// <summary>
/// Exceção lançada quando uma regra de negócio do domínio é violada.
/// Ex: tentar aprovar pagamento de um pedido que já foi cancelado.
///
/// Usamos uma exceção específica (em vez de Exception genérica) para que,
/// lá na API, a gente consiga capturar SÓ esse tipo de erro e devolver
/// um HTTP 400 (Bad Request) com uma mensagem amigável, em vez de um 500 genérico.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }
}
