using Microsoft.Extensions.DependencyInjection;

namespace PaymentGateway.Application;

/// <summary>
/// Centraliza o registro dos serviços da camada Application na injeção de dependência.
/// A ideia é que o Program.cs da Api só precise chamar "services.AddApplication()",
/// sem precisar saber o que tem dentro (MediatR, Handlers, etc.). Isso mantém a Api
/// desacoplada dos detalhes internos de cada camada.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        return services;
    }
}
