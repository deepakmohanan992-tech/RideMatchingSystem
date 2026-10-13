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
===================

Description
-----------
RideMatchingSystem is a .NET 8 web application that matches riders with available drivers. The project includes an API, SignalR hub for real-time updates, EF Core persistence and a background matching worker.

Prerequisites
-------------
- .NET 8 SDK (dotnet --version should show 8.x)
- Git
- Visual Studio 2022/2026 or VS Code (optional)
- If using EF Core migrations: dotnet-ef (dotnet tool install --global dotnet-ef)
- A database server (SQLite for demo, or SQL Server/Postgres for production)

Quick start (PowerShell)
-------------------------
1. Clone the repository
   git clone https://github.com/deepakmohanan992-tech/RideMatchingSystem.git
   cd RideMatchingSystem

2. Restore and build
   dotnet restore
   dotnet build --configuration Debug

3. Configure application settings
   - Edit appsettings.Development.json or set environment variables for connection strings and secrets.
   - Example (PowerShell):
     $env:ASPNETCORE_ENVIRONMENT = "Development"
     $env:ConnectionStrings__DefaultConnection = "Server=.;Database=RideMatchingDb;Trusted_Connection=True;"

4. Apply EF Core migrations (if applicable)
   dotnet ef database update --project RideMatchingSystem --startup-project RideMatchingSystem

5. Run the application
   dotnet run --project RideMatchingSystem --configuration Debug

6. Open in browser
   - Typical URLs: http://localhost:5000 and https://localhost:5001 (check console or launchSettings.json)

Run from Visual Studio
----------------------
- Open RideMatchingSystem.slnx
- Set the RideMatchingSystem project as startup and press F5 or Ctrl+F5

Run tests
---------
- Run all tests: dotnet test

Notes
-----
- If the project is using SQLite in Development it may create a local file (ride_matching.db) automatically.
- For production, replace the demo SQLite provider with SQL Server or Postgres and configure connection strings accordingly.
- Use environment variables or user secrets for sensitive configuration.

Contributing
------------
- Fork or create a branch, implement changes, add tests, and open a pull request against the Development branch.

License
-------
- See LICENSE in the repository root (if present) or contact the project owner for licensing details.

Want more details added?
-----------------------
If you want repository-specific sections (detailed environment variable names, seed data, deployment steps, or CI instructions), tell me what to include and I will update this README.

