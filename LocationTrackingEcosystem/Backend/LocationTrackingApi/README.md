# Location Tracking API

A comprehensive ASP.NET Core 8 Web API for real-time GPS location tracking with JWT authentication and SignalR integration.

## 🏗️ Architecture

**Security First** - Built with JWT authentication, role-based access control, and secure real-time communication.

### Key Features

- **JWT Authentication** - Secure token-based authentication
- **Real-time Location Tracking** - SignalR integration for live GPS updates
- **Entity Framework Core** - Code-first database approach with SQL Server
- **ASP.NET Core Identity** - User management and authentication
- **Clean Architecture** - Layered design with separation of concerns
- **Comprehensive Logging** - Structured logging with multiple providers
- **API Documentation** - Swagger/OpenAPI with JWT security integration

## 🚀 Quick Start

### Prerequisites

- .NET 8 SDK
- SQL Server LocalDB (or SQL Server)
- Visual Studio 2022 or VS Code

### Installation

1. **Clone and Navigate**
   ```bash
   cd LocationTrackingEcosystem/Backend/LocationTrackingApi
   ```

2. **Install Dependencies**
   ```bash
   dotnet restore
   ```

3. **Database Setup**
   ```bash
   # Database will be created automatically on first run
   # Or create manually:
   dotnet ef database update
   ```

4. **Run the Application**
   ```bash
   dotnet run
   ```

5. **Access the API**
   - **Swagger UI**: https://localhost:7246 (or http://localhost:5049)
   - **SignalR Hub**: https://localhost:7246/hubs/location
   - **Health Check**: https://localhost:7246/health

## 📡 API Endpoints

### Authentication (`/api/auth`)

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| POST | `/register` | Register new user | ❌ |
| POST | `/login` | Authenticate user | ❌ |
| POST | `/logout` | Logout user | ✅ |
| GET | `/me` | Get current user info | ✅ |

### Location Tracking (`/api/location`)

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| POST | `/update` | Update user location | ✅ |
| GET | `/online-users` | Get all online users | ✅ |
| GET | `/history` | Get own location history | ✅ |
| GET | `/history/{userId}` | Get user location history | ✅ |
| GET | `/current/{userId}` | Get user current location | ✅ |
| POST | `/offline` | Set user offline | ✅ |

## 🔐 Authentication

The API uses JWT Bearer tokens for authentication. Include the token in the Authorization header:

```
Authorization: Bearer {your-jwt-token}
```

### Example Registration/Login

```bash
# Register
curl -X POST "https://localhost:7246/api/auth/register" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "password": "Password123!",
    "displayName": "John Doe"
  }'

# Login
curl -X POST "https://localhost:7246/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "password": "Password123!"
  }'
```

## 📍 Location Updates

### REST API Location Update

```bash
curl -X POST "https://localhost:7246/api/location/update" \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "latitude": 37.7749,
    "longitude": -122.4194,
    "accuracy": 5.0,
    "altitude": 100.0,
    "speed": 0.0,
    "bearing": 0.0
  }'
```

### SignalR Real-time Updates

The SignalR hub (`/hubs/location`) provides real-time location broadcasting:

**Client Events (Send to Server):**
- `UpdateLocation` - Send location update
- `JoinTrackingGroup` - Join a tracking group
- `LeaveTrackingGroup` - Leave a tracking group
- `GetOnlineUsers` - Request online users list

**Server Events (Receive from Server):**
- `LocationUpdated` - User location updated
- `UserConnected` - User came online
- `UserDisconnected` - User went offline
- `LocationUpdateConfirmed` - Location update confirmation
- `OnlineUsersList` - List of online users

## 🛠️ Configuration

### Database Connection

Update `appsettings.json` for production or `appsettings.Development.json` for development:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=your-server;Database=LocationTrackingDb;Trusted_Connection=true;"
  }
}
```

### JWT Settings

```json
{
  "JwtSettings": {
    "SecretKey": "your-256-bit-secret-key",
    "Issuer": "LocationTrackingApi",
    "Audience": "LocationTrackingClients",
    "ExpiryMinutes": 60
  }
}
```

### CORS Configuration

```json
{
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:4200",
      "https://your-frontend-domain.com"
    ]
  }
}
```

## 🏗️ Project Structure

```
LocationTrackingApi/
├── Controllers/         # API Controllers
│   ├── AuthController.cs
│   └── LocationController.cs
├── Data/               # Database Context
│   └── ApplicationDbContext.cs
├── DTOs/               # Data Transfer Objects
│   ├── AuthDto.cs
│   └── LocationDto.cs
├── Hubs/               # SignalR Hubs
│   └── LocationHub.cs
├── Models/             # Entity Models
│   ├── ApplicationUser.cs
│   └── LocationRecord.cs
├── Services/           # Business Logic
│   ├── AuthService.cs
│   └── LocationService.cs
└── Program.cs          # Application Entry Point
```

## 🔧 Development

### Adding Migrations

```bash
# Add new migration
dotnet ef migrations add MigrationName

# Update database
dotnet ef database update

# Remove last migration
dotnet ef migrations remove
```

### Running Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Development Tools

- **Hot Reload**: `dotnet watch run`
- **Database Tools**: SQL Server Management Studio or Azure Data Studio
- **API Testing**: Swagger UI, Postman, or curl
- **Real-time Testing**: Browser developer tools or SignalR client

## 📊 Performance Features

- **Database Indexing** - Optimized queries for location and user data
- **Connection Pooling** - Efficient database connections
- **Pagination** - Limited result sets for large datasets
- **Background Cleanup** - Automatic old data cleanup
- **Caching Headers** - Optimized HTTP response caching

## 🔒 Security Features

- **JWT Token Validation** - Secure authentication
- **CORS Protection** - Controlled cross-origin access
- **Input Validation** - Comprehensive request validation
- **Password Hashing** - ASP.NET Core Identity security
- **Rate Limiting** - Built-in request throttling
- **HTTPS Enforcement** - Secure communication

## 📝 Logging

The API includes comprehensive logging:

- **Request/Response Logging** - HTTP request tracking
- **Authentication Events** - Login/logout tracking
- **Location Updates** - GPS tracking events
- **Error Logging** - Exception and error tracking
- **Performance Metrics** - Operation timing

Log levels can be configured in `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "LocationTrackingApi": "Debug",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  }
}
```

## 🚀 Deployment

### Docker Deployment

```dockerfile
# Build and run with Docker
docker build -t location-tracking-api .
docker run -p 5000:80 location-tracking-api
```

### Azure Deployment

1. Publish to Azure App Service
2. Configure SQL Database connection
3. Set environment variables for JWT and CORS
4. Enable Application Insights for monitoring

## 🤝 Integration

This backend API is designed to work with:

- **Mobile App**: .NET MAUI cross-platform application
- **Web Dashboard**: Angular SPA with real-time maps
- **Third-party Services**: Webhook integration for location events

## 📚 Documentation

- **API Documentation**: Available at `/swagger` endpoint
- **SignalR Hub**: Real-time connection at `/hubs/location`
- **Health Checks**: Monitor at `/health` endpoint

## 🛡️ Security Considerations

- Store JWT secret key securely (Azure Key Vault in production)
- Use HTTPS in production
- Implement rate limiting for public endpoints
- Regular security audits and dependency updates
- Monitor for suspicious location update patterns

---

## Next Steps

Once the backend is running, proceed to create:

1. **Mobile App** - .NET MAUI application for GPS tracking
2. **Web Dashboard** - Angular application for location visualization
3. **Integration Testing** - End-to-end testing across all platforms
