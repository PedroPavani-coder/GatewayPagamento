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
- [ ] Application — os casos de uso (o que a API realmente faz)
- [ ] Infrastructure — banco de dados + fila de mensagens
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

## Como rodar na sua máquina

Precisa ter o [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) instalado
(`dotnet --version` pra conferir).

```bash
# compilar
dotnet build

# rodar os testes
dotnet test
```

## Próximo passo

Vou começar a camada **Application** — onde entram os "casos de uso" (tipo
`CriarPedido`, `ConfirmarCheckout`) que vão conectar a API com o Domain. É aqui que
também entra o padrão Repository.
