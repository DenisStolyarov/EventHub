# EventHub

Microservices solution for managing events, built on ASP.NET Core (.NET 10) with EF Core and PostgreSQL.

Three independent services + three shared projects:

| Service  | Ports (http/https) | Database          | Responsibilities                                  |
| -------- | ------------------ | ----------------- | ------------------------------------------------- |
| Users    | 5101 / 7101        | `eventhub_users`  | Registration, login, password hashing, JWT issuing |
| Events   | 5102 / 7102        | `eventhub_events` | Event CRUD, seat accounting                        |
| Bookings | 5103 / 7103        | `eventhub_bookings` | Booking creation and cancellation                 |

## Solution Layout

```
src/
├── EventHub.Shared.Authentication/    Shared JWT validation options, claim types, current-user service, UserRoles
├── EventHub.Shared.Contracts/        Integration event contracts + topic names shared by Bookings and Events
├── EventHub.Shared.Messaging/        Kafka infrastructure: producer, outbox dispatcher, consumers, topic initializer
├── Users/
│   ├── EventHub.Users.Domain/        User, UserRole, domain exceptions
│   ├── EventHub.Users.Application/  UserService, DTOs, port interfaces
│   ├── EventHub.Users.Infrastructure/ UsersDbContext, repositories, migrations, JWT issuing (JwtIssuerOptions)
│   └── EventHub.Users.Api/           AuthController, JWT validation, Swagger
├── Events/
│   ├── EventHub.Events.Domain/       Event, Period, seat reservation rules
│   ├── EventHub.Events.Application/  EventService, DTOs, pagination, filters
│   ├── EventHub.Events.Infrastructure/ EventsDbContext, repositories, migrations
│   └── EventHub.Events.Api/          EventsController, JWT validation, Swagger
└── Bookings/
    ├── EventHub.Bookings.Domain/     Booking, BookingStatus, BookingManager (max 10 active)
    ├── EventHub.Bookings.Application/ BookingService, DTOs, port interfaces
    ├── EventHub.Bookings.Infrastructure/ BookingsDbContext, repositories, migrations
    └── EventHub.Bookings.Api/        BookingsController, JWT validation, Swagger
```

Each service follows clean architecture:

```
Presentation (Api) ──▶ Application ──▶ Domain
        │
        └──▶ Infrastructure ──▶ Application
```

Services are fully decoupled — bookings reference users and events by `Guid` ids only (`UserId`, `EventId`), with no cross-service foreign keys. Each service owns its database, EF Core `DbContext`, and migrations.

## Requirements

- .NET 10 SDK
- PostgreSQL 16
- Apache Kafka 3.9

## Database and Kafka

Start the full stack — PostgreSQL, Kafka, three API services and the Gateway:

```bash
podman compose up -d --build
# or:
docker compose up -d --build
```

| Container | Port | Purpose |
| --------- | ---- | ------- |
| eventhub-postgres | 5432 | one instance, three databases (`eventhub_*`) — created automatically by each service's `MigrateAsync()` |
| eventhub-kafka | 9092 | KRaft (no Zookeeper); internal listener `kafka:29092` for compose services |
| eventhub-users-api / events-api / bookings-api | 5101–5103 | API services |
| eventhub-gateway | **5000** | single entry point (YARP reverse proxy + aggregated Swagger UI at `/swagger`) |

Data lives in named volumes (`eventhub_pgdata`, `eventhub_kafka_data`) and survives restarts and `compose down`; a full wipe (databases + Kafka topics/offsets) is `podman compose down -v`.

Stop: `podman compose down`.

## Build and Run

```bash
dotnet build
dotnet run --project src/Users/EventHub.Users.Api       # http://localhost:5101
dotnet run --project src/Events/EventHub.Events.Api     # http://localhost:5102
dotnet run --project src/Bookings/EventHub.Bookings.Api # http://localhost:5103
dotnet run --project src/Gateway/EventHub.Gateway       # http://localhost:5000
```

Swagger UI: `http://localhost:<port>/swagger`; via Gateway — a single aggregated UI at `http://localhost:5000/swagger`.

Migrations are applied automatically at startup (`MigrateAsync`).

## Migrations

Migrations live in each service's `Infrastructure/Persistence/Migrations` folder. Use the service's Api project as the startup project:

```bash
dotnet ef migrations add <Name> -c UsersDbContext -p src/Users/EventHub.Users.Infrastructure -s src/Users/EventHub.Users.Api -o Persistence/Migrations

dotnet ef migrations add <Name> -c EventsDbContext -p src/Events/EventHub.Events.Infrastructure -s src/Events/EventHub.Events.Api -o Persistence/Migrations

dotnet ef migrations add <Name> -c BookingsDbContext -p src/Bookings/EventHub.Bookings.Infrastructure -s src/Bookings/EventHub.Bookings.Api -o Persistence/Migrations
```

## Testing

