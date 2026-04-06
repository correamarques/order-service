# Technical Decisions

## Architecture

### Clean Architecture with SOLID Principles

**Decision**: Implemented an architecture with clear separation of concerns.

**Rationale**:
- **Maintainability**: Each layer has a single responsibility
- **Testability**: Business logic is isolated and easily testable
- **Scalability**: Easy to add new features without affecting existing code
- **Independence**: Domain logic is independent of frameworks and infrastructure

**Layers**:
- **API**: Controllers, dependency injection wiring
- **Application**: DTOs, command/query handlers, and validators
- **Domain**: Core business logic, entities, and value objects
- **Infrastructure**: Data access, repositories, and token generation

---

## Data Access

### Entity Framework Core with Repository Pattern

**Decision**: Use EF Core as ORM with Repository pattern for data access.

**Rationale**:
- **Abstraction**: Repositories abstract data access logic
- **Testability**: Easy to mock repositories in tests
- **Flexibility**: Can switch implementations without affecting application logic
- **Performance**: EF Core provides good balance of productivity and performance

**Key Implementations**:
- `IOrderRepository` and `IProductRepository` interfaces defined in Domain
- `OrderRepository` and `ProductRepository` implementations in Infrastructure
- Unit of Work pattern for coordinating multiple repositories

### PostgreSQL Database

**Decision**: Use PostgreSQL as the relational database.

**Rationale**:
- **Reliability**: Proven, stable database with strong ACID compliance
- **Features**: JSON support, extensibility, excellent async support
- **Performance**: Optimized for complex queries
- **Open Source**: No licensing costs

---

## Business Logic

### Inventory Management

**Decision**: Implement inventory validation and reservation at the order creation and confirmation level.

**Rationale**:
- **Consistency**: Prevents overselling by checking stock before order placement
- **Transactionality**: Stock is reserved when orders are confirmed
- **Idempotency**: Stock is released when orders are cancelled
- **Audit Trail**: All stock changes are tracked in the database

**Implementation**:
- Stock held as `AvailableQuantity` on Product entity
- Stock validation occurs in `CreateOrderCommandHandler`
- Stock reservation on `ConfirmOrder`
- Stock release on `CancelOrder`

### Order State Machine

**Decision**: Implement order statuses as enum with state transitions enforced at domain level.

**Rationale**:
- **Type Safety**: Using enum prevents invalid states
- **Domain Driven Design**: Business rules are in the domain model
- **Validation**: State transitions validated in domain entities

**States**:
- `Draft` (0): Initial state (unused in current implementation)
- `Placed` (1): Order placed, awaiting confirmation
- `Confirmed` (2): Order confirmed, inventory reserved
- `Canceled` (3): Order cancelled, inventory released

---

## API Design

### RESTful API with Pagination

**Decision**: Implement REST API with proper pagination for list endpoints.

**Rationale**:
- **Scalability**: Pagination prevents loading large datasets into memory
- **Performance**: Clients can request data in manageable chunks
- **Standard**: Follows REST conventions

**Pagination Implementation**:
- `pageNumber` and `pageSize` query parameters (default: 1, 10)
- Response includes total count and calculated total pages
- `PaginatedResult<T>` generic response wrapper

### JWT Authentication

**Decision**: Use JWT tokens for stateless authentication.

**Rationale**:
- **Scalability**: No server-side session storage required
- **Distributed**: Works well with microservices and load balancing
- **Security**: Signed tokens cannot be tampered with
- **Standard**: Industry standard for API authentication

**Implementation**:
- `/auth/token` endpoint issues tokens
- Tokens include user ID and email claims
- Configurable expiration (default: 60 minutes)
- HS256 algorithm for signing

---

## Validation

### FluentValidation

**Decision**: Use FluentValidation for comprehensive input validation.

**Rationale**:
- **Expressiveness**: Fluent API makes validation rules readable
- **Reusability**: Validators can be shared across commands and queries
- **Composability**: Validators can be combined
- **Error Messages**: Provides detailed, localized error messages

**Validators Implemented**:
- `CreateOrderCommandValidator`: Validates order creation
- `ConfirmOrderCommandValidator`: Validates order confirmation
- `CancelOrderCommandValidator`: Validates order cancellation

---

## Asynchronous Processing

### Async/Await Throughout

**Decision**: Use async/await for all I/O-bound operations.

**Rationale**:
- **Performance**: Thread pool threads are freed while waiting for I/O
- **Scalability**: Application can handle more concurrent requests
- **Responsiveness**: UI/API remains responsive during I/O operations

**Implementation**:
- All repository methods are async (`Async` suffix)
- All handlers inherit from `IRequestHandler<TRequest, TResponse>`
- Database operations use `SaveChangesAsync()`

---

## Testing Strategy

### Unit Tests with xUnit

**Decision**: Use xUnit for unit testing with Moq for mocking and FluentAssertions for assertions.

**Rationale**:
- **xUnit**: Modern, attribute-based testing framework
- **Moq**: Simple, intuitive mocking library
- **FluentAssertions**: Readable assertion syntax

**Testing Approach**:
- Tests for domain entity invariants
- Tests for command handlers
- Tests for query handlers
- Tests for validation

