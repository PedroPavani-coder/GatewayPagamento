# PaymentGateway — Checkout Assíncrono (DDD)

Projeto de portfolio: um gateway de pagamentos com checkout assíncrono, construído em .NET
seguindo os princípios de Domain-Driven Design (DDD).

## Stack planejada
- **.NET 8**
- **SQL Server** (persistência)
- **RabbitMQ** (mensageria assíncrona)
- **Docker** (para subir SQL Server e RabbitMQ localmente)

## Status do projeto (vamos evoluindo aos poucos)

- [x] **Passo 1 — Camada Domain**: entidades, value objects e regras de negócio puras
- [x] **Passo 2 — Testes unitários** do Domain (xUnit + FluentAssertions)
- [ ] Passo 3 — Camada Application (casos de uso / CQRS)
- [ ] Passo 4 — Camada Infrastructure (EF Core + RabbitMQ)
- [ ] Passo 5 — Camada Api (controllers)
- [ ] Passo 6 — Worker de processamento assíncrono
- [ ] Passo 7 — Docker Compose (SQL Server + RabbitMQ + Api + Worker)

## O que foi feito no Passo 1

Criamos o projeto `PaymentGateway.Domain`, que é o núcleo do sistema. Ele **não depende de
nada externo** (nem EF Core, nem RabbitMQ, nem ASP.NET) — só de C# puro. Isso é proposital:
no DDD, o Domain é o coração do sistema, e ele não pode saber COMO os dados são
persistidos ou COMO as mensagens são enviadas, apenas O QUE deve acontecer.

### Estrutura criada

```
src/PaymentGateway.Domain/
├── Common/
│   ├── Entity.cs          → Classe base para entidades (identidade por Id)
│   ├── AggregateRoot.cs   → Classe base para agregados (gerencia Domain Events)
│   ├── ValueObject.cs     → Classe base para objetos de valor (igualdade por valor)
│   └── IDomainEvent.cs    → Contrato para eventos de domínio
├── Exceptions/
│   └── DomainException.cs → Exceção para violação de regras de negócio
├── ValueObjects/
│   └── Money.cs           → Valor monetário seguro (evita valores negativos, mistura de moedas)
└── Orders/
    ├── Order.cs           → Aggregate Root: o Pedido, com todas as regras de checkout
    ├── OrderItem.cs       → Entidade filha do agregado Order
    ├── OrderStatus.cs     → Enum com os estados possíveis do pedido
    └── Events/
        ├── OrderCreatedEvent.cs
        ├── PaymentApprovedEvent.cs
        └── PaymentDeclinedEvent.cs
```

### Fluxo de negócio já modelado

```
Order.Create()        → cria pedido com status PendingPayment
Order.AddItem()       → adiciona itens (só antes de confirmar)
Order.ConfirmCheckout() → valida e dispara OrderCreatedEvent
Order.StartProcessing() → Worker pega da fila (PendingPayment -> Processing)
Order.ApprovePayment()  → Processing -> Paid (dispara PaymentApprovedEvent)
Order.DeclinePayment()  → Processing -> Declined (dispara PaymentDeclinedEvent)
Order.Cancel()          → cancela, se ainda não foi processado
```

## O que foi feito no Passo 2

Criamos o projeto `PaymentGateway.Domain.Tests`, usando:
- **xUnit**: framework de testes (atributos `[Fact]` para um teste único, `[Theory]` +
  `[InlineData]` para rodar o mesmo teste com várias entradas diferentes)
- **FluentAssertions**: deixa as asserções mais legíveis (`resultado.Should().Be(x)`
  em vez de `Assert.Equal(x, resultado)`)

### Estrutura criada

```
tests/PaymentGateway.Domain.Tests/
├── PaymentGateway.Domain.Tests.csproj
├── ValueObjects/
│   └── MoneyTests.cs   → testa criação, soma e igualdade do Value Object Money
└── Orders/
    └── OrderTests.cs   → testa todo o ciclo de vida do agregado Order
```

Os testes de `OrderTests.cs` cobrem:
- Criação válida e inválida de pedido
- Adição de itens e cálculo do total
- Regra de que não dá pra confirmar checkout sem itens
- Regra de que não dá pra editar pedido já confirmado
- Transições de status (Pending → Processing → Paid / Declined)
- Que os Domain Events corretos são disparados em cada transição
- Regra de que não dá pra cancelar pedido já pago

## Como rodar/verificar este passo na sua máquina

1. Instale o [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (confira com `dotnet --version`)
2. Descompacte este projeto em uma pasta
3. Na raiz (`PaymentGateway/`), rode:
   ```bash
   dotnet build
   ```
4. Para rodar os testes:
   ```bash
   dotnet test
   ```
   Você deve ver algo como `Passed! - Failed: 0, Passed: 17` no final. Isso confirma
   que todas as regras de negócio estão se comportando como esperado.

## Próximo passo

Seguimos para a camada **Application**, onde vamos criar os casos de uso (Commands/Queries)
que vão orquestrar o Domain — por exemplo, um `CriarPedidoCommandHandler` que recebe um
DTO da API, monta o `Order`, e chama um repositório (que ainda não existe — só a
interface, seguindo o princípio de Inversão de Dependência do DDD).
