# EventHub

REST API service for managing events, built on ASP.NET Core Web API.

## Project Architecture

```
Presentation (EventHub.Api) ──▶ Application ──▶ Domain
        │
        └──▶ Infrastructure ──▶ Application
```

| Project                   | Responsibility                                                                                                 |
| ------------------------- | -------------------------------------------------------------------------------------------------------------- |
| `EventHub.Domain`         | Core entities, value objects, enums, and domain exceptions. Pure C# — no framework references.                 |
| `EventHub.Application`    | Business logic: use-case services, port interfaces, DTOs, filters, and mapping extensions.                     |
| `EventHub.Infrastructure` | EF Core persistence, repositories, migrations, and background processing.                                      |
| `EventHub.Api`            | Composition root, thin controllers, request DTOs, exception handler, and Swagger/API versioning configuration. |

## Requirements

- .NET 10 SDK or later
- PostgreSQL 14+

## Database

Start a local PostgreSQL database with Docker Compose:

```bash
docker compose up -d
```

The included `docker-compose.yml` runs `postgres:16-alpine` on port `5432` with:

- Database: `eventapi`
- Username: `postgres`
- Password: `postgres`

To stop the database:

```bash
docker compose down
```

## Configuration

Set the connection string in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=eventapi;Username=postgres;Password=postgres"
  }
}
```

You can also override it via environment variables or .NET user secrets.

## Build and Run

```bash
docker compose up -d
dotnet build
dotnet run --project src/EventHub.Api
```

After launch, Swagger UI is available at: [http://localhost:5000/swagger](http://localhost:5000/swagger).

## Migrations

Migrations live in `EventHub.Infrastructure`; use `EventHub.Api` as the startup project for configuration.

Create a new migration after changing the model in `AppDbContext` or entity configurations:

```bash
dotnet ef migrations add <MigrationName> --project src/EventHub.Infrastructure --output-dir Persistence/Migrations --startup-project src/EventHub.Api
```

Apply pending migrations manually:

```bash
dotnet ef database update --project src/EventHub.Infrastructure --startup-project src/EventHub.Api
```

## Testing

| Project                     | Provider                           | Docker required |
| --------------------------- | ---------------------------------- | --------------- |
| `EventHub.UnitTests`        | EF Core InMemory                   | No              |
| `EventHub.IntegrationTests` | Real PostgreSQL via Testcontainers | Yes             |

### Unit tests (`EventHub.UnitTests`)

Moq and a fake time provider. Each test class gets an isolated in-memory database.

```bash
dotnet test tests/EventHub.UnitTests
```

### Integration tests (`EventHub.IntegrationTests`)

The `IntegrationTestFixture` starts a single `postgres:16-alpine` container per test class, applies migrations via `MigrateAsync`, and truncates all tables between tests via `ResetDataAsync()`.

> **Docker is required.** The Docker daemon must be running before executing these tests.

```bash
dotnet test tests/EventHub.IntegrationTests
```

### Run all tests

```bash
dotnet test
```

## API Endpoints

API versioning is supported via URL segment, `X-Api-Version` header, or `api-version` query string parameter. The default version is 1.0.

| Method | Endpoint                 | Description                              | Auth          | Success Status | Error Status                |
| ------ | ------------------------ | ---------------------------------------- | ------------- | -------------- | --------------------------- |
| POST   | /api/v1/auth/register    | Register new user                        | None          | 204 No Content | 400 / 409                   |
| POST   | /api/v1/auth/login       | Login and get JWT token                  | None          | 200 OK         | 400 / 401                   |
| GET    | /api/v1/events           | Get events with filtering and pagination | None          | 200 OK         | 400                         |
| GET    | /api/v1/events/{id}      | Get event by id                          | None          | 200 OK         | 404                         |
| POST   | /api/v1/events           | Create a new event                       | Admin         | 201 Created    | 400 / 401 / 403 / 422       |
| POST   | /api/v1/events/{id}/book | Create a booking for an event            | Authenticated | 202 Accepted   | 400 / 401 / 404 / 409       |
| PUT    | /api/v1/events/{id}      | Update an event                          | Admin         | 200 OK         | 400 / 401 / 403 / 404 / 422 |
| DELETE | /api/v1/events/{id}      | Delete an event                          | Admin         | 204 No Content | 401 / 403 / 404             |
| GET    | /api/v1/bookings/{id}    | Get booking by id                        | Authenticated | 200 OK         | 401 / 403 / 404             |
| DELETE | /api/v1/bookings/{id}    | Cancel a booking                         | Authenticated | 204 No Content | 401 / 403 / 404             |

### Query Parameters (GET /api/v1/events)

| Parameter | Type           | Required | Default | Description                           |
| --------- | -------------- | -------- | ------- | ------------------------------------- |
| title     | string         | No       | -       | Filter by event title (partial match) |
| from      | DateTimeOffset | No       | -       | Filter events starting from this date |
| to        | DateTimeOffset | No       | -       | Filter events ending before this date |
| page      | int            | No       | 1       | Page number (>= 1)                    |
| pageSize  | int            | No       | 10      | Items per page (1–50)                 |

### Paginated Response

The response is wrapped in a `PaginatedResult` object:

| Field           | Type  | Description                         |
| --------------- | ----- | ----------------------------------- |
| data            | array | Array of events on the current page |
| pageNumber      | int   | Current page number                 |
| pageSize        | int   | Number of items per page            |
| totalPages      | int   | Total number of pages               |
| totalRecords    | int   | Total number of matching events     |
| itemsOnPage     | int   | Number of items on the current page |
| hasNextPage     | bool  | Whether a next page exists          |
| hasPreviousPage | bool  | Whether a previous page exists      |

## Authentication

### Role Model

| Role  | Permissions                                                            |
| ----- | --------------------------------------------------------------------- |
| Admin | Create, update, delete events; cancel any booking; everything User can |
| User  | Book events; view and cancel own bookings only                        |

### JWT Configuration

Token parameters are stored in `appsettings.json` under `Authentication:Jwt`:

```json
{
  "Authentication": {
    "Jwt": {
      "Issuer": "https://eventhub.com",
      "Audience": "eventhub-api",
      "ExpiryMinutes": 15,
      "Secret": "<at-least-32-chars-secret>"
    }
  }
}
```

> **Security:** The `Secret` must be at least 32 characters. In production, set it via environment variables or user secrets — never commit a real secret to the repository.

### Obtaining a Token

Register via `POST /api/v1/auth/register`, then login via `POST /api/v1/auth/login` to obtain a JWT. Click **Authorize** in Swagger UI and paste the token.

## Event Model

| Field          | Type           | Required | Description                                                         |
| -------------- | -------------- | -------- | ------------------------------------------------------------------- |
| id             | Guid           | Yes      | Auto-generated identifier                                           |
| title          | string         | Yes      | Event title (non-empty, trimmed)                                    |
| description    | string         | No       | Event description                                                   |
| totalSeats     | int            | Yes      | Total number of seats (must be > 0)                                 |
| availableSeats | int            | Yes      | Available seats (decreases on booking, increases on cancellation)   |
| startAt        | DateTimeOffset | Yes      | Event start time (UTC, ISO 8601 with Z suffix)                      |
| endAt          | DateTimeOffset | Yes      | Event end time (UTC, ISO 8601 with Z suffix, must be after startAt) |

## Booking Model

Booking endpoints return `BookingInfo`.

| Field   | Type          | Description                    |
| ------- | ------------- | ------------------------------ |
| id      | Guid          | Booking identifier             |
| eventId | Guid          | Identifier of the booked event |
| userId  | Guid          | Identifier of the booking user |
| status  | BookingStatus | Current booking status         |

### Booking Status

| Value     | Description                                   |
| --------- | --------------------------------------------- |
| Pending   | Booking created, awaits background processing |
| Confirmed | Booking processed successfully                |
| Rejected  | Event deleted or processing failed            |
| Cancelled | Booking cancelled by user or admin            |

### Booking Rules

- **Past events:** Booking is rejected if the event has already started (`400 Bad Request`).
- **Active bookings limit:** A user cannot have more than 10 active (pending) bookings across future events (`409 Conflict`).
- **Cancellation:** Users can cancel only their own bookings; admins can cancel any booking. Unauthorized cancellation returns `403 Forbidden`.

### Date/Time Format

All date/time values must be provided in **UTC** using ISO 8601 format with the `Z` suffix:

```json
"startAt": "2026-01-15T10:00:00Z"
```

Using the `Z` suffix ensures that both the client and server interpret the timestamp as UTC, avoiding timezone ambiguity.

## Error Responses

Errors are returned as Problem Details JSON.

### 400 Bad Request

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Validation Error",
  "status": 400,
  "errors": {
    "From": [
      "'from' must be before or equal to 'to'."
    ]
  },
  "traceId": "00-7d6f9f2e4f0b7c1c0f2e8b9d2f4a6c01-1b2c3d4e5f6a7b8c-00"
}
```

