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