### Integration Tests with Real PostgreSQL

**Decision**: Implement API integration tests in a dedicated project (`OrderService.Tests.Integration`) using `WebApplicationFactory<Program>` and Testcontainers for PostgreSQL.

**Rationale**:
- **Separation of concerns**: Keeps fast unit/controller tests separate from end-to-end API tests.
- **Higher fidelity**: Uses real HTTP pipeline + authentication + middleware + EF Core + PostgreSQL.
- **Reliability**: Testcontainers provides isolated, reproducible database lifecycle per test run.
- **Safety**: Integration tests validate behavior without changing production startup/dependency wiring.

**Implementation**:
- New test project: `OrderService.Tests.Integration`
- Infrastructure fixture starts `postgres:15-alpine` container automatically.
- API host is bootstrapped with `WebApplicationFactory<Program>` and environment-based configuration for test connection/JWT settings.
- Scenarios covered:
	- Anonymous user access control
	- Invalid token rejection
	- Happy path from product creation through order confirmation/cancellation with stock assertions

---

## Deployment

### Docker Containerization

**Decision**: Use Docker and Docker Compose for containerization and local development.

**Rationale**:
- **Consistency**: Same environment everywhere (dev, test, prod)
- **Simplicity**: Single command to start entire stack
- **Infrastructure as Code**: compose file documents the setup
- **Isolation**: Services run in isolated containers

**Configuration**:
- Multi-stage Dockerfile for optimized image size
- Alpine base images for minimal footprint
- Docker Compose with PostgreSQL and API services
- Automatic migration running on startup
- Health checks for service readiness

### Database Migrations

**Decision**: Use EF Core migrations with automatic application on startup.

**Rationale**:
- **Version Control**: Schema changes tracked in git
- **Automation**: No manual SQL needed
- **Rollback**: Easy to revert changes with migration system
- **Consistency**: Same schema across all environments

**Implementation**:
- Migrations run automatically when API starts
- `Program.cs` calls `dbContext.Database.MigrateAsync()`
- Migrations stored in `Infrastructure/Migrations/`

---

## Error Handling

### Exception-Based Error Handling

**Decision**: Use typed exceptions for domain errors and validation.

**Rationale**:
- **Clarity**: Different exception types for different error categories
- **Handling**: Can catch specific exception types
- **Messages**: Descriptive error messages for debugging

**Exception Types**:
- `DomainException`: For domain violations
- `ValidationException` (FluentValidation): For input validation errors
- Standard exceptions: For unexpected errors

---

## Configuration Management

### Layered Configuration

**Decision**: Use `appsettings.json` for configuration with environment-specific overrides.

**Rationale**:
- **Flexibility**: Different configs per environment
- **Security**: Secrets can be injected at runtime
- **Simplicity**: Standard ASP.NET Core approach

**Key Settings**:
- Database connection string
- JWT configuration (key, issuer, audience, expiration)
- Logging levels

---

## Future Considerations

### Potential Enhancements

1. **Caching**: Implement Redis caching for frequently accessed data
2. **Event Sourcing**: Track all order changes as events
3. **CQRS**: Separate command and query models for scalability
4. **Message Queue**: Integrate RabbitMQ/Azure Service Bus for async processing
5. **Monitoring**: Add Application Insights or similar for observability
6. **API Versioning**: Implement versioning strategy (URL, header-based)
7. **GraphQL**: Consider GraphQL alongside REST API
8. **Distributed Tracing**: Add tracing with correlation IDs

---

## Assumptions and Constraints

### Assumptions

- Single-tenant system (one organization)
- Orders are for physical products (no services, subscriptions)
- Customers already exist (no customer creation in scope)
- Authentication is simplified (no password validation)

### Constraints

- No historical data - orders are current state only
- No payment processing integration
- No shipping/fulfillment integration
- Simplified audit logging (only timestamps, no user tracking)
- No role-based authorization (all authenticated users have same permissions)

---

## Security Considerations

### Implemented

- JWT token-based authentication
- CORS policy configured
- Input validation on all endpoints
- Error messages don't expose sensitive information
- HTTPS ready (configured in docker-compose)

### Recommended for Production

- Enable HTTPS/TLS
- Implement rate limiting
- Add request/response logging (sensitive data redaction)
- SQL injection prevention (parameterized queries via EF Core)
- OWASP compliance audit
- Security headers (X-Content-Type-Options, X-Frame-Options, etc.)
- API key management
- Encryption of sensitive data at rest

---

## Performance Considerations

### Optimizations Implemented

- Database connection pooling (EF Core default)
- Async/await throughout
- Proper indexing on frequently queried columns
- Pagination for list endpoints
- N+1 query prevention with proper includes

### Monitoring Recommendations

- Query execution time monitoring
- Database connection pool monitoring
- API endpoint response time tracking
- Error rate monitoring
- Database backup/recovery testing

---

## Maintenance and Versioning

### Code Organization

- Clear file structure matching architecture layers
- Consistent naming conventions
- XML documentation for public APIs
- Meaningful commit messages

### Future Maintenance

- Regular dependency updates
- Security patch monitoring
- Performance profiling and optimization
- Code review process
- Documentation updates with changes
