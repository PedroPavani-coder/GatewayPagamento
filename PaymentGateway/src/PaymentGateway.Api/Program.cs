using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PaymentGateway.Api.Auth;
using PaymentGateway.Api.Middleware;
using PaymentGateway.Application;
using PaymentGateway.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ===== Registro de serviços =====

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Isso adiciona um botão "Authorize" no topo do Swagger, onde dá pra colar o
    // token JWT uma vez só, e ele passa a ser enviado automaticamente em toda
    // chamada que você testar por ali.
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Cole aqui só o token (sem escrever a palavra 'Bearer' na frente)."
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// Repare como fica simples: cada camada expõe seu próprio "AddXxx()", e o Program.cs
// só precisa chamar os dois, sem saber o que tem dentro de cada uma.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// ===== Autenticação JWT =====

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
builder.Services.AddScoped<ITokenService, TokenService>();

var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
                   ?? new JwtSettings();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Aqui a Api define as regras pra considerar um token válido: precisa ter
        // sido assinado com a MESMA chave secreta, ter o Issuer/Audience corretos,
        // e ainda não ter expirado.
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
        };
    });

builder.Services.AddAuthorization();

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

// UseAuthentication PRECISA vir antes de UseAuthorization: primeiro a Api descobre
// "quem é você" (lendo e validando o token), só depois decide "você pode fazer isso?".
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
