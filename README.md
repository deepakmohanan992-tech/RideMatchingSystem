RideMatchingSystem
=================

Project skeleton created to match the requested folder layout:

- Controllers/
- Hubs/
- Models/
- DTOs/
- Services/
- Data/
- BackgroundServices/

Populate these placeholders with your implementations.

Getting started
---------------

Prerequisites
- .NET 10 SDK
- (Optional) Visual Studio 2026 or VS Code

Run locally
1. Restore packages: dotnet restore
2. Build: dotnet build
3. Run: dotnet run --project RideMatchingSystem\RideMatchingSystem.csproj

On first run the demo SQLite database (ride_matching.db) will be created automatically.

API and testing
- Swagger/OpenAPI is available in Development at /openapi and the Swagger UI depending on environment configuration.
- SignalR hub endpoint: /rideHub

Example endpoints (Postman collection included in postman_collection.json):
- POST /api/drivers/{id}/availability?available=true
- POST /api/drivers/{id}/location  { "latitude": 1.23, "longitude": 4.56 }
- POST /api/rides  { "riderId": 1, "pickupLocation": {"latitude":..,"longitude":..} }
- POST /api/rides/{id}/match  (attempt to match a driver)
- GET /api/rides/{id}
- GET /api/rides/pending

Architecture & design notes
- ASP.NET Core Web API with SignalR for real-time updates
- EF Core (SQLite) for persistence in the demo
- In-memory caching for available drivers to reduce DB load
- BackgroundService (RideMatchingWorker) polls pending rides and attempts to match using MatchingService (Haversine proximity)

Assumptions & tradeoffs
- SQLite used for demo; swap to Postgres/SQL Server for production
- Geospatial uses Haversine in-memory filtering; for large scale use spatial indexes (PostGIS / SQL Server geography)
- Matching is simple nearest-driver; production systems need smarter constraints, routing, and multi-criteria scoring

Next steps (optional)
- Add authentication and authorization
- Add driver/rider onboarding, verification, and reputation
- Use a message broker (Redis/ Kafka) to scale background matching and cross-instance coordination
- Add telemetry and metrics

Postman collection
- See postman_collection.json for a simple collection to exercise the API and hub.

 Using SQL Server (localhost\\SQLEXPRESS)
 ---------------------------------------
 This project can use a local SQL Server instance. The default connection string in appsettings.json points to:

   Server=localhost\\SQLEXPRESS;Database=RideMatchingDb;Trusted_Connection=True;TrustServerCertificate=True;

 If your SQL Server instance is available and you have permissions, the app will attempt to create the database/tables on startup using EF Core EnsureCreated().

 Alternatively, you can run the provided SQL script to create the database and tables manually:

 1. Open a developer PowerShell and run:

	sqlcmd -S localhost\\SQLEXPRESS -i sql\\CreateDatabaseAndTables.sql

	(Or run the script in SQL Server Management Studio connected to localhost\\SQLEXPRESS.)

 2. Start the application:

	dotnet run --project RideMatchingSystem\\RideMatchingSystem.csproj

 EF Core migrations (optional)
 ----------------------------
 To use migrations instead of EnsureCreated:

 1. Install dotnet-ef tools if not present: dotnet tool install --global dotnet-ef
 2. Add a migration: dotnet ef migrations add InitialCreate --project RideMatchingSystem\\RideMatchingSystem.csproj
 3. Update database: dotnet ef database update --project RideMatchingSystem\\RideMatchingSystem.csproj

 Note: Run these commands from a shell with access to your SQL Server instance.

 ----------------------------------------------------------------------------------------
 Detail Explanation
 ----------------------------------------------------------------------------------------
 RideMatchingSystem

Short README

Overview

RideMatchingSystem is a .NET 8 microservice-style Web API that matches riders to drivers in real time. It provides REST endpoints for riders, drivers, and rides, uses SignalR hubs for real-time notifications, and runs a background worker to evaluate and match offers.

Key components

- API (Controllers): RidersController, DriversController, RidesController — expose REST endpoints for CRUD and matching operations.
- Services: DriverService, RiderService, RideService, MatchingService, OfferStore, ConnectionRegistry — encapsulate business logic, in-memory stores and connection management.
- Background worker: RideMatchingWorker — periodically processes pending ride requests and matches them to available offers.
- Real-time: SignalR hubs (RiderHub, DriverHub, RideHub) — push notifications and live updates to connected clients.
- Data: AppDbContext (EF Core) — persistence layer for drivers, riders, and rides.
- DTOs & Models: Shape API contracts and domain entities.

Architecture overview

The application follows a layered architecture:

- Presentation/API layer: Controllers expose HTTP endpoints and forward requests to the service layer.
- Service layer: Contains domain logic (matching, rider/driver management) and interacts with persistence and SignalR hubs.
- Background processing: A hosted worker evaluates matching logic asynchronously to keep request latency low.
- Persistence: EF Core DbContext stores core entities; the in-memory OfferStore is used for fast ephemeral matching data.
- Real-time messaging: SignalR hubs notify clients about match updates and ride status changes.

Assumptions and trade-offs

- Single responsibility services: Services are focused on a small set of operations for clarity and testability.
- In-memory OfferStore: Chosen for lower latency and simplicity during initial development; not durable across restarts.
- Consistency: Best-effort matching (eventual consistency) rather than distributed transaction across components to keep the system simple.
- Simplicity over completeness: The project prioritizes a clear matching flow and real-time updates over advanced features (pricing, routing, geospatial optimizations).
- Local-first: Defaults and examples assume a single-server deployment for development; production readiness requires additional infrastructure.

Scalability considerations

- State & persistence:
  - Replace in-memory OfferStore and ConnectionRegistry with a shared store (Redis, distributed cache) if running multiple API instances.
  - Use a central relational database or partitioned data stores; consider read replicas for read-heavy workloads.

- Real-time scale:
  - Configure SignalR backplane (Redis) or use managed SignalR to distribute messages across multiple servers.

- Background processing:
  - Move matching logic into a scalable job queue (e.g., Azure Service Bus, RabbitMQ, or Hangfire) for higher throughput and retries.
  - Workers can be scaled horizontally; ensure idempotency when multiple workers process the same queue.

- Partitioning and routing:
  - Shard riders and drivers by geographic region or logical partition to reduce matching search scope and speed up lookups.

- Performance & caching:
  - Cache frequently read data (driver availability, recent locations) with TTL and invalidation strategies.
  - Introduce indexing and geospatial indexes (PostGIS or built-in DB features) for location-based queries.

- Observability:
  - Add metrics (Prometheus/OpenTelemetry), structured logs, and distributed tracing to monitor matching latency and system bottlenecks.

Getting started (dev)

- Requirements: .NET 8 SDK
- Run locally: dotnet run --project RideMatchingSystem
- Configuration: appsettings.json / appsettings.Development.json contains connection strings and SignalR settings.

Project structure (high level)

- RideMatchingSystem/Program.cs — app startup and DI
- RideMatchingSystem/Controllers — Web API controllers
- RideMatchingSystem/Services — business logic and in-memory helpers
- RideMatchingSystem/BackgroundServices — hosted worker(s)
- RideMatchingSystem/Hubs — SignalR hubs
- RideMatchingSystem/Data — EF Core DbContext and migrations
- RideMatchingSystem/DTOs, Models — API contracts and domain models

Contributing

- Follow existing coding style and patterns.
- Add tests for new matching logic or service behavior.

License

- Check repository license file (if any) for usage terms.

