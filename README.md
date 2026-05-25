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

## Setup

1. Restore packages:

   ```powershell
   dotnet restore
   ```

2. Configure the database connection.

   For local development, use user secrets or your local `appsettings.Development.json`:

   ```powershell
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=.;Database=MiniInstagram;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
   ```

3. Configure Redis if it is not running on `localhost:6379`:

   ```powershell
   dotnet user-secrets set "ConnectionStrings:Redis" "localhost:6379,abortConnect=false"
   ```

4. Apply migrations:

   ```powershell
   dotnet ef database update
   ```

5. Run the app:

   ```powershell
   dotnet run
   ```

## GitHub Notes

Do not commit local build output, database files, or uploaded media. The `.gitignore` file excludes `bin/`, `obj/`, `Data/*.db`, and `wwwroot/uploads/`.

Keep real passwords and connection strings out of GitHub. Use user secrets locally and environment variables in production.
