# Architecture Rules

## 1. Architectural Style

The backend uses:

- Modular Monolith
- Pragmatic Clean Architecture
- Feature-first organization within modules
- Shared PostgreSQL database
- Shared schema
- TenantId-based isolation

The backend is deployed as one application.

Do not introduce:

- Microservices
- Kubernetes-specific infrastructure
- Event Sourcing
- Database-per-tenant
- Schema-per-tenant
- Distributed messaging without explicit approval
- Generic repository abstractions over Entity Framework Core
- Shared/Common/Core dumping-ground projects

## 2. Project Responsibilities

### SaaSPlatform.Domain

Contains:

- Entities
- Value objects
- Domain rules
- Domain contracts
- Domain exceptions where justified

Must not reference:

- Entity Framework Core
- ASP.NET Core
- PostgreSQL
- HTTP abstractions
- Configuration
- Infrastructure

### SaaSPlatform.Application

Contains:

- Use cases
- Application contracts
- Application services
- Current tenant abstraction
- Commands, queries, and application orchestration
- DTOs belonging to application use cases

May reference:

- Domain

Must not depend directly on:

- ASP.NET Core HTTP types
- PostgreSQL provider types
- Infrastructure implementations

### SaaSPlatform.Infrastructure

Contains:

- Entity Framework Core
- PostgreSQL integration
- DbContext
- Entity configurations
- Persistence implementations
- External service implementations
- Object storage implementations
- Background job implementations

May reference:

- Application
- Domain

### SaaSPlatform.WebApi

Contains:

- HTTP pipeline
- Endpoint definitions
- ProblemDetails mapping
- Dependency injection composition
- Middleware
- Environment-specific web behavior

May reference:

- Application
- Infrastructure

Business rules must not live in WebApi.

## 3. Dependency Direction

Allowed conceptual dependency flow:

Domain
↑
Application
↑
Infrastructure
↑
WebApi

Actual project references may allow WebApi to reference both Application and
Infrastructure, but business logic must still follow inward dependency rules.

Not allowed:

- Domain referencing Application
- Domain referencing Infrastructure
- Application referencing WebApi
- Application referencing Infrastructure implementations
- Infrastructure business logic replacing Domain or Application logic

## 4. Module Organization

Future business modules should be organized by business capability.

Examples:

Features/
├── Tenants/
├── Authentication/
├── Projects/
├── Tasks/
├── Documents/
├── Reports/
└── Billing/

Avoid broad horizontal folders containing unrelated features, such as:

Services/
Repositories/
Helpers/
Managers/
Utils/

Small technical folders are acceptable inside clearly bounded modules.

## 5. Entity Rules

1. Entities use Guid identifiers unless a documented design says otherwise.
2. Base entities must remain minimal.
3. Tenant ownership must be explicit.
4. Tenant-independent entities must not implement tenant-owned contracts.
5. Domain entities must protect important invariants.
6. EF Core attributes must not be added to Domain entities.
7. Persistence mapping belongs in Infrastructure.
8. Avoid public setters when domain invariants require controlled mutation.
9. Do not introduce domain events until a real use case requires them.
10. Do not introduce soft delete without an approved ADR.

## 6. Multi-Tenant Rules

1. Tenant is a tenant-independent platform entity.
2. Tenant-owned entities must contain a non-null TenantId.
3. TenantId must not be authoritative when received from clients.
4. Tenant context must be scoped.
5. Tenant context must not be static or singleton.
6. Tenant-owned queries must fail closed.
7. Writes must validate tenant ownership.
8. Cross-tenant updates and deletes must be rejected.
9. TenantId mutation must be rejected.
10. Background jobs must later use explicit tenant context.
11. Global query filters alone are not sufficient.
12. Tenant isolation must be covered by integration tests.

## 7. Persistence Rules

1. Use Entity Framework Core directly.
2. Do not create a generic repository abstraction.
3. Do not hide IQueryable behind unnecessary wrappers.
4. Keep EF Core configurations explicit.
5. Use migrations for schema changes.
6. Migrations must not execute automatically at application startup.
7. Review generated migrations before applying them.
8. Avoid provider-specific features unless justified.
9. Use PostgreSQL-compatible integration tests.
10. Do not use SQLite as authoritative proof of PostgreSQL behavior.

## 8. API Rules

1. Business endpoints should eventually use versioned routes.
2. Operational endpoints such as /health may remain unversioned.
3. Tenant-independent endpoints must be explicitly marked.
4. Tenant-related errors use ProblemDetails.
5. Client responses must not expose internal exceptions.
6. HTTP concerns must not leak into Domain.
7. Request models must not determine the active TenantId.
8. Controllers or minimal APIs may be used consistently, but do not mix
   styles without a reason.
9. OpenAPI must only be added using an approved secure package version.
10. API contracts must be documented before implementation.

## 9. Simplicity Rules

Prefer:

- Explicit code
- Small interfaces
- Focused classes
- Clear names
- Standard framework features
- Measured optimization

Avoid:

- Reflection-heavy frameworks without need
- Custom mediator frameworks without approval
- Generic base service hierarchies
- Premature caching
- Premature messaging
- Premature distributed transactions
- Abstractions with only one trivial implementation unless testing or
  architectural boundaries justify them

