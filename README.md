# Order Service API

A REST API for order management with inventory control, built with .NET 10+, ASP.NET Core, Entity Framework Core, Docker and PostgreSQL.

## Features

- **Product Management**: Create and retrieve products with stock control
- **Order Management**: Create, confirm, cancel, and retrieve orders
- **Inventory Control**: Real-time stock validation and reservation
- **JWT Authentication**: Secure API endpoints with JWT tokens
- **Clean Architecture**: Layered architecture following SOLID principles
- **Async/Await**: End-to-end async operations
- **Database Migrations**: Automatic schema management with EF Core
- **Docker Support**: Full containerization with Docker Compose
- **Comprehensive Tests**: Unit tests and PostgreSQL-backed integration tests
- **API Documentation**: Swagger/OpenAPI integration

## Tech Stack

- **.NET**: 10.0+
- **Language**: C#
- **Framework**: ASP.NET Core Web API
- **ORM**: Entity Framework Core 10.0+
- **Database**: PostgreSQL 15
- **Authentication**: JWT (JSON Web Tokens)
- **Testing**: xUnit, Moq, FluentAssertions, Testcontainers
- **Container**: Docker, Docker Compose
- **API Documentation**: Swagger/Swashbuckle

## Project Structure

```
OrderService/
├── OrderService.Api/                 # API layer (controllers)
│   ├── Controllers/
│   ├── Wrappers/
│   ├── appsettings.json
│   └── Program.cs
├── OrderService.Application/         # Application layer (use cases, DTOs)
│   ├── Commands/
│   ├── DTOs/
│   ├── Handlers/
│   ├── Queries/
│   ├── Requests/
│   ├── Validators/
│   ├── Wrappers/
│   └── ApplicationServices.cs
├── OrderService.Domain/              # Domain layer (entities, value objects)
│   ├── Entities/
│   ├── Enums/
│   ├── Repositories/                 # Repository interfaces
│   └── DomainException.cs
├── OrderService.Infrastructure/      # Infrastructure layer (DB, Auth)
│   ├── Auth/
│   ├── Data/
│   ├── Migrations/
│   ├── Repositories/
│   └── InfrastructureServices.cs
|
|   # Tests layers (FluentAssertions, xUnit)
|
├── OrderService.Tests.Api/
│   └── Controllers/
├── OrderService.Tests.Application/
│   ├── Handlers/
│   ├── Helpers/
│   └── Validators/
├── OrderService.Tests.Domain/
│   ├── Builders/
│   └── Entities/
├── OrderService.Tests.Infrastructure/
│   └── Repositories/
├── OrderService.Tests.Integration/
│   ├── Infrastructure/
│   ├── Models/
│   └── Scenarios/
├── Dockerfile
└── docker-compose.yml
```

## Quick Start

### Prerequisites

- Docker and Docker Compose installed
- Alternatively: .NET 10.0+ SDK and PostgreSQL 15+

### Option 1: Docker Compose (Recommended)

```bash
docker-compose up --build
```
If you dont have docker-compose, you can try to run with the docker command directly:

```bash
docker compose up --build
```

This will:
- Start PostgreSQL database
- Build and run the API
- Apply database migrations automatically
- Expose API on `http://localhost:8080`

### Option 2: Local Development

```bash
# Install dependencies
dotnet restore

# Build
dotnet build

# Update database
dotnet ef database update --project OrderService.Infrastructure --startup-project OrderService.Api

# Run the API
dotnet run --project OrderService.Api
```

## API Endpoints

### Authentication

```
POST /auth/token
Content-Type: application/json

{
  "email": "fabian@server.com",
  "password": "superSenhaForte123@"
}

Response: { "token": "...", "expiresIn": 3600 }
```

### Orders

All order endpoints require JWT token in Authorization header:
```
Authorization: Bearer <token>
```

#### Create Order

```
POST /orders
Content-Type: application/json

{
  "customerId": "550e8400-e29b-41d4-a716-446655440000",
  "currency": "USD",
  "items": [
    {
      "productId": "550e8400-e29b-41d4-a716-446655440001",
      "quantity": 2
    }
  ]
}

Response: 201 Created
{
  "id": "550e8400-e29b-41d4-a716-446655440010",
  "customerId": "550e8400-e29b-41d4-a716-446655440000",
  "status": "Placed",
  "currency": "USD",
  "items": [...],
  "total": 299.98,
  "createdAt": "2026-04-02T10:30:00Z",
  "updatedAt": "2026-04-02T10:30:00Z"
}
```

#### Get Order by ID

```
GET /orders/{id}
Response: 200 OK - Order details
```

#### List Orders

```
GET /orders?customerId=&status=Placed&fromDate=2026-04-01&toDate=2026-04-03&pageNumber=1&pageSize=10

Query Parameters:
- customerId (optional): Filter by customer
- status (optional): Filter by status (Placed, Confirmed, Canceled)
- fromDate (optional): Filter orders created after this date
- toDate (optional): Filter orders created before this date
- pageNumber (optional): Page number (default: 1)
- pageSize (optional): Items per page (default: 10)

Response: 200 OK
{
  "items": [...],
  "total": 45,
  "pageNumber": 1,
  "pageSize": 10,
  "totalPages": 5
}
```

#### Confirm Order

```
POST /orders/{id}/confirm
Response: 200 OK - Updated order
```

#### Cancel Order

```
POST /orders/{id}/cancel
Response: 200 OK - Cancelled order
```

### Products

