using PaymentGateway.Domain.Exceptions;

namespace PaymentGateway.Api.Middleware;

/// <summary>
/// Middleware = um "filtro" que passa por TODA requisição HTTP que entra na Api.
/// Esse aqui envolve a chamada de qualquer Controller num try/catch, então a gente
/// não precisa repetir try/catch em cada endpoint.
///
/// Regra: DomainException (erro de regra de negócio, ex: "pedido sem itens") vira
/// HTTP 400. Qualquer outra exceção inesperada vira HTTP 500 — e nesse caso, NÃO
/// devolvemos os detalhes internos pro cliente (por segurança), só logamos.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Regra de negócio violada: {Mensagem}", ex.Message);

            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro não tratado ao processar a requisição");

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Ocorreu um erro interno. Tente novamente mais tarde."
            });
        }
    }
}

/// <summary>
/// Extension method só pra deixar o Program.cs mais limpo:
/// "app.UseExceptionHandling()" em vez de "app.UseMiddleware&lt;ExceptionHandlingMiddleware&gt;()".
/// </summary>
public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder app)
        => app.UseMiddleware<ExceptionHandlingMiddleware>();
}
