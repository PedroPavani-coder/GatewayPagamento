# Testes de carga (k6)

Aqui ficam os scripts que testam **quanta pancada a Api aguenta**. Uso o
[k6](https://grafana.com/docs/k6/latest/), uma ferramenta gratuita e open source pra
isso. Os scripts são em JavaScript, mas não precisa saber JS pra rodar.

> ⚠️ **Só rode isso contra a sua própria Api, na sua máquina.** Nunca aponte teste de
> carga pra sistema de terceiros.

## O que tem aqui

| Arquivo | O que faz |
|---|---|
| `load-test.js` | **Teste de carga**: sobe de 0 a 50 usuários virtuais aos poucos, com pausa de 1s entre rodadas (uso realista e intenso). Pergunta: "a Api se comporta bem sob o movimento esperado?" |
| `stress-test.js` | **Teste de estresse**: sobe até 300 usuários **sem pausa nenhuma**. Pergunta: "onde está o limite, e o que acontece quando passa dele?" |
| `helpers.js` | Código compartilhado: faz o login (JWT) e o fluxo "criar pedido + consultar pedido" |

Cada "rodada" de um usuário virtual faz: `POST /api/orders` → `GET /api/orders/{id}`.

## 1. Instalar o k6 (Windows)

No PowerShell:

```powershell
winget install k6 --source winget
```

Feche e abra o terminal e confira:

```powershell
k6 version
```

(Alternativas: `choco install k6`, ou baixar o instalador oficial na página de releases
do k6 no GitHub.)

## 2. Preparar o ambiente

**a) Suba o banco e a fila**
```powershell
docker compose up -d
```

**b) Rode a Api em modo Release e sem o log "tagarela" do Development**

Medir desempenho com a Api em Debug, jogando log de cada comando SQL no console, distorce
tudo. Por isso, pra esse teste:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Production"
$env:ASPNETCORE_URLS = "http://localhost:5000"
dotnet run -c Release --no-launch-profile --project src/PaymentGateway.Api
```

(Em Production o Swagger fica desligado, mas pra teste de carga a gente não precisa dele.)

**c) Deixe o Worker DESLIGADO durante o teste**

A ideia aqui é medir a **Api**. Com o Worker ligado, cada pedido criado dispararia um
processamento de 2s, e o resultado ficaria misturado. As mensagens simplesmente
acumulam na fila do RabbitMQ (se ela já existir), sem problema nenhum.

## 3. Rodar

Em **outro terminal**, na raiz do projeto:

```powershell
k6 run loadtests/load-test.js
```

Dura uns 3 minutos e meio. Depois, o de estresse:

```powershell
k6 run loadtests/stress-test.js
```

## 4. Como ler o resultado

No final, o k6 imprime um resumo. Os números que mais importam:

| Métrica | O que significa |
|---|---|
| `http_req_duration` | Quanto tempo cada requisição levou. Olhe o **`p(95)`**: "95% das requisições foram mais rápidas que isso". É mais honesto que a média, porque mostra a experiência dos usuários mais azarados |
| `http_req_failed` | Porcentagem de requisições que deram erro (status 4xx/5xx ou falha de conexão) |
| `http_reqs` | Total de requisições e quantas por segundo (**throughput**) |
| `erros_de_fluxo` | Métrica própria: falhas em criar/consultar pedido |
| `checks` | Quantas verificações (status 201/200) passaram |

As linhas `✓` e `✗` ao lado das **thresholds** dizem se as metas foram cumpridas.

## 5. Limpar a bagunça depois

O teste cria milhares de pedidos no banco e (se a fila existir) milhares de mensagens
no RabbitMQ.

- **Fila**: abra `http://localhost:15672` → aba **Queues** → clique em
  `payment-processing.order-created` → **Purge Messages**. Se não limpar, quando você
  ligar o Worker de novo ele vai processar todos esses pedidos, 2s por vez.
- **Banco**: pra zerar tudo, `docker compose down -v` e depois suba de novo e rode o
  `dotnet ef database update` outra vez.

## ⚠️ Sobre os números

O k6, a Api, o SQL Server e o RabbitMQ estão todos rodando **na mesma máquina**
disputando CPU e memória. Os números absolutos vão ser mais baixos do que seriam num
servidor de verdade. O valor está em **comparar antes e depois** de cada melhoria
(por exemplo, antes e depois do rate limiting), na mesma máquina e nas mesmas condições.

## Resultados

_Preencha depois de rodar, pra ter o registro de cada fase do projeto._

| Cenário | Usuários (pico) | Requisições/s | p(95) | Falhas | Observações |
|---|---|---|---|---|---|
| Carga — antes do rate limiting | 50 | | | | |
| Estresse — antes do rate limiting | 300 | | | | |
