# EventHub

REST API service for managing events, built on ASP.NET Core Web API.

## Requirements

- .NET 10 SDK or later
- PostgreSQL 14+

## Database Setup

The application stores events and bookings in PostgreSQL. The easiest way to start a local database is with Docker Compose:

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

You can also configure the connection string via environment variables or .NET user secrets.

## Build and Run

```bash
docker compose up -d
dotnet build
dotnet run --project src/EventHub.Api
```

The database schema is created automatically on startup using `EnsureCreatedAsync`. No manual migrations are required for local development.

After launch, Swagger UI is available at: [http://localhost:5000/swagger](http://localhost:5000/swagger).

## Testing

Unit and integration tests use the EF Core InMemory provider. Each test class gets its own isolated in-memory database via `ServiceCollection` and `AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(dbName))`.

Run all tests from the repository root:

```bash
dotnet test
```

## API Endpoints

API versioning is supported via URL segment, `X-Api-Version` header, or `api-version` query string parameter. The default version is 1.0.

| Method | Endpoint                 | Description                              | Success Status | Error Status                    |
| ------ | ------------------------ | ---------------------------------------- | -------------- | ------------------------------- |
| GET    | /api/v1/events           | Get events with filtering and pagination | 200 OK         | 400 Bad Request                 |
| GET    | /api/v1/events/{id}      | Get event by id                          | 200 OK         | 404 Not Found                   |
| POST   | /api/v1/events           | Create a new event                       | 201 Created    | 400 Bad Request                 |
| POST   | /api/v1/events/{id}/book | Create a booking for an event            | 202 Accepted   | 404 Not Found / 409 Conflict    |
| PUT    | /api/v1/events/{id}      | Update an event                          | 200 OK         | 404 Not Found / 400 Bad Request |
| DELETE | /api/v1/events/{id}      | Delete an event                          | 204 No Content | 404 Not Found                   |
| GET    | /api/v1/bookings/{id}    | Get booking by id                        | 200 OK         | 404 Not Found                   |

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

### Versioning Examples

**URL segment:**
```
GET /api/v1/events
```

**Header:**
```
GET /api/v1/events
X-Api-Version: 1.0
```

**Query string:**
```
GET /api/v1/events?api-version=1.0
```

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
| status  | BookingStatus | Current booking status         |

### Booking Status

| Value     | Description                               |
| --------- | ----------------------------------------- |
| Pending   | Booking was created and awaits processing |
| Confirmed | Booking was confirmed                     |
| Rejected  | Booking was rejected                      |

### Date/Time Format

All date/time values must be provided in **UTC** using ISO 8601 format with the `Z` suffix:

```json
"startAt": "2026-01-15T10:00:00Z"
```

Using the `Z` suffix ensures that both the client and server interpret the timestamp as UTC, avoiding timezone ambiguity.

## Validation Rules

- `title` is required
- `startAt` is required
- `endAt` is required and must be later than `startAt`
- `totalSeats` is required and must be greater than zero
- `description` is optional

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

Booking status transitions are handled by a background service that polls for pending bookings on a fixed schedule.

### Processing Flow

1. Every 2 minutes the service polls for bookings with `Pending` status
2. Pending bookings are processed in parallel
3. Each booking is processed simulating an external system call (30-second delay)
4. If the associated event still exists, the booking transitions from `Pending` to `Confirmed`
5. If the event was deleted, the booking transitions to `Rejected`
6. On unexpected failure, the booking is rejected and the reserved seat is returned to the pool
7. `ProcessedAt` is recorded at the time of the status change

### Lifecycle

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
              ↓                               ↓
         Event exists?                   Event deleted?
              ↓                               ↓
         Confirmed                        Rejected
```

## Concurrency and Synchronization

The service uses the following mechanisms to prevent race conditions:

### `SemaphoreSlim` in BookingService

`BookingService.CreateBookingAsync` uses a `SemaphoreSlim(1, 1)` to protect the atomic check-and-reserve sequence when working with EF Core. The critical section reads the event, decrements `AvailableSeats`, and persists both the updated event and the new booking in a single `SaveChangesAsync`:

```
await _semaphore.WaitAsync();
try
{
    event = await context.Events.FindAsync(id);  // read
    event.TryReserveSeats();                     // check + decrement
    context.Bookings.Add(new Booking(...));      // create booking
    await context.SaveChangesAsync();            // atomic write
}
finally
{
    _semaphore.Release();
}
```

Without this synchronization, two concurrent requests could both read `AvailableSeats > 0`, both pass the check, and both create a booking — exceeding the seat limit.

### Scoped DbContext in BookingProcessor

`BookingProcessor` runs in the background and processes pending bookings in parallel. Each processing task creates a new `IServiceScope` via `IServiceScopeFactory.CreateAsyncScope()` and resolves its own `AppDbContext`. This is required because `DbContext` is not thread-safe and must not be shared across parallel operations.

```
await using var scope = scopeFactory.CreateAsyncScope();
var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

var booking = await db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId);
```

## Overbooking Scenario

When all seats for an event are taken, subsequent booking attempts return `409 Conflict`:

### Request

```http
POST /api/v1/events/{id}/book
```

### Response (409 Conflict)

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.10",
  "title": "No Available Seats",
  "status": 409,
  "detail": "No available seats for this event.",
  "traceId": "00-7d6f9f2e4f0b7c1c0f2e8b9d2f4a6c01-1b2c3d4e5f6a7b8c-00"
}
```

### Example Flow

1. Event is created with `totalSeats: 1`
2. First `POST /events/{id}/book` succeeds → `202 Accepted`, `availableSeats` becomes 0
3. Second `POST /events/{id}/book` fails → `409 Conflict` with `NoAvailableSeatsException`
4. The background processor confirms the first booking; `availableSeats` stays 0
5. Any further booking attempts continue to receive `409 Conflict`

## Data Storage

Event and booking data is stored in PostgreSQL and accessed through Entity Framework Core (`AppDbContext`).

Filtering, pagination, and counting are implemented with `IQueryable` and executed by EF Core. `EventService` validates input, normalizes pagination parameters, builds the query, and maps results to DTOs. This keeps the service independent from storage details and allows efficient query execution at the database level.

Tests use the EF Core InMemory provider instead of PostgreSQL so they can run without an external database and remain isolated from each other.