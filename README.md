# MiniInstagram

MiniInstagram is an ASP.NET Core Blazor Server social app inspired by Instagram. It includes posts, stories, profiles, follows, saved posts, comments, chat, voice messages, notifications, calls, trending content, rate limiting, and spam detection.

The app uses a **polyglot persistence** architecture: **SQL Server** for structured relational data, **MongoDB** for high-volume chat and notifications, and **Redis** for ephemeral realtime features.

## Tech Stack

- ASP.NET Core / Blazor Server on .NET 9
- **SQL Server** + Entity Framework Core (users, posts, likes, comments, follows, stories, conversations)
- **MongoDB** (chat messages, notifications)
- ASP.NET Core Identity
- SignalR for realtime chat, calls, presence, and notifications
- Hangfire for background jobs (SQL Server storage)
- Redis for presence, rate limiting, and trending cache

## Data Architecture

| Store | Used for |
|-------|----------|
| **SQL Server** | Login, users, posts, likes, comments, saves, follows, stories, conversation metadata |
| **MongoDB** | Chat messages (`chat_messages`), notifications (`notifications`) |
| **Redis** | Online / last-seen, trending cache, rate limiting |
| **Local disk** | Uploaded images, videos, avatars, voice notes (`wwwroot/uploads/`) |

## Features

- User registration and login
- Profile editing with avatars and privacy controls
- Feed, explore, trending, saved posts, and post details
- Image/video posts with comments, likes, saves, and visibility controls
- Stories with automatic cleanup
- Follow requests for private accounts
- Realtime notifications (MongoDB)
- Realtime chat with voice messages (MongoDB)
- Video/audio call session support
- Redis-backed rate limiting and spam detection

## Prerequisites

- .NET 9 SDK
- SQL Server
- MongoDB (local or Atlas)
- Redis
- Optional: Docker for Redis and/or MongoDB

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

4. Configure MongoDB.

   Chat messages and notifications are stored in MongoDB. Default settings in `appsettings.json`:

   ```json
   "MongoDb": {
     "ConnectionString": "mongodb://localhost:27017",
     "DatabaseName": "MiniInstagram"
   }
   ```

   Override with user secrets if needed:

   ```powershell
   dotnet user-secrets set "MongoDb:ConnectionString" "mongodb://localhost:27017"
   dotnet user-secrets set "MongoDb:DatabaseName" "MiniInstagram"
   ```

   Quick Docker option:

   ```powershell
   docker run --name miniinstagram-mongo -p 27017:27017 -d mongo:latest
   ```

   If the container already exists:

   ```powershell
   docker start miniinstagram-mongo
   ```

   MongoDB indexes are created automatically on app startup.

5. Start Redis.

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

6. Install the EF Core CLI tool if you do not already have it:

   ```powershell
   dotnet tool install --global dotnet-ef
   ```

   If it is already installed, update it:

   ```powershell
   dotnet tool update --global dotnet-ef
   ```

7. Apply database migrations (SQL Server):

   ```powershell
   dotnet ef database update
   ```

   Migrations also run automatically when the app starts.

8. Run the app:

   ```powershell
   dotnet run
   ```

9. Open the local URL shown in the terminal. Default ports from `launchSettings.json`:

   ```text
   https://localhost:7059
   http://localhost:5126
   ```

## Background Jobs

The project uses Hangfire for background jobs and SQL Server for Hangfire storage. In development, the Hangfire dashboard is available at:

```text
/hangfire
```

For example:

```text
https://localhost:7059/hangfire
```

## MongoDB Notes

- Collections: `chat_messages`, `notifications`
- Conversation metadata (participants) stays in SQL Server; message bodies live in MongoDB
- Use MongoDB Compass to inspect data after sending a message or receiving a notification
- For production, set `MongoDb__ConnectionString` as an environment variable instead of committing credentials

```powershell
$env:MongoDb__ConnectionString="mongodb://user:password@host:27017"
$env:MongoDb__DatabaseName="MiniInstagram"
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
- If chat or notifications fail, confirm MongoDB is running on port `27017` (or your configured connection string).
- If Redis errors appear, start Redis and confirm it is listening on port `6379`.
- If `dotnet ef database update` is not recognized, install `dotnet-ef` with the command above.
- If a build fails because `MiniInstagram.exe` or `MiniInstagram.dll` is locked, stop the running app and build again.
- If uploads do not appear, make sure the app can create folders under `wwwroot/uploads/`.

## GitHub Notes

Do not commit local build output, database files, or uploaded media. The `.gitignore` file excludes `bin/`, `obj/`, `Data/*.db`, and `wwwroot/uploads/`.

Keep real passwords and connection strings out of GitHub. Use user secrets locally and environment variables in production.

## About

ASP.NET Core Blazor Instagram clone with SQL Server, MongoDB, Redis, SignalR, and Hangfire.