| Project                              | Provider                           | Docker required |
| ------------------------------------ | ---------------------------------- | --------------- |
| `EventHub.Users.UnitTests`           | Moq + FakeTimeProvider             | No              |
| `EventHub.Users.IntegrationTests`    | Real PostgreSQL via Testcontainers | Yes             |
| `EventHub.Events.UnitTests`          | EF Core InMemory                   | No              |
| `EventHub.Events.IntegrationTests`   | Real PostgreSQL via Testcontainers | Yes             |
| `EventHub.Bookings.UnitTests`        | Moq + FakeTimeProvider             | No              |
| `EventHub.Bookings.IntegrationTests` | Real PostgreSQL via Testcontainers | Yes             |

```bash
dotnet test
```

## API Endpoints

API versioning via URL segment (`/api/v1/...`), `X-Api-Version` header, or `api-version` query parameter.

### Users

| Method | Endpoint             | Description             | Auth | Success | Errors       |
| ------ | -------------------- | ----------------------- | ---- | ------- | ------------ |
| POST   | /api/v1/auth/register | Register new user      | None | 204     | 400 / 409    |
| POST   | /api/v1/auth/login   | Login and get JWT       | None | 200     | 400 / 401    |

### Events

| Method | Endpoint          | Description              | Auth          | Success | Errors                   |
| ------ | ----------------- | ------------------------ | ------------- | ------- | ------------------------ |
| GET    | /api/v1/events    | Filter + paginate events | None          | 200     | 400                      |
| GET    | /api/v1/events/{id} | Get event by id        | None          | 200     | 404                      |
| POST   | /api/v1/events    | Create event             | Admin         | 201     | 400 / 401 / 403 / 422    |
| PUT    | /api/v1/events/{id} | Update event           | Admin         | 200     | 400 / 401 / 403 / 404 / 422 |
| DELETE | /api/v1/events/{id} | Delete event           | Admin         | 204     | 401 / 403 / 404          |

Query parameters for GET /api/v1/events: `title`, `from`, `to`, `page` (default 1), `pageSize` (default 10, max 50).

### Bookings

| Method | Endpoint               | Description              | Auth          | Success | Errors                |
| ------ | ---------------------- | ------------------------ | ------------- | ------- | --------------------- |
| POST   | /api/v1/bookings       | Create booking| Authenticated | 202     | 400 / 401 / 409       |
| GET    | /api/v1/bookings/{id}  | Get booking by id       | Authenticated | 200     | 401 / 403 / 404       |
| DELETE | /api/v1/bookings/{id}  | Cancel booking           | Authenticated | 204     | 401 / 403 / 404 / 422 |

### Booking Rules

- **Active bookings limit:** a user cannot have more than 10 active bookings (Pending or Confirmed) — `409 Conflict`.
- **Access:** users see and cancel only their own bookings; admins can access any booking — otherwise `403 Forbidden`.
- **Missing event:** a booking for a non-existent event stays `Pending` indefinitely.

### Role Model

| Role  | Permissions                                                          |
| ----- | -------------------------------------------------------------------- |
| Admin | Create/update/delete events; access any booking                      |
| User  | Book events; view and cancel own bookings only                       |

Booking statuses: `Pending`, `Confirmed`, `Rejected`, `Cancelled`.

JWT is issued by the Users service and validated by Events and Bookings with the same Issuer/Audience/Secret (configured in `Authentication:Jwt` of each service's `appsettings.json`).

## Async Booking Saga (Kafka)

Bookings and Events communicate asynchronously via Kafka. Message contracts and topic names live in `EventHub.Shared.Contracts`. One booking = one seat.

Topics and their events:

| Topic                   | Event              | Publisher → Subscriber |
| ----------------------- | ------------------ | ---------------------- |
| `booking-created`       | `BookingCreated`    | Bookings → Events      |
| `event-seat-reserved`   | `EventSeatReserved` | Events → Bookings      |
| `event-seat-unavailable`| `EventSeatUnavailable` | Events → Bookings   |
| `booking-cancelled`     | `BookingCancelled`  | Bookings → Events      |
| `event-cancelled`       | `EventCancelled`    | Events → Bookings      |

Message keys are `EventId` — all messages of one saga flow into the same partition and are processed in order.

```
POST /bookings      → Booking(Pending) + outbox[BookingCreated]   (single transaction)
Events              → TryReserveSeats(): true  → EventSeatReserved  + outbox
                    → TryReserveSeats(): false → EventSeatUnavailable + outbox
Bookings            → Confirm(booking) / Reject(booking) + inbox (single transaction)
DELETE /bookings/{id} → Cancel() + outbox[BookingCancelled] → Events: ReleaseSeats
DELETE /events/{id}  → delete + outbox[EventCancelled] → Bookings: Cancel() all active bookings of the event
```

Reliability:

- **Transactional outbox** in both services: business change + outbox row commit in one DB transaction; a background `OutboxDispatcher` publishes to Kafka strictly after commit and retries until published.
- **Inbox deduplication by primary key**: every processed message id is recorded in the same transaction as its effect; a duplicate delivery fails on the inbox PK, the whole transaction rolls back, and the consumer logs-and-skips the duplicate.
- **Compensation:** if `EventSeatReserved` arrives for an already-cancelled booking, Bookings publishes `BookingCancelled` back and Events releases the seat; a duplicate release is a logged no-op.
- **Topic creation:** each subscriber creates the topics it consumes at startup (`KafkaTopicInitializer`, idempotent, retries while Kafka is unavailable, never fails the host). Malformed messages are logged and skipped.

