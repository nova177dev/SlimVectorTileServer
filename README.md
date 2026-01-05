# Slim Vector Tile Server

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/download/dotnet/8.0)

A lightweight, high-performance vector tile server built with .NET Core that dynamically generates vector tiles from MS SQL Server database data. This server follows clean architecture principles and provides a simple API for serving MapBox Vector Tiles to web applications.

## Table of Contents

- [Features](#features)
- [Architecture](#architecture)
  - [Key Components](#key-components)
- [Prerequisites](#prerequisites)
- [Installation](#installation)
- [Configuration](#configuration)
- [Usage](#usage)
  - [API Endpoints](#api-endpoints)
  - [Web Viewer](#web-viewer)
- [Implementation Details](#implementation-details)
  - [Vector Tile Generation](#vector-tile-generation)
  - [Database Integration](#database-integration)
  - [Performance Optimization](#performance-optimization)
- [Development](#development)
  - [Project Structure](#project-structure)
  - [Adding New Features](#adding-new-features)
- [Security](#security)
- [Logging](#logging)
- [Caching](#caching)
- [Rate Limiting](#rate-limiting)
- [Troubleshooting](#troubleshooting)
- [License](#license)
- [Contributing](#contributing)

## Features

- Dynamically generates vector tiles from MS SQL Server database data
- RESTful API following the XYZ tile scheme + uuid of additional filtering params (`/tiles/{z}/{x}/{y}/{uuid}.mvt`)
- Built with .NET 8 for high performance
- Clean architecture design with CQRS pattern using MediatR
- Integrated web viewer for tile visualization using Mapbox GL JS
- GZIP compression for efficient tile delivery
- Detailed error handling and logging with Serilog
- SQL Server distributed caching for improved performance
- Rate limiting to prevent abuse (60 requests per minute per IP)
- API versioning support

## Architecture

The project follows the principles of Clean Architecture with the following layers:

- **Domain**: Contains business entities and logic
- **Application**: Contains business rules, commands, and queries
- **Infrastructure**: Contains data access implementations
- **WebApi**: Contains API controllers and endpoints

### Key Components

- **VectorTileController**: Handles API requests for vector tiles
- **TilesService**: Dynamically generates vector tiles from database data
- **GetVectorTileQueryHandler**: Processes tile requests using the CQRS pattern
- **map.html**: Web interface for viewing vector tiles using Mapbox GL JS

## Prerequisites

- .NET 8 SDK
- SQL Server database
- SQL Server database for caching (can be the same instance)
- Environment variables for configuration

## Installation

1. Clone the repository:
   ```
   git clone https://github.com/nova177dev/SlimVectorTileServer.git
   ```

2. Navigate to the project directory:
   ```
   cd SlimVectorTileServer
   ```

3. Set up environment variables:
   ```
   ConnectionStrings__SlimVectorTileServer=your_connection_string
   ConnectionStrings__SlimVectorTileServerCache=your_cache_connection_string
   ```

4. Create the cache table in your SQL Server database:
   ```sql
   CREATE TABLE [dbo].[vector_tile_cache] (
       [Id] [nvarchar](449) NOT NULL,
       [Value] [varbinary](max) NOT NULL,
       [ExpiresAtTime] [datetimeoffset](7) NOT NULL,
       [SlidingExpirationInSeconds] [bigint] NULL,
       [AbsoluteExpiration] [datetimeoffset](7) NULL,
       CONSTRAINT [pk_Id] PRIMARY KEY CLUSTERED ([Id] ASC)
   );
   ```

5. Build the project:
   ```
   dotnet build
   ```

6. Run the project:
   ```
   dotnet run
   ```

## Configuration

The application uses environment variables for configuration, which are referenced in `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "SlimVectorTileServer": "${ConnectionStrings__SlimVectorTileServer}",
    "SlimVectorTileServerCache": "${ConnectionStrings__SlimVectorTileServerCache}"
  },
  "AppSettings": {}
}
```

## Usage

### API Endpoints

- **Create Request Parameters**: `POST /api/tiles/request-params`
  - Request body: JSON object with data parameters
  - Response: JSON object with UUID for tile requests

- **Get Vector Tile**: `GET /api/tiles/{z}/{x}/{y}/{uuid}.mvt`
  - Parameters:
    - `z`: Zoom level
    - `x`: X coordinate
    - `y`: Y coordinate
    - `uuid`: UUID from the request parameters

### Web Viewer

A simple web viewer is included to visualize the tiles. Access it at:

```
http://localhost:5035/map.html
```

The web viewer allows you to:
1. Enter JSON parameters in the sidebar
2. Submit the parameters to create a request
3. View the resulting vector tiles on the map

Example request parameters:
```json
{
  "data": {
    "dma_code": 501,
    "zip_code": 10001
  }
}
```

## Implementation Details

### Vector Tile Generation

The server dynamically generates vector tiles from database data:

1. The client sends request parameters to create a request with a UUID
2. The client requests a tile with the UUID and XYZ coordinates
3. The server queries the database for points within the tile's geographic bounds
4. The server creates a vector tile with the points
5. The server compresses the tile with GZIP and returns it

### Database Integration

The server uses a stored procedure called `dbo.sites_get` to retrieve point data from the database. The procedure takes parameters for:
- x, y, z: Tile coordinates
- uuid: UUID corresponding to the request parameters

### Performance Optimization

- Parallel processing of database results using `Parallel.ForEach`
- GZIP compression for smaller tile sizes
- SQL Server distributed caching for frequently requested tiles
- Response caching headers for client-side caching
- Efficient tile generation using NetTopologySuite.IO.VectorTiles

## Development

### Project Structure

- **Application/**
  - **Common/**: Common services like TilesService, AppLogger, and JsonHelper
  - **Static/VectorTiles/**: Commands and queries for vector tiles
- **Domain/Entities/**: Business entities
- **Infrastructure/Data/**: Data access implementations
- **WebApi/Controllers/**: API controllers
- **wwwroot/**: Static files including map.html

### Adding New Features

To add new features:

1. Define entities in the Domain layer
2. Create commands/queries in the Application layer
3. Implement data access in the Infrastructure layer
4. Expose endpoints in the WebApi layer

## Security

The application uses JWT authentication for secure access to protected endpoints. Public endpoints like the vector tile API are marked with `[AllowAnonymous]` to allow access without authentication.

## Logging

The application uses Serilog for structured logging:

- Console logging for development
- JSON file logging for production
- Log rotation by day

```csharp
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .WriteTo.Console()
    .WriteTo.File(new JsonFormatter(), "Logs/applog-.json", rollingInterval: RollingInterval.Day)
    .CreateLogger();
```

## Caching

SQL Server distributed caching with configurable zoom-level expiration:

| Zoom Level | Default Expiration |
|------------|-------------------|
| 0-3 | 7 days (168 hours) |
| 4-6 | 3 days (72 hours) |
| 7-10 | 24 hours |
| 11+ | Caching disabled |

Configure via `CacheSettings.ZoomLevelExpirations` and `CacheSettings.MaxCacheZoomLevel`.

## Rate Limiting

Fixed window rate limiting using `System.Threading.RateLimiting`:

- **Default**: 600 requests per minute per IP
- **Configurable**: Via `AppSettings.RateLimitPerMinute`
- **Partition**: By `RemoteIpAddress` or "anonymous"

## Troubleshooting

### Common Issues

1. **Database Connection Issues**
   - Verify connection strings in environment variables or `.env` file
   - Ensure SQL Server is running with spatial data support enabled
   - Check firewall settings and SQL Server authentication mode

2. **Missing Vector Tiles**
   - Verify stored procedures exist (`dbo.sites_get`, `dbo.polygons_get`)
   - Check that coordinates are within valid tile ranges
   - Examine `Logs/applog-*.json` for detailed errors

3. **Performance Issues**
   - Ensure cache table has proper indexes
   - Adjust `TileSettings.MaxDegreeOfParallelism` for your server
   - Add spatial indexes to your data tables
   - Consider adjusting `CacheSettings.MaxCacheZoomLevel`

4. **Antimeridian Issues**
   - Geometries crossing ±180° are automatically normalized
   - Check debug output for "Error handling antimeridian crossing" messages

### Debugging

- Enable Development environment for detailed Debug output
- Check `Logs/` directory for JSON-formatted error logs
- Use Swagger UI at `/swagger` to test API endpoints
- Use browser DevTools Network tab to inspect tile responses

## License

This project is licensed under the MIT License - see the [LICENSE.txt](LICENSE.txt) file for details.

Copyright (c) 2025 Anton V. Novoseltsev

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request