### 404 Not Found

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Not Found",
  "status": 404,
  "detail": "'Event' with id '01972f5f-2c71-7368-92dc-9ea346eb5442' was not found.",
  "traceId": "00-7d6f9f2e4f0b7c1c0f2e8b9d2f4a6c01-1b2c3d4e5f6a7b8c-00"
}
```

### 409 Conflict

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.10",
  "title": "No Available Seats",
  "status": 409,
  "detail": "No available seats for this event.",
  "traceId": "00-7d6f9f2e4f0b7c1c0f2e8b9d2f4a6c01-1b2c3d4e5f6a7b8c-00"
}
```

### 500 Internal Server Error

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.6.1",
  "title": "Internal Server Error",
  "status": 500,
  "detail": "An unexpected error occurred.",
  "traceId": "00-7d6f9f2e4f0b7c1c0f2e8b9d2f4a6c01-1b2c3d4e5f6a7b8c-00"
}
```

## Background Processing

Booking status transitions are handled by a background service that polls for pending bookings every 2 minutes:

```
POST /events/{id}/book → Pending
                             ↓
                    Polling every 2 minutes
                             ↓
                 Parallel processing (Task.WhenAll)
                             ↓
             External service call (30 s delay)
                             ↓
               ┌──────────────┴──────────────┐
               ↓                             ↓
          Event exists?                 Event deleted?
               ↓                             ↓
          Confirmed                       Rejected
