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
| GET      | /api/v1/events        | Get all events           | 200 OK         | -            |
| GET      | /api/v1/events/{id}   | Get event by id          | 200 OK         | 404 Not Found|
| POST     | /api/v1/events        | Create a new event       | 201 Created    | 400 Bad Request |
| PUT      | /api/v1/events/{id}   | Update an event          | 200 OK         | 404 Not Found / 400 Bad Request |
| DELETE   | /api/v1/events/{id}   | Delete an event          | 204 No Content | 404 Not Found|

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