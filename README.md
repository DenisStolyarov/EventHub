# EventHub

REST API service for managing events, built on ASP.NET Core Web API.

## Requirements

- .NET 10 SDK or later

## Build and Run

```bash
dotnet build

dotnet run --project src/EventHub.Api
```

After launch, Swagger UI is available at: [http://localhost:5000/swagger](http://localhost:5000/swagger).

## API Endpoints

API versioning is supported via URL segment, `X-Api-Version` header, or `api-version` query string parameter. The default version is 1.0.

| Method   | Endpoint              | Description              | Success Status | Error Status |
|----------|-----------------------|--------------------------|----------------|--------------|
| GET      | /api/v1/events        | Get events with filtering and pagination | 200 OK         | 400 Bad Request |
| GET      | /api/v1/events/{id}   | Get event by id          | 200 OK         | 404 Not Found|
| POST     | /api/v1/events        | Create a new event       | 201 Created    | 400 Bad Request |
| PUT      | /api/v1/events/{id}   | Update an event          | 200 OK         | 404 Not Found / 400 Bad Request |
| DELETE   | /api/v1/events/{id}   | Delete an event          | 204 No Content | 404 Not Found|

### Query Parameters (GET /api/v1/events)

| Parameter | Type           | Required | Default | Description                            |
|-----------|----------------|----------|---------|----------------------------------------|
| title     | string         | No       | -       | Filter by event title (partial match)   |
| from      | DateTimeOffset | No       | -       | Filter events starting from this date  |
| to        | DateTimeOffset | No       | -       | Filter events ending before this date  |
| page      | int            | No       | 1       | Page number (>= 1)                    |
| pageSize  | int            | No       | 10      | Items per page (1–50)                  |

### Paginated Response

The response is wrapped in a `PaginatedResult` object:

| Field           | Type    | Description                              |
|-----------------|---------|------------------------------------------|
| data            | array   | Array of events on the current page      |
| pageNumber      | int     | Current page number                      |
| pageSize        | int     | Number of items per page                  |
| totalPages      | int     | Total number of pages                     |
| totalRecords    | int     | Total number of matching events          |
| itemsOnPage     | int     | Number of items on the current page      |
| hasNextPage     | bool    | Whether a next page exists               |
| hasPreviousPage | bool    | Whether a previous page exists           |

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

| Field        | Type           | Required | Description                |
|-------------|----------------|----------|----------------------------|
| id          | Guid           | Yes      | Auto-generated identifier  |
| title       | string         | Yes      | Event title (non-empty, trimmed) |
| description | string         | No       | Event description |
| startAt     | DateTimeOffset | Yes      | Event start time (UTC, ISO 8601 with Z suffix) |
| endAt       | DateTimeOffset | Yes      | Event end time (UTC, ISO 8601 with Z suffix, must be after startAt) |

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
- `description` is optional

## Data Storage

Event data is stored in application memory. All data is lost when the application restarts.