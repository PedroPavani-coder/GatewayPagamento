using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PaymentGateway.Application.Common.Interfaces;
using PaymentGateway.Infrastructure.Persistence;
using PaymentGateway.IntegrationTests.Fakes;
using Testcontainers.MsSql;
using Xunit;

namespace PaymentGateway.IntegrationTests;

/// <summary>
/// Essa classe é o coração dos testes de integração. Ela faz três coisas:
///
/// 1. Usa o Testcontainers pra subir um container Docker de SQL Server DE VERDADE,
///    do zero, antes de qualquer teste rodar (e derruba ele no final).
/// 2. Usa o WebApplicationFactory&lt;Program&gt; do ASP.NET Core pra hospedar a Api
///    INTEIRA em memória — com todos os Controllers, Middlewares, injeção de
///    dependência, tudo funcionando de verdade, só que sem precisar de um servidor
///    web real escutando numa porta.
/// 3. Troca a connection string real (a do appsettings.json, que aponta pro SQL
///    Server do docker-compose) pela connection string do container temporário, e
///    troca o RabbitMqMessagePublisher por um Fake (ver Fakes/FakeMessagePublisher.cs).
///
/// "IAsyncLifetime" é uma interface do xUnit que garante que InitializeAsync roda
/// ANTES dos testes, e DisposeAsync roda DEPOIS — perfeito pra "subir container,
/// roda os testes, derruba container".
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _sqlContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public FakeMessagePublisher MessagePublisher { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            // Sobrescreve a connection string do appsettings.json com a do container
            // temporário, que só existe durante a execução dos testes.
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _sqlContainer.GetConnectionString()
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove o IMessagePublisher real (que tentaria conectar num RabbitMQ de
            // verdade) e coloca o Fake no lugar.
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IMessagePublisher));
            if (descriptor is not null)
                services.Remove(descriptor);

            services.AddSingleton<IMessagePublisher>(MessagePublisher);
        });
    }

    public async Task InitializeAsync()
    {
        // Sobe o container do zero (baixa a imagem se ainda não tiver, cria, inicia,
        // e espera o SQL Server ficar pronto pra aceitar conexões).
        await _sqlContainer.StartAsync();

        // Aplica as Migrations no banco novinho do container, pra ele ficar com o
        // schema (tabelas Orders/OrderItems) igualzinho ao do ambiente real.
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await _sqlContainer.DisposeAsync();
    }
}
