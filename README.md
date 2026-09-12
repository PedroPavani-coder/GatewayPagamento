# 💳 PaymentGateway

Meu projeto de portfolio: um gateway de pagamentos com checkout assíncrono, feito em .NET
estudando DDD (Domain-Driven Design) na prática. Tô construindo isso aos poucos, camada por
camada, pra realmente entender o porquê de cada decisão (e não só copiar um template pronto).

## Stack

- .NET 8
- SQL Server (banco de dados)
- RabbitMQ (fila de mensagens, pra processar pagamento de forma assíncrona)
- Docker (pra subir banco e fila sem precisar instalar nada na máquina)

## Como tá o progresso

- [x] Domain — as entidades e regras de negócio (o coração do sistema)
- [x] Testes unitários do Domain
- [x] Application — os casos de uso (o que a API realmente faz)
- [x] Infrastructure — banco de dados + fila de mensagens
- [ ] Api — os endpoints
- [ ] Worker — quem processa o pagamento em segundo plano
- [ ] Docker Compose — subir tudo junto com um comando só

## A ideia do projeto

É um checkout **assíncrono**. Isso quer dizer que quando o cliente confirma a compra, a
API não fica "travada" esperando o pagamento ser aprovado. Ela:

1. Recebe o pedido, salva como "pendente" e devolve resposta na hora (rápido!)
2. Manda uma mensagem pra uma fila (RabbitMQ)
3. Um "Worker" (um processo rodando em segundo plano) pega essa mensagem e processa o
   pagamento com calma
4. O cliente consulta depois se o pagamento foi aprovado ou não

Isso é bem parecido com como funciona de verdade em gateways de pagamento reais tipo
Stripe/PagSeguro — o pagamento raramente é instantâneo pro seu sistema, ele só te avisa
depois (via fila ou webhook).

## O que eu já fiz

### Camada Domain

Essa é a camada que não depende de nada — nem banco, nem API, nem nada externo. Só as
regras do negócio em C# puro. A ideia é que, se eu quiser trocar SQL Server por outro
banco, ou RabbitMQ por outra fila, essa camada aqui nem precisa mudar uma linha.

O que tem aqui:

- `Order` — o Pedido. É o "agregado principal": toda mudança de estado do pedido passa
  por um método dele (`AddItem`, `ConfirmCheckout`, `ApprovePayment`...). Isso evita que
  o pedido fique num estado esquisito, tipo "pago mas cancelado" ao mesmo tempo.
- `Money` — em vez de usar `decimal` solto pra representar valor, criei esse objeto que
  já garante que não existe valor negativo e não deixa somar Real com Dólar sem querer.
- Domain Events (`OrderCreatedEvent`, `PaymentApprovedEvent`, `PaymentDeclinedEvent`) —
  são "avisos" que o pedido dispara quando algo importante acontece. Vou usar isso depois
  pra saber quando publicar uma mensagem no RabbitMQ.

### Testes unitários

Criei testes pro `Order` e pro `Money` usando xUnit + FluentAssertions. Já apanhei um
bug real com isso (esqueci de atualizar o status do pedido depois de confirmar o
checkout 😅) — o que só reforça que vale a pena escrever teste mesmo em projeto pequeno.

### Camada Application

Aqui entram os "casos de uso" de verdade — o que a API vai permitir fazer. Usei o
**MediatR** pra separar tudo em Commands (mudam dado) e Queries (só leem), seguindo
CQRS. Por enquanto tem:

- `CreateOrderCommand` — recebe email do cliente + lista de itens, monta o `Order`
  usando o Domain, salva e devolve o resultado
- `GetOrderByIdQuery` — busca um pedido pelo Id

Também criei as interfaces `IOrderRepository`, `IUnitOfWork` e `IMessagePublisher`.
Repare que são só *contratos* — quem implementa de verdade (com EF Core e RabbitMQ)
vai ser a camada Infrastructure, no próximo passo. Isso é proposital: o Application
não sabe (e não precisa saber) qual banco ou qual fila tô usando por trás.

Escrevi testes pros dois Handlers usando **Moq** — assim consigo testar a lógica de
orquestração sem precisar de um banco de dados real rodando. O Moq cria uma versão
"falsa" do `IOrderRepository`, e eu só confiro se o Handler chamou ela do jeito certo.

### Camada Infrastructure

Aqui os contratos da Application viram código de verdade:

- `OrderRepository` — implementa `IOrderRepository` usando **Entity Framework Core**
- `UnitOfWork` — implementa `IUnitOfWork`, e além de salvar no banco, também publica os
  Domain Events acumulados pelo `Order` (tipo `OrderCreatedEvent`) na fila, logo depois
  que o `SaveChangesAsync` confirma que salvou com sucesso
- `RabbitMqMessagePublisher` — implementa `IMessagePublisher`, publicando as mensagens
  de verdade no RabbitMQ

O `Order` continua sem saber nada disso — ele só sabe que "aconteceu algo importante"
(o Domain Event). Quem decide o que fazer com isso é a Infrastructure.

Também precisei configurar o EF Core "na mão" (Fluent API, em vez de Data Annotations),
porque o `Order` foi desenhado com encapsulamento forte (lista de itens privada,
construtor privado). Isso dá mais trabalho, mas mantém o Domain limpo — sem nenhuma
"marca" de banco de dados espalhada nele.

⚠️ Nota de aprendizado: a forma como estou publicando os eventos (salvar no banco →
publicar na fila) funciona bem pra portfolio, mas não é 100% à prova de falhas. Se o
app cair bem entre essas duas etapas, o evento se perde. Em produção, o ideal é usar o
**Transactional Outbox Pattern**. Deixei isso anotado como próximo estudo depois que o
projeto estiver rodando ponta a ponta.

## Como rodar na sua máquina

Precisa ter o [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) instalado
(`dotnet --version` pra conferir).

```bash
# compilar
dotnet build

# rodar os testes
dotnet test
```

Pra rodar de fato com banco e fila, ainda falta o Passo 6 (Docker Compose) — por
enquanto a Infrastructure só compila, mas não tem como testar de ponta a ponta ainda
(falta a Api pra expor os endpoints).

## Próximo passo

Camada **Api**: os Controllers (ou Minimal APIs) que expõem os endpoints HTTP, chamando
os Commands/Queries da Application via MediatR, e o `Program.cs` juntando tudo
(`AddApplication()` + `AddInfrastructure()`).
