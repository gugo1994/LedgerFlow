# BankingApp

**BankingApp** is a portfolio banking backend built with **ASP.NET Core, Entity Framework Core, PostgreSQL, Docker, and xUnit**.

The project focuses on reliable money transfers, concurrency control, transaction consistency, idempotency, retry strategies, database integrity, containerized deployment, and integration testing.

## Features

* User management
* Bank account management
* Money transfers between accounts
* PostgreSQL database transactions
* Pessimistic row locking with `FOR UPDATE`
* Deterministic lock ordering to reduce deadlock risk
* Retry handling for PostgreSQL concurrency errors
* Idempotent transfer requests
* SHA-256 request hashing
* Protection against reusing the same idempotency key with a different payload
* Transfer history
* Debit and credit transaction records linked to the same transfer
* Global exception handling middleware
* Structured logging
* Environment-aware error responses
* EF Core migrations
* Integration tests with a separate PostgreSQL test database
* Dockerized API and PostgreSQL setup
* Environment-based configuration for secrets and connection strings

## Tech Stack

* .NET 10
* C#
* ASP.NET Core Web API
* Entity Framework Core
* PostgreSQL
* Npgsql
* Docker
* Docker Compose
* xUnit
* Microsoft.AspNetCore.Mvc.Testing
* DotNetEnv

## Project Structure

```text
BankingApp/
├── .env
├── .env.example
├── .gitignore
├── .dockerignore
├── Dockerfile
├── docker-compose.yml
├── BankingApp.slnx
│
├── BankingApp.Api/
│   ├── Constants/
│   ├── Controllers/
│   ├── Data/
│   ├── DTOs/
│   ├── Entities/
│   ├── Exceptions/
│   ├── Mappings/
│   ├── Middleware/
│   ├── Migrations/
│   ├── Services/
│   └── Program.cs
│
└── BankingApp.Api.Tests/
    ├── CustomWebApplicationFactory.cs
    └── BankAccountTransferTests.cs
```

## Transfer Flow

A money transfer is executed inside a PostgreSQL database transaction.

```text
HTTP Request
↓
Validate request
↓
Generate request hash
↓
Check idempotency key
↓
Start database transaction
↓
Lock source and destination accounts
↓
Validate balance
↓
Debit source account
↓
Credit destination account
↓
Create Transfer record
↓
Create debit BankTransaction
↓
Create credit BankTransaction
↓
Create IdempotencyRecord
↓
Save changes
↓
COMMIT
```

If any step fails before the transaction is committed, the complete operation is rolled back.

This keeps account balances, transfer history, transaction records, and idempotency data consistent.

## Concurrency Control

BankingApp uses PostgreSQL pessimistic row locking during money transfers.

The source and destination accounts are selected using:

```sql
SELECT *
FROM "BankAccounts"
WHERE "Id" IN (...)
ORDER BY "Id"
FOR UPDATE;
```

`FOR UPDATE` locks the selected rows until the transaction is committed or rolled back.

This prevents multiple concurrent operations from modifying the same account balances at the same time.

## Deadlock Prevention

Opposite transfers may create a deadlock if account rows are locked in different orders.

Example:

```text
Request 1:
A → B

Request 2:
B → A
```

Without deterministic lock ordering:

```text
Transaction 1:
locks A
waits for B

Transaction 2:
locks B
waits for A
```

This creates a circular dependency.

BankingApp reduces this risk by acquiring account locks in deterministic order based on account identifiers.

Conceptually:

```text
smaller account ID
↓
larger account ID
```

The transfer direction does not determine the lock order.

Therefore both:

```text
A → B
```

and:

```text
B → A
```

attempt to acquire the same account rows in the same order.

## Retry Strategy

Some PostgreSQL concurrency errors are transient.

BankingApp retries the entire transfer transaction when PostgreSQL reports concurrency failures such as:

```text
40001 → Serialization Failure
40P01 → Deadlock Detected
```

The complete transaction is retried instead of retrying only the failed SQL statement.

A short delay is used between attempts.

The retry mechanism is designed so that failed EF Core tracked state is cleared before retrying the operation.

## Idempotency

Transfer requests support an:

```http
Idempotency-Key
```

The client should generate a unique key for each logical transfer request.

Example:

```http
Idempotency-Key: 7fd24b0d-1f32-40f4-a550-e364127eaf82
```

If the client retries the same request because of a network timeout or lost response, BankingApp prevents the transfer from being executed multiple times.

## Request Hashing

The same idempotency key must not be reused for a different transfer payload.

BankingApp generates a SHA-256 request hash based on:

```text
FromAccountId
ToAccountId
Amount
```

Example:

```text
Key = X
From = A
To = B
Amount = 100.00
```

If the same key is later reused with:

```text
Key = X
From = A
To = B
Amount = 500.00
```

the request hash differs and the second request is rejected as an idempotency conflict.