```

On unexpected failure, the booking is rejected and the reserved seat is returned to the pool. `ProcessedAt` is recorded at the time of the status change.

## Concurrency and Synchronization

### `SemaphoreSlim` in BookingService

`BookingService.CreateBookingAsync` uses a `SemaphoreSlim(1, 1)` to protect the atomic check-and-reserve sequence: it reads the event, decrements `AvailableSeats`, and persists both the updated event and the new booking in a single `SaveChangesAsync`. Without this synchronization, two concurrent requests could both read `AvailableSeats > 0`, both pass the check, and both create a booking — exceeding the seat limit.

### Scoped DbContext in BookingProcessor

`BookingProcessor` runs in the background and processes pending bookings in parallel. Each processing task creates a new `IServiceScope` via `IServiceScopeFactory.CreateAsyncScope()` and resolves its own `AppDbContext`, because `DbContext` is not thread-safe and must not be shared across parallel operations.

## Overbooking Scenario

When all seats for an event are taken, subsequent `POST /api/v1/events/{id}/book` requests return `409 Conflict` with the No Available Seats error (see [Error Responses](#error-responses)).

Example flow with `totalSeats: 1`:
1. First booking succeeds → `202 Accepted`, `availableSeats` becomes 0
2. The background processor confirms the booking; `availableSeats` stays 0
3. Any further booking attempts receive `409 Conflict`

## Data Storage

Event and booking data is stored in PostgreSQL and accessed through EF Core. `AppDbContext` and entity configurations live in the Infrastructure layer; Application services access them only through the `IUnitOfWork` abstraction defined in Application, keeping business logic decoupled from storage details.

Filtering, pagination, and counting use `IQueryable` and execute at the database level.
