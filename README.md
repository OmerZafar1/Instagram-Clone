# MiniInstagram

MiniInstagram is an ASP.NET Core Blazor Server social app inspired by Instagram. It includes posts, stories, profiles, follows, saved posts, comments, chat, voice messages, notifications, calls, trending content, rate limiting, and spam detection.

## Tech Stack

- ASP.NET Core / Blazor Server on .NET 9
- Entity Framework Core with SQL Server
- ASP.NET Core Identity
- SignalR for realtime chat, calls, presence, and notifications
- Hangfire for background jobs
- Redis for presence, rate limiting, and trending data

## Features

- User registration and login
- Profile editing with avatars and privacy controls
- Feed, explore, trending, saved posts, and post details
- Image/video posts with comments, likes, saves, and visibility controls
- Stories with automatic cleanup
- Follow requests for private accounts
- Realtime notifications
- Realtime chat with voice messages
- Video/audio call session support
- Redis-backed rate limiting and spam detection

## Prerequisites

- .NET 9 SDK
- SQL Server
- Redis
- Optional: Docker, if you want the quickest Redis setup

## Setup

1. Clone the repository:

   ```powershell
   git clone https://github.com/OmerZafar1/Instagram-Clone.git
   cd Instagram-Clone
   ```

2. Restore packages:

   ```powershell
   dotnet restore
   ```

3. Configure SQL Server.

   The app uses SQL Server through Entity Framework Core. Create/use a local SQL Server instance and configure the connection string with .NET user secrets:

   ```powershell
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=.;Database=MiniInstagram;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
   ```

   If you use SQL authentication instead of Windows authentication:

   ```powershell
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=.;Database=MiniInstagram;User Id=YOUR_USER;Password=YOUR_PASSWORD;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
   ```

4. Start Redis.

   Redis is used for presence, rate limiting, and trending data. The default connection is `localhost:6379`.

   Quick Docker option:

   ```powershell
   docker run --name miniinstagram-redis -p 6379:6379 -d redis:latest
   ```

   If the container already exists:

   ```powershell
   docker start miniinstagram-redis
   ```

   If your Redis server uses a different host, port, or password, update the Redis connection string:

   ```powershell
   dotnet user-secrets set "ConnectionStrings:Redis" "localhost:6379,abortConnect=false"
   ```

5. Install the EF Core CLI tool if you do not already have it:

   ```powershell
   dotnet tool install --global dotnet-ef
   ```

   If it is already installed, update it:

   ```powershell
   dotnet tool update --global dotnet-ef
   ```

6. Apply database migrations:

   ```powershell
   dotnet ef database update
   ```

7. Run the app:

   ```powershell
   dotnet run
   ```

8. Open the local URL shown in the terminal. It is usually one of these:

   ```text
   https://localhost:5001
   http://localhost:5000
   ```

## Background Jobs

The project uses Hangfire for background jobs and SQL Server for Hangfire storage. In development, the Hangfire dashboard is available at:

```text
/hangfire
```

For example:

```text
https://localhost:5001/hangfire
```

## Redis Notes

Redis must be reachable before using realtime presence, rate limiting, and trending features. The app reads Redis from:

```json
"ConnectionStrings": {
  "Redis": "localhost:6379,abortConnect=false"
}
```

For production, set this as an environment variable or secret instead of committing a real password:

```powershell
$env:ConnectionStrings__Redis="your-redis-host:6379,password=YOUR_PASSWORD,abortConnect=false"
```

## Troubleshooting

- If `dotnet run` fails because SQL Server cannot connect, check that SQL Server is running and that your `DefaultConnection` user secret is correct.
- If Redis errors appear, start Redis and confirm it is listening on port `6379`.
- If `dotnet ef database update` is not recognized, install `dotnet-ef` with the command above.
- If a build fails because `MiniInstagram.exe` or `MiniInstagram.dll` is locked, stop the running app and build again.
- If uploads do not appear, make sure the app can create folders under `wwwroot/uploads/`.

## GitHub Notes

Do not commit local build output, database files, or uploaded media. The `.gitignore` file excludes `bin/`, `obj/`, `Data/*.db`, and `wwwroot/uploads/`.

Keep real passwords and connection strings out of GitHub. Use user secrets locally and environment variables in production.
