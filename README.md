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

| Method   | Endpoint         | Description              | Success Status | Error Status |
|----------|------------------|--------------------------|----------------|--------------|
| GET      | /events          | Get all events           | 200 OK         | -            |
| GET      | /events/{id}     | Get event by id          | 200 OK         | 404 Not Found|
| POST     | /events          | Create a new event       | 201 Created    | 400 Bad Request |
| PUT      | /events/{id}     | Update an event          | 200 OK         | 404 Not Found / 400 Bad Request |
| DELETE   | /events/{id}     | Delete an event          | 204 No Content | 404 Not Found|

## Event Model

| Field        | Type       | Required | Description                |
|-------------|------------|----------|----------------------------|
| id          | Guid       | Yes      | Auto-generated identifier  |
| title       | string     | Yes      | Event title  |
| description | string     | No       | Event description |
| startAt     | DateTimeOffset | Yes      | Event start time |
| endAt       | DateTimeOffset | Yes      | Event end time (must be after startAt) |

## Validation Rules

- `title` is required
- `startAt` is required
- `endAt` is required and must be later than `startAt`
- `description` is optional

## Data Storage

Event data is stored in application memory. All data is lost when the application restarts.
