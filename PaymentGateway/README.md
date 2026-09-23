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
- [x] Api — os endpoints
- [x] Docker Compose — subir tudo junto com um comando só
- [x] Worker — quem processa o pagamento em segundo plano

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

### Camada Api

Agora sim, os endpoints HTTP de verdade:

- `POST /api/orders` — cria um pedido e já confirma o checkout
- `GET /api/orders/{id}` — consulta o status atual de um pedido

O `OrdersController` é bem enxuto de propósito: ele só converte o request HTTP num
Command/Query do MediatR e devolve a resposta. Nenhuma regra de negócio mora aqui.

Também adicionei um `ExceptionHandlingMiddleware`, que fica "escutando" qualquer
exceção que role em qualquer requisição:
- `DomainException` (erro de regra de negócio) → vira HTTP 400 com uma mensagem clara
- Qualquer outro erro inesperado → vira HTTP 500 genérico (sem expor detalhes internos)

O `Program.cs` é onde tudo se junta: `AddApplication()` + `AddInfrastructure()` +
Controllers + Swagger.

### Docker Compose

Criei um `docker-compose.yml` na raiz do projeto que sobe:
- **SQL Server** (via imagem do Azure SQL Edge, porta 1433) — mesma senha que já tá
  no `appsettings.json`. Optei pelo Azure SQL Edge em vez do SQL Server 2022 "cheio"
  porque ele é mais leve e mais estável rodando em Docker Desktop/WSL2 no Windows
  (cheguei a apanhar de um crash no container do SQL Server 2022 puro). Pra tudo que
  a gente precisa aqui (T-SQL, EF Core), o comportamento é idêntico.
- **RabbitMQ** com o painel de administração (porta 15672) — dá pra acessar
  `http://localhost:15672` (usuário/senha: `guest`/`guest`) e ver as filas e
  mensagens na telinha, o que ajuda MUITO a entender o que tá acontecendo.

Os dados dos dois ficam salvos em **volumes nomeados do Docker** (gerenciados pelo
próprio Docker, não numa pasta visível do projeto) — isso evita problemas de
compatibilidade entre o SQL Server e o sistema de arquivos do Windows.

## Como rodar na sua máquina (do zero)

Pré-requisitos: [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) e
[Docker Desktop](https://www.docker.com/products/docker-desktop/) instalados e rodando.

**1. Suba o banco e a fila:**
```bash
docker compose up -d
```
(o `-d` roda em segundo plano; confira com `docker ps` se os dois containers subiram)

**2. Instale a ferramenta de linha de comando do EF Core** (só precisa fazer isso uma vez):
```bash
dotnet tool install --global dotnet-ef
```

**3. Crie a Migration** (o "script" que descreve as tabelas a serem criadas):
```bash
dotnet ef migrations add InitialCreate --project src/PaymentGateway.Infrastructure --startup-project src/PaymentGateway.Api
```

**4. Aplique a Migration no banco** (agora sim cria as tabelas de verdade):
```bash
dotnet ef database update --project src/PaymentGateway.Infrastructure --startup-project src/PaymentGateway.Api
```

**5. Abra DOIS terminais** (a Api e o Worker precisam rodar ao mesmo tempo):

Terminal 1 — a Api:
```bash
dotnet run --project src/PaymentGateway.Api
```

Terminal 2 — o Worker:
```bash
dotnet run --project src/PaymentGateway.Worker
```

Isso deve abrir o navegador direto no Swagger (`/swagger`), onde dá pra testar o
`POST /api/orders` e o `GET /api/orders/{id}` sem precisar de Postman.

**6. Faça login pra conseguir um token** (os endpoints de pedido agora exigem
autenticação — veja a seção "Autenticação JWT" abaixo):
- No Swagger, abra `POST /api/auth/login` e envie `{"userName": "admin", "password": "admin123"}`
- Copie o `token` da resposta
- Clique no botão **Authorize** (cadeado, no topo da página do Swagger) e cole o token

**7. Teste o fluxo completo de checkout assíncrono:**
- Crie um pedido pelo Swagger (`POST /api/orders`)
- Olhe o terminal do **Worker**: em alguns segundos devem aparecer os logs
  "Mensagem recebida" e "APROVADO"/"RECUSADO"
- Consulte o pedido de novo (`GET /api/orders/{id}`) — o `status` deve ter mudado
  de `Confirmed` pra `Paid` ou `Declined`!

**8. (opcional) Rode os testes automatizados também:**
```bash
dotnet test
```

### Worker

Essa é a peça que **fecha o ciclo** do checkout assíncrono. É um projeto separado
(`PaymentGateway.Worker`), do tipo Worker Service — ele não tem endpoint HTTP nenhum,
só fica rodando em segundo plano, escutando a fila.

O que ele faz:
1. Conecta no RabbitMQ e cria uma fila vinculada ao mesmo exchange que a Api publica
2. Fica esperando mensagens `OrderCreatedEvent` chegarem
3. Pra cada mensagem, dispara o `ProcessOrderPaymentCommand` (um Command novo criado
   na Application) — que simula uma checagem com um provedor de pagamento externo
   (2 segundos de espera + 80% de chance de aprovar) e chama `order.ApprovePayment()`
   ou `order.DeclinePayment()`
4. Salva via `UnitOfWork` — que, como já configuramos na Infrastructure, também
   publica os eventos `PaymentApprovedEvent`/`PaymentDeclinedEvent` de volta na fila

Detalhe de arquitetura que vale destacar: criei uma classe `OrderCreatedMessage`
separada, só pra representar "o formato da mensagem que trafega na fila" — em vez de
reaproveitar direto a classe `OrderCreatedEvent` do Domain. Isso evita acoplar o
"contrato entre sistemas" com uma classe interna que pode mudar por outros motivos.

### Autenticação JWT

Os endpoints de `/api/orders` agora exigem um token JWT válido — sem ele, a Api
devolve `401 Unauthorized`.

Como funciona:
1. `POST /api/auth/login` recebe usuário e senha, e devolve um token (string longa e
   codificada) + a data de expiração
2. Esse token precisa ser enviado no header `Authorization: Bearer {token}` em toda
   chamada pros endpoints de pedidos
3. A Api confere a assinatura digital do token (usando a mesma chave secreta que o
   gerou) pra confirmar que ele é legítimo e não expirou — sem precisar consultar
   banco de dados nenhum a cada requisição

⚠️ **Simplificação proposital**: como o projeto não tem uma tabela de Usuários, o
login usa uma credencial fixa (`admin` / `admin123`), só pra demonstrar o mecanismo
de autenticação funcionando de ponta a ponta. Numa aplicação real, o `AuthController`
consultaria uma tabela de usuários no banco, com senha armazenada com hash (nunca em
texto puro) — o ideal seria usar o ASP.NET Core Identity pra isso.

Também vale registrar: a chave secreta do JWT (`Jwt:Key` no `appsettings.json`) está
commitada no repositório só porque é uma chave de **desenvolvimento local**. Numa
aplicação de produção, isso jamais deveria estar no controle de versão — viria de uma
variável de ambiente ou de um cofre de segredos.

## Ideias pra evoluir ainda mais

- CI/CD com GitHub Actions rodando os testes a cada push
- Deploy real (Railway/Render) com link no README
- Testes de integração com Testcontainers
- Transactional Outbox Pattern (mencionado lá na seção da Infrastructure)
- Tabela de Usuários de verdade (ASP.NET Core Identity) no lugar do login fixo