All product endpoints require JWT token in Authorization header:
```
Authorization: Bearer <token>
```

#### Create Product

```
POST /products
Content-Type: application/json

{
  "name": "Wireless Headphones",
  "unitPrice": 149.99,
  "availableQuantity": 200,
}

Response: 201 Created
{
  "id": "550e8400-e29b-41d4-a716-446655440020",
  "name": "Wireless Headphones",
  "unitPrice": 149.99,
  "availableQuantity": 200,
  "isActive": true,
  "createdAt": "2026-04-03T10:00:00Z"
}
```

#### Get Product by ID

```
GET /products/{id}
Response: 200 OK - Product details
```

#### List Products

```
GET /products?name=headphones&pageNumber=1&pageSize=10

Query Parameters:
- name (optional): Filter by name (contains, case-insensitive)
- pageNumber (optional): Page number (default: 1)
- pageSize (optional): Items per page (default: 10)

Response: 200 OK
{
  "items": [...],
  "total": 3,
  "pageNumber": 1,
  "pageSize": 10,
  "totalPages": 1
}
```

## Running Tests

```bash
# Run all tests
dotnet test

# Run only integration tests (requires Docker running locally)
dotnet test OrderService.Tests.Integration/OrderService.Tests.Integration.csproj

# Run tests with coverage on windows
dotnet test /p:CollectCoverage=true

# Run tests with coverage on linux/mac
dotnet test --collect:"XPlat Code Coverage"
```

## Integration Tests

Integration tests are implemented in `OrderService.Tests.Integration` and validate real HTTP flows against a real PostgreSQL database provisioned by Testcontainers.

Current covered scenarios:
- Anonymous user can generate token on `/auth/token` and is denied (`401`) on protected endpoints.
- Authenticated endpoints reject invalid bearer tokens.
- Happy path flow: create product, verify listing, reject order with insufficient stock, create valid order, confirm order, cancel order, and validate stock/state transitions.

Notes:
- The integration fixture boots the API using `WebApplicationFactory<Program>`.
- A PostgreSQL container (`postgres:15-alpine`) is started/stopped automatically during test execution.
- Run with Docker daemon available locally.

## Database Schema

### Orders Table
- **Id** (UUID): Primary key
- **CustomerId** (UUID): Customer identifier
- **Status** (int): Order status (0=Draft, 1=Placed, 2=Confirmed, 3=Canceled)
- **Currency** (varchar(3)): Order currency
- **Total** (decimal): Order total amount
- **CreatedAt** (datetime): Creation timestamp
- **UpdatedAt** (datetime): Last update timestamp

### OrderItems Table
- **Id** (UUID): Primary key
- **OrderId** (UUID): Foreign key to Orders
- **ProductId** (UUID): Product identifier
- **UnitPrice** (decimal): Price per unit
- **Quantity** (int): Quantity ordered

### Products Table
- **Id** (UUID): Primary key
- **Name** (varchar(255)): Product name (unique)
- **UnitPrice** (decimal): Unit price
- **AvailableQuantity** (int): Stock available
- **Currency** (varchar(3)): Product currency
- **IsActive** (bool): Product active flag
- **CreatedAt** (datetime): Creation timestamp

## Security

- **JWT Authentication**: All endpoints except `/auth/token` require valid JWT
- **Password Hashing**: In production, we should implement proper password hashing
- **HTTPS**: Use HTTPS in production environments
- **CORS**: Configured to allow cross-origin requests (modify as needed)
- **Input Validation**: All inputs validated using FluentValidation

## Configuration

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=orderservicedb;Username=postgres;Password=postgres123"
  },
  "JwtSettings": {
    "Key": "your-super-secret-key-that-is-very-long-and-secure",
    "Issuer": "OrderService",
    "Audience": "OrderServiceUsers",
    "ExpirationMinutes": 60
  }
}
```

## Development Workflow

1. **Create feature branch**: `git checkout -b feature/your-feature`
2. **Make changes**: Implement feature with tests
3. **Run tests**: `dotnet test`
4. **Build**: `dotnet build`
5. **Add changes**: `git add .`
6. **Commit**: `git commit -am "Add feature"`
7. **Push**: `git push origin feature/your-feature`
8. **Create PR**: Submit pull request

## Troubleshooting

### Docker Connection Issues
If the API cannot connect to the database, ensure:
- PostgreSQL service is healthy: `docker-compose ps`
- Connection string matches compose setup
- Network is properly configured

### Migration Errors
Reset the database:
```bash
dotnet ef database reset --project OrderService.Infrastructure --startup-project OrderService.Api --force
```

### Port Conflicts
If port 8080 is already in use:
- Change `ports` in docker-compose.yml
- Or specify port when running locally: `dotnet run --urls "http://localhost:8090"`

## Performance Considerations

- **Database Indexing**: Product name is indexed for faster searches
- **Connection Pooling**: EF Core automatically manages connection pools
- **Async Operations**: All I/O operations are asynchronous
- **Query Optimization**: Queries include necessary includes for related entities
- **Pagination**: Large result sets are paginated

## Future Enhancements

- [ ] Distributed caching (Redis)
- [ ] Message queue integration (RabbitMQ/Azure Service Bus)
- [ ] Advanced search and filtering
- [ ] Order history and audit logging
- [ ] Payment integration
- [ ] Email notifications
- [ ] Admin dashboard
- [ ] Analytics and reporting

## License

MIT

## Support

For issues, questions, or contributions, please open an issue or contact the development team.
