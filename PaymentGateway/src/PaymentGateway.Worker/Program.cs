using PaymentGateway.Application;
using PaymentGateway.Infrastructure;
using PaymentGateway.Worker.Consumers;

var builder = Host.CreateApplicationBuilder(args);

// Mesmo esquema que a Api: cada camada expõe seu "AddXxx()", o Worker só chama os dois.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// AddHostedService registra o Consumer como um serviço que roda em segundo plano
// durante toda a vida do processo, começando automaticamente quando o Worker inicia.
builder.Services.AddHostedService<OrderCreatedConsumer>();

var host = builder.Build();
host.Run();
