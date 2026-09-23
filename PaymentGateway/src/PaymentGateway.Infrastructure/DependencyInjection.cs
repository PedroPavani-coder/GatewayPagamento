using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PaymentGateway.Application.Common.Interfaces;
using PaymentGateway.Infrastructure.Messaging;
using PaymentGateway.Infrastructure.Persistence;
using PaymentGateway.Infrastructure.Persistence.Repositories;

namespace PaymentGateway.Infrastructure;

/// <summary>
/// Centraliza o registro de tudo que a Infrastructure oferece. O Program.cs da Api
/// (próximo passo) só vai precisar chamar "services.AddInfrastructure(configuration)".
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.Configure<RabbitMqSettings>(configuration.GetSection(RabbitMqSettings.SectionName));

        // AddScoped: uma instância nova por requisição HTTP — faz sentido pro DbContext
        // e pro repositório/unit of work, que trabalham juntos numa mesma "unidade de trabalho".
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // AddSingleton: uma única instância pra aplicação inteira — faz sentido aqui porque
        // abrir uma conexão nova com o RabbitMQ a cada requisição seria caro e desnecessário.
        services.AddSingleton<IMessagePublisher, RabbitMqMessagePublisher>();

        return services;
    }
}
