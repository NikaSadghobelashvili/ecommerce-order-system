# E-Commerce Order & Notification System

A Clean Architecture **.NET 10 Web API** demonstrating:
- **Clean Architecture** (`Domain`, `Application`, `Infrastructure`, `Api`) with Repository & Unit of Work patterns
- **Dependency Injection** across layers
- **MS SQL Server** with Entity Framework Core & Code-First Migrations
- **Distributed Caching** with **Redis** (Cache-Aside & Cache Invalidation strategies)
- **Asynchronous Event-Driven Messaging** with **RabbitMQ** & **MassTransit**
- **Docker Compose** for local infrastructure (MS SQL Server, Redis, RabbitMQ)

## Architecture

```
src/
 ├── Core/
 │    ├── Domain/          # Entities (Product, Order, OrderItem) & Enums
 │    └── Application/     # Business logic, DTOs, Interfaces, Integration Events
 ├── Infrastructure/       # EF Core (MS SQL), RedisCacheService, MassTransit/RabbitMQ
 └── Api/                  # REST Controllers & DI setup
```

## Getting Started

### 1. Start Infrastructure (MS SQL, Redis, RabbitMQ)
```bash
docker compose up -d
```
- **RabbitMQ Management UI:** http://localhost:15672 (`guest` / `guest`)

### 2. Apply Database Migrations
```bash
dotnet ef database update --project src/Infrastructure --startup-project src/Api
```

### 3. Run the API
```bash
dotnet run --project src/Api/Api.csproj
```

## API Endpoints
- `GET /api/products` — Fetches products using **Redis Cache-Aside** (falls back to MS SQL on cache miss)
- `GET /api/products/{id}` — Fetches a single product with Redis caching
- `POST /api/products` — Creates a product and invalidates the product list cache
- `POST /api/orders` — Creates an order (`Pending`), updates stock, invalidates Redis cache, and publishes `OrderCreatedEvent` to **RabbitMQ**
- `GET /api/orders/{id}` — Retrieves order details (status transitions to `Completed` asynchronously via `OrderCreatedConsumer`)