Transfer amounts are normalized before hashing so logically equivalent decimal values produce the same canonical representation.

## Database-Level Idempotency Protection

Idempotency is protected by a composite unique index:

```text
Key + Operation
```

Example:

```text
("abc", "Transfer") → allowed
("abc", "Transfer") → duplicate
```

The database constraint protects the application even when two requests using the same idempotency key arrive concurrently.

PostgreSQL unique constraint violations are identified using SQLSTATE:

```text
23505
```

## Transfer Model

A transfer is stored as its own business operation.

Example:

```text
Transfer
--------------------------------
Id: T1
FromAccountId: A
ToAccountId: B
Amount: 100
CreatedAt: ...
```

Each transfer creates two `BankTransaction` records.

```text
BankTransactions
--------------------------------
TransferId: T1
AccountId: A
Amount: -100

TransferId: T1
AccountId: B
Amount: +100
```

Both records reference the same `TransferId`.

This makes it possible to determine which debit and credit entries belong to the same transfer.

## Transaction Atomicity

The following changes are persisted inside the same database transaction:

```text
Source account balance update
+
Destination account balance update
+
Transfer record
+
Debit transaction record
+
Credit transaction record
+
Idempotency record
```

The expected behavior is:

```text
Everything succeeds
→ COMMIT
```

or:

```text
Something fails
→ ROLLBACK everything
```

Partial transfers must never be persisted.

## Database Relationships

The domain currently includes relationships such as:

```text
User
└── BankAccounts
```

```text
Transfer
├── FromAccount
├── ToAccount
└── BankTransactions
```

```text
BankTransaction
├── BankAccount
└── Transfer
```

Foreign keys are enforced at the PostgreSQL level.

Delete behavior is explicitly configured where necessary to avoid accidental cascading deletion of financial records.

## Exception Handling

The API uses centralized exception handling middleware.

Domain-specific exceptions are mapped to appropriate HTTP responses.

Examples:

```text
InsufficientFundsException
→ 409 Conflict

IdempotencyConflictException
→ 409 Conflict

ArgumentException
→ 400 Bad Request

Unexpected exception
→ 500 Internal Server Error
```

This keeps controllers cleaner and avoids repetitive `try/catch` logic.

## Logging

Unexpected exceptions are logged using ASP.NET Core `ILogger`.

The logs include:

* Exception message
* Stack trace
* Exception type
* Application context

The API does not need to expose internal exception details to production clients.

## Environment-Aware Error Responses

In the `Development` environment, detailed exception messages may be returned to simplify debugging.

In `Production`, internal details are hidden.

Example production response:

```json
{
  "error": "An unexpected error occurred."
}
```

The full exception remains available in application logs.

## Environment Configuration

Sensitive configuration is not hardcoded in source code.

Local development configuration can be stored in a `.env` file.

Example:

```env
POSTGRES_DB=banking_app
POSTGRES_USER=postgres
POSTGRES_PASSWORD=CHANGE_ME

ConnectionStrings__Postgres=Host=localhost;Port=5432;Database=banking_app;Username=postgres;Password=CHANGE_ME

ConnectionStrings__PostgresTest=Host=localhost;Port=5432;Database=banking_app_test;Username=postgres;Password=CHANGE_ME
```

The `.env` file must not be committed to Git.

An `.env.example` file can be committed instead:

```env
POSTGRES_DB=banking_app
POSTGRES_USER=postgres
POSTGRES_PASSWORD=CHANGE_ME

ConnectionStrings__Postgres=Host=localhost;Port=5432;Database=banking_app;Username=postgres;Password=CHANGE_ME

ConnectionStrings__PostgresTest=Host=localhost;Port=5432;Database=banking_app_test;Username=postgres;Password=CHANGE_ME
```

ASP.NET Core reads the connection string using:

```csharp
builder.Configuration.GetConnectionString("Postgres");
```

Local `.env` values are loaded before the application builder is created.

## Docker

BankingApp includes Docker support.

The API uses a multi-stage Docker build:

```text
.NET SDK image
↓
restore
↓
build
↓
publish
↓
ASP.NET Core runtime image
```

This keeps the final runtime image smaller than an SDK-based image.

## Docker Compose

Docker Compose starts:

```text
bankingapp-api
+
bankingapp-postgres
```

Inside Docker, the API connects to PostgreSQL using the Docker Compose service name:

```text
Host=postgres
```

instead of:

```text
Host=localhost
```

because `localhost` inside the API container refers to the API container itself.

## Running with Docker

Build and start the environment:

```bash
docker compose up --build
```

Stop the containers:

```bash
docker compose down
```

Remove containers and the PostgreSQL volume:

```bash
docker compose down -v
```

Warning: `-v` deletes the persisted PostgreSQL Docker volume and should not be used when data must be preserved.

The API is exposed locally on:

```text
http://localhost:8080
```

## Database Migrations

BankingApp uses EF Core migrations to manage PostgreSQL schema changes.

Create a migration:

```bash
dotnet ef migrations add MigrationName --project BankingApp.Api
```

Apply migrations:

```bash
dotnet ef database update --project BankingApp.Api
```

The application can also apply pending migrations during startup in the current development/container setup.

For larger production deployments, migrations would normally be executed as a dedicated deployment step rather than independently by every running API replica.

## Integration Testing

BankingApp includes integration tests using:

```text
xUnit
Microsoft.AspNetCore.Mvc.Testing
WebApplicationFactory
```

The tests start the real ASP.NET Core application pipeline and communicate with it using HTTP requests.

A separate PostgreSQL test database is used to isolate test data from development data.

Example:

```text
Development DB
→ banking_app

Integration Test DB
→ banking_app_test
```

## Custom WebApplicationFactory

Integration tests use a custom `WebApplicationFactory` to replace the normal application database configuration with the test database.

This allows the test suite to exercise the real:

```text
Controller
↓
Service
↓
Entity Framework Core
↓
PostgreSQL
```

flow without using the development database.

## Tested Scenarios

Integration tests cover scenarios including:

* Successful transfer between two accounts
* Correct source account balance update
* Correct destination account balance update
* Debit transaction creation
* Credit transaction creation
* Transfer record creation
* Concurrent requests using the same idempotency key
* Prevention of duplicate transfer execution
* Same idempotency key with a different request payload
* `409 Conflict` for idempotency violations
* Correct transaction history
* Correct idempotency record count

## Concurrent Idempotency Test

A concurrent integration test verifies that two simultaneous requests using the same idempotency key do not execute the transfer twice.

Initial state:

```text
Source = 1000
Destination = 0
```

Two requests are sent concurrently:

```text
Amount = 100
Same Idempotency-Key
```

Expected final state:

```text
Source = 900
Destination = 100
```

The following result would indicate duplicate execution:

```text
Source = 800
Destination = 200
```

## Idempotency Conflict Test

The test suite also verifies that the same idempotency key cannot be reused with a different transfer payload.

Example:

```text
Request 1
Key = X
Amount = 100
→ Success
```

```text
Request 2
Key = X
Amount = 500
→ 409 Conflict
```

Only the first transfer is persisted.

## Running Locally

Restore dependencies:

```bash
dotnet restore
```

Build the solution:

```bash
dotnet build
```

Run the API:

```bash
dotnet run --project BankingApp.Api
```

## Running Tests

Run all tests:

```bash
dotnet test
```

The integration tests use the configured PostgreSQL test database.

## Formatting and Code Quality

The project uses `.editorconfig` and .NET analyzers for consistent code style.

Format the project:

```bash
dotnet format
```

Verify formatting without changing files:

```bash
dotnet format --verify-no-changes
```

## Git Ignore

Sensitive and generated files should not be committed.

Typical ignored files include:

```text
.env
.env.*
bin/
obj/
.vs/
.idea/
TestResults/
coverage/
```

The repository can include `.env.example` as documentation for required environment variables.

## Security Considerations

Sensitive information such as:

* Database passwords
* API tokens
* Signing keys
* Connection credentials
* Production secrets

must not be committed to Git.

Secrets should be supplied through environment variables or a dedicated secret-management solution.

Production systems should not expose raw database exception details or stack traces to clients.

## Engineering Concepts Demonstrated

BankingApp demonstrates practical backend engineering concepts including:

* Dependency Injection
* Scoped service lifetimes
* ASP.NET Core middleware
* Entity Framework Core
* PostgreSQL
* EF Core migrations
* ACID transactions
* Transaction boundaries
* Row-level locking
* `SELECT ... FOR UPDATE`
* Pessimistic concurrency control
* Deterministic lock ordering
* Deadlock prevention
* Serialization failures
* Retry strategies
* Change tracking
* Idempotency
* Request hashing
* Unique constraints
* Foreign key constraints
* Transaction history
* Transfer modeling
* Atomic state changes
* Domain exceptions
* Global exception handling
* Structured logging
* Environment-specific behavior
* Environment-based secret management
* Docker
* Docker Compose
* Integration testing
* Test database isolation
* Concurrent HTTP testing

## Purpose

BankingApp is a portfolio and learning project focused on backend engineering patterns commonly required in financial and transactional systems.

The project is intentionally built step by step to demonstrate not only how to implement money transfers, but also how to reason about:

* concurrent requests
* race conditions
* database consistency
* duplicate operations
* transaction boundaries
* database locks
* deadlocks
* retries
* idempotency
* failure recovery
* data integrity
* API reliability
* application observability
* environment configuration
* containerization
* integration testing

The goal is to evolve BankingApp toward a production-style backend architecture while keeping the implementation understandable, testable, and well documented.
