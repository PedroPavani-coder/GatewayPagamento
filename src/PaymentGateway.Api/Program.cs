using PaymentGateway.Api.Middleware;
using PaymentGateway.Application;
using PaymentGateway.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ===== Registro de serviços =====

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Repare como fica simples: cada camada expõe seu próprio "AddXxx()", e o Program.cs
// só precisa chamar os dois, sem saber o que tem dentro de cada uma.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// ===== Pipeline HTTP (a ordem aqui importa!) =====

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Precisa vir bem no início, pra capturar exceções de qualquer coisa que rodar depois.
app.UseExceptionHandling();

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
