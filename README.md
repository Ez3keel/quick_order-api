# QuickOrder

Sistema de pedidos e entrega (estilo *food delivery*) construído como um conjunto de
**microsserviços .NET 10**, para praticar os problemas reais de sistemas distribuídos:
consistência eventual, mensageria assíncrona confiável, falhas parciais e
observabilidade de ponta a ponta.

O projeto nasce com Outbox Pattern + idempotência como diferencial técnico central —
não é um CRUD com fila anexada por cima, é um sistema desenhado desde o domínio para
nunca perder nem duplicar um evento entre os limites de serviço.

> O porquê de cada decisão relevante — não só o que foi feito, mas o motivo — está
> registrado em [docs/DECISOES-DE-ARQUITETURA.md](docs/DECISOES-DE-ARQUITETURA.md).
> O desenho geral da arquitetura (diagramas, fluxo de eventos, mapa de portas) está em
> [docs/ARQUITETURA.md](docs/ARQUITETURA.md).

## Serviços

| Serviço | Tipo | Responsabilidade |
|---|---|---|
| **Identity** | API HTTP | Registro, login, JWT + refresh token com rotação e detecção de reuso |
| **Catalog** | API HTTP | Restaurantes e cardápio, cache-aside no Redis |
| **Ordering** | API HTTP | Ciclo de vida do pedido (máquina de estado), valida cardápio via HTTP no Catalog |
| **Delivery.Api** | API HTTP | Cadastro e disponibilidade de entregadores |
| **Delivery.Worker** | Worker | Consome fila de pedidos prontos e atribui entregador (consumidor único, sem lock) |
| **Notification** | API HTTP + SignalR | Empurra atualizações em tempo real (pedido, entregador) para o cliente conectado |

Cada serviço com domínio próprio (todos exceto Notification) segue Clean Architecture:
`Domain` → `Application` → `Infrastructure` → `Api`/`Worker`, com repositório + Unit of
Work e eventos de domínio capturados no Outbox dentro da própria transação do EF Core.

## Stack

- **.NET 10 / C#**, ASP.NET Core, EF Core + PostgreSQL (um schema por serviço)
- **RabbitMQ** — Outbox Pattern, exchanges topic por serviço, consumidores idempotentes,
  fila de delay + DLQ com contagem de tentativas explícita
- **Redis** — cache-aside (Catalog), índice geoespacial de entregadores (Delivery),
  backplane do SignalR + dedup de eventos (Notification)
- **SignalR** — rastreamento de pedido/entregador em tempo real
- **JWT (HS256)** — autenticação/autorização por papel, refresh token com rotação
- **Polly** (`Microsoft.Extensions.Http.Resilience`) — timeout → retry → circuit breaker
  na única chamada HTTP síncrona entre serviços (Ordering → Catalog)
- **OpenTelemetry** — tracing distribuído (propagado manualmente através do RabbitMQ,
  inclusive através do Outbox) + métricas Prometheus
- **Docker Compose** — Postgres, Redis, RabbitMQ, Prometheus, Jaeger, Grafana e os 6
  serviços, prontos para `docker compose up`
- **xUnit + Testcontainers** — testes de integração contra infraestrutura real (Postgres,
  Redis, RabbitMQ), não mocks

## Como rodar

### Com Docker Compose (recomendado)

```bash
docker compose up -d --build
```

Sobe os 6 serviços + Postgres (4 bancos) + Redis + RabbitMQ + Prometheus + Jaeger +
Grafana. As migrations do EF Core são aplicadas automaticamente na subida.

| Serviço | URL |
|---|---|
| Identity API | http://localhost:5084 |
| Catalog API | http://localhost:5080 |
| Ordering API | http://localhost:5081 |
| Delivery API | http://localhost:5082 |
| Notification API (SignalR hub em `/hubs/order-tracking`) | http://localhost:5083 |
| RabbitMQ management | http://localhost:15672 (guest/guest) |
| Prometheus | http://localhost:9090 |
| Jaeger UI | http://localhost:16686 |
| Grafana | http://localhost:3000 |

```bash
docker compose down          # para tudo
docker compose down -v       # para tudo e apaga os dados do Postgres
```

### Localmente (fora de container)

Requer Postgres, Redis e RabbitMQ rodando em `localhost` nas portas padrão (os
`appsettings.json` de cada serviço já apontam para isso). Rode cada serviço que
precisar a partir da sua pasta em `src/`:

```bash
dotnet run --project src/Identity/Identity.Api
dotnet run --project src/Catalog/Catalog.Api
dotnet run --project src/Ordering/Ordering.Api
dotnet run --project src/Delivery/Delivery.Api
dotnet run --project src/Delivery/Delivery.Worker
dotnet run --project src/Notification/Notification.Api
```

## Testes

```bash
dotnet test
```

Testes unitários de domínio (`*.Domain.Tests`) rodam sem dependências externas. Os
testes de integração (`*.IntegrationTests`) sobem Postgres/Redis/RabbitMQ reais via
Testcontainers — só precisam do Docker disponível, nada precisa estar rodando antes.

## Principais endpoints HTTP

| Método | Rota | Serviço | Papel exigido |
|---|---|---|---|
| `POST` | `/api/auth/register` | Identity | — |
| `POST` | `/api/auth/login` | Identity | — |
| `POST` | `/api/auth/refresh` | Identity | — |
| `POST` | `/api/auth/revoke` | Identity | autenticado |
| `POST` | `/api/restaurants` | Catalog | `RestaurantOwner` |
| `GET` | `/api/restaurants` / `/api/restaurants/{id}` | Catalog | — (público) |
| `POST` | `/api/restaurants/{id}/menu-items` | Catalog | `RestaurantOwner` |
| `PUT` | `/api/restaurants/{id}/menu-items/{menuItemId}/price` | Catalog | `RestaurantOwner` |
| `POST` | `/api/orders` | Ordering | `Customer` |
| `GET` | `/api/orders/{id}` | Ordering | dono do pedido |
| `POST` | `/api/orders/{id}/start-preparing` \| `mark-ready-for-assignment` | Ordering | `RestaurantOwner` |
| `POST` | `/api/orders/{id}/dispatch-for-delivery` \| `mark-delivered` | Ordering | `Courier` |
| `POST` | `/api/orders/{id}/cancel` | Ordering | `Customer` |
| `POST` | `/api/couriers` \| `/go-online` \| `/go-offline` | Delivery | `Courier` |
| `PUT` | `/api/couriers/{id}/location` | Delivery | `Courier` |
| `GET` | `/metrics` | todos os serviços | — (scrape do Prometheus) |

Detalhes de request/response de cada endpoint estão nos próprios controllers em
`src/*/​*.Api/Controllers`.
