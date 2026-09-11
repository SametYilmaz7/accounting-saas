# Sprint 01 Technical Design: Multi-Tenant Foundation

- Status: Proposed
- Sprint: 01
- Scope: Backend foundation
- Related ADR: ADR-001 Architecture Vision
- Related Requirements: Sprint 01 Multi-Tenant Foundation Requirements

## 1. Purpose

### S1-17 implementation baseline

The implemented bootstrap resolver is Development-only. It accepts exactly one
non-empty GUID, leaves missing headers unresolved, and rejects invalid or
multiple values with minimal ProblemDetails. It does not query Tenant or check
activation/membership. Endpoint opt-out metadata and global exception mapping
described below are deferred; no header is required for /health, though an
invalid supplied header is still rejected by the unchanged middleware.

SaveChanges rejects empty or mismatched TenantId values for Added, Modified,
and Deleted entities and rejects ownership mutation. It never assigns TenantId.
The Tenant entity stores one canonical Slug, with no separate NormalizedSlug.
These delivered decisions supersede the earlier proposed alternatives below.

Production configuration uses ConnectionStrings:DefaultConnection and fails
fast when blank. Integration tests exclusively use the externally supplied
ConnectionStrings__IntegrationTestDatabase and validate saas_platform_test
before migrations/setup. They serialize tests and roll back transactional
test-only table creation and data. No production test entities are introduced.

This document defines the technical design for the initial multi-tenant
foundation.

The design must support:

- Shared database
- Shared schema
- TenantId-based isolation
- PostgreSQL
- Entity Framework Core
- Development-time tenant resolution
- Automatic tenant query filtering
- Tenant-aware write protection
- Future replacement of development tenant resolution with authenticated
  tenant claims

The design intentionally avoids:

- Database-per-tenant
- Schema-per-tenant
- Generic repository abstractions
- HTTP dependencies in Domain or Application
- Automatic migration execution during application startup
- Production authorization based on a request header

## 2. Layer Responsibilities

### Domain

The Domain project contains:

- Tenant entity
- Tenant ownership contracts
- Base entity abstractions
- Domain validation rules
- Domain exceptions where appropriate

The Domain project must not reference:

- Entity Framework Core
- ASP.NET Core
- PostgreSQL
- HTTP context
- Configuration providers
- Infrastructure implementations

### Application

The Application project contains:

- Current tenant abstraction
- Tenant context contracts
- Application-level tenant validation abstractions
- Use case orchestration added in future features

The Application project may reference Domain.

The Application project must not directly depend on:

- ASP.NET Core HTTP types
- Entity Framework Core provider details
- PostgreSQL-specific types

### Infrastructure

The Infrastructure project contains:

- EF Core DbContext
- Entity configurations
- PostgreSQL integration
- Tenant-aware query filters
- Tenant-aware SaveChanges enforcement
- Persistence implementations
- Database health check registration where appropriate

Infrastructure may reference Application and Domain.

### WebApi

The WebApi project contains:

- HTTP request pipeline
- Development tenant resolution middleware
- ProblemDetails responses
- Dependency injection composition
- Health endpoint mapping
- Environment-specific tenant resolver registration

WebApi may reference Application and Infrastructure.

## 3. Domain Model

### 3.1 Base Entity

A minimal base entity will be introduced.

Proposed contract:

- Id: Guid

The base entity must remain small.

It must not automatically include:

- TenantId
- Soft delete
- Audit fields
- Domain events

Those concerns must remain explicit through separate contracts or base types.

### 3.2 Auditable Entity

An auditable base type may be introduced for entities requiring timestamps.

Proposed fields:

- CreatedAtUtc: DateTime
- UpdatedAtUtc: DateTime

All timestamps must be stored in UTC.

User-based audit fields such as CreatedByUserId and UpdatedByUserId are outside
this sprint because authentication has not yet been implemented.

### 3.3 Tenant Entity

Tenant is a tenant-independent root entity.

Proposed fields:

- Id: Guid
- Name: string
- Slug: string
- NormalizedSlug: string
- IsActive: bool
- CreatedAtUtc: DateTime
- UpdatedAtUtc: DateTime

Tenant itself must not contain TenantId.

Tenant is the root record used to identify an organization or workspace using the SaaS platform.

### 3.4 Tenant-Owned Contract

Tenant-owned entities must implement an explicit contract.

Proposed contract:

ITenantOwned

Property:

- TenantId: Guid

This contract enables:

- Static code review
- Query filter discovery
- SaveChanges validation
- Automated tests
- Clear ownership semantics

Tenant ownership must not be inferred only from naming conventions.

## 4. Current Tenant Abstraction

The application must expose an abstraction independent of HTTP.

Proposed interface:

ICurrentTenant

Responsibilities:

- Indicate whether a tenant has been resolved
- Return the active TenantId when available

Proposed members:

- bool IsResolved
- Guid? TenantId
- Guid GetRequiredTenantId()

GetRequiredTenantId must fail with a controlled application exception if no
tenant has been resolved.

The interface belongs in Application.

The implementation may belong in WebApi or Infrastructure depending on the
final dependency composition.

The abstraction must be replaceable in tests.

## 5. Tenant Context Lifetime

Tenant context must use scoped lifetime.

One tenant context instance must exist per HTTP request.

The context must not be:

- Singleton
- Static
- Stored in global mutable state

Future background jobs must create an explicit tenant scope rather than depend
on HTTP context.

## 6. Development Tenant Resolution

### 6.1 Header

Development requests may supply:

X-Tenant-Id: {guid}

### 6.2 Resolution Flow

The development middleware must:

1. Check whether the current endpoint requires tenant context.
2. Read the X-Tenant-Id header.
3. Reject missing headers when tenant context is required.
4. Reject malformed Guid values.
5. Load the Tenant record without tenant filtering.
6. Reject unknown tenants.
7. Reject inactive tenants.
8. Store the resolved TenantId in the scoped tenant context.
9. Continue the request pipeline.

### 6.3 Environment Restriction

Header-based tenant resolution must only be enabled in the Development
environment.

It must not be registered as the production tenant resolver.

Production tenant resolution will later use authenticated claims and tenant
membership validation.

### 6.4 Tenant-Independent Endpoints

The following endpoints must remain tenant-independent:

- Health checks
- Future authentication endpoints
- Future tenant onboarding endpoints
- Operational diagnostics explicitly approved as tenant-independent

Tenant-independent endpoints must not be forced to provide X-Tenant-Id.

A simple endpoint metadata marker or explicit path policy may be used.

The preferred design is endpoint metadata rather than hard-coded URL checks.

## 7. EF Core DbContext Design

The Infrastructure project will introduce:

SaaSPlatformDbContext

Responsibilities:

- Expose DbSet<Tenant>
- Configure PostgreSQL mappings
- Apply tenant query filters
- Enforce tenant ownership during writes
- Set UTC audit timestamps
- Support migrations

The DbContext must receive ICurrentTenant through dependency injection.

The DbContext must not resolve services through a service locator.

## 8. Query Isolation Strategy

### 8.1 Global Query Filters

Global query filters must be applied to entities implementing ITenantOwned.

Conceptual behavior:

entity.TenantId == currentTenant.TenantId

The filter must fail closed when tenant context is required but unavailable.

The implementation must not accidentally expose all tenant-owned rows when
TenantId is null.

### 8.2 Tenant Entity

Tenant must not receive a tenant query filter.

It is a tenant-independent platform entity.

### 8.3 Filter Discovery

The DbContext model configuration may scan mapped entity types for the
ITenantOwned contract and apply a tenant filter.

Reflection may be used only in model construction if the implementation
remains small, explicit, and tested.

Do not introduce an external framework for query filter registration.

### 8.4 Bypassing Filters

IgnoreQueryFilters must be treated as security-sensitive.

Permitted use cases must be explicit, such as:

- Tenant resolution
- Administrative maintenance
- Carefully reviewed migrations or operational tooling

Business feature code must not use IgnoreQueryFilters casually.

## 9. Write Isolation Strategy

Write protection must be enforced centrally in SaveChanges and
SaveChangesAsync.

### 9.1 Added Tenant-Owned Entities

For added entities implementing ITenantOwned:

- Active TenantId must be resolved
- TenantId must be assigned from the active server-side tenant context
- Client-supplied TenantId must not be authoritative

Preferred behavior:

- If TenantId is Guid.Empty, set it to active TenantId
- If TenantId is non-empty and differs from active TenantId, reject the write
- If TenantId matches active TenantId, allow the write

### 9.2 Modified Tenant-Owned Entities

For modified entities:

- Active TenantId must be resolved
- Entity TenantId must match active TenantId
- TenantId must not be mutable

The original TenantId and current TenantId must be compared.

Any attempted TenantId modification must be rejected.

### 9.3 Deleted Tenant-Owned Entities

For deleted entities:

- Active TenantId must be resolved
- Entity TenantId must match active TenantId
- Cross-tenant deletion must be rejected

Soft delete is not included in this sprint.

Physical deletion may remain available until a future ADR defines soft-delete
policy.

## 10. Audit Timestamp Strategy

CreatedAtUtc and UpdatedAtUtc must be controlled server-side.

For added auditable entities:

- CreatedAtUtc = current UTC timestamp
- UpdatedAtUtc = current UTC timestamp

For modified auditable entities:

- CreatedAtUtc must remain unchanged
- UpdatedAtUtc = current UTC timestamp

Use TimeProvider rather than directly calling DateTime.UtcNow where practical.

TimeProvider must be injectable for deterministic tests.

## 11. Tenant Slug Normalization

Tenant slug behavior must be deterministic.

Proposed normalization:

- Trim surrounding whitespace
- Convert to lowercase invariant
- Replace whitespace with hyphens
- Remove unsupported characters
- Collapse repeated hyphens
- Remove leading and trailing hyphens

Examples:

"Example Workspace" -> "example-workspace"

"  ABC   Workspace  " -> "abc-workspace"

The persisted NormalizedSlug must be unique.

Slug normalization logic should live outside Infrastructure.

It may be implemented as:

- Domain value object
- Domain service
- Small domain utility

The implementation must be deterministic and testable.

## 12. Error Handling

Tenant-related failures must produce controlled errors.

Proposed failure categories:

- Tenant context missing
- Tenant identifier invalid
- Tenant not found
- Tenant inactive
- Tenant mismatch
- Tenant ownership violation

WebApi must map these errors to ProblemDetails responses.

Suggested HTTP status codes:

- Missing tenant header: 400 Bad Request
- Invalid tenant header: 400 Bad Request
- Tenant not found: 404 Not Found
- Tenant inactive: 403 Forbidden
- Tenant mismatch: 403 Forbidden

Responses must not expose stack traces or internal database details.

## 13. Dependency Injection

Proposed registrations:

- ICurrentTenant: Scoped
- Mutable request tenant context implementation: Scoped
- SaaSPlatformDbContext: Scoped
- TimeProvider: Singleton using TimeProvider.System
- Development tenant middleware: Middleware registration in WebApi

The final implementation must avoid duplicate tenant context instances within
the same request scope.

## 14. Configuration

The PostgreSQL connection string key will be:

ConnectionStrings:DefaultConnection

Real development credentials must not be committed.

The repository may contain:

- appsettings.json without secrets
- .env.example or documented user-secrets instructions
- local user-secrets outside source control

Preferred local development secret storage:

dotnet user-secrets

The WebApi project should be configured with a UserSecretsId when the
database integration task is implemented.

## 15. Migration Strategy

EF Core migrations will be stored in Infrastructure.

Proposed migration assembly:

SaaSPlatform.Infrastructure

The startup project for migration commands will be:

SaaSPlatform.WebApi

Migrations must not run automatically during application startup.

Development migration commands will be executed manually.

Example future command:

dotnet ef migrations add InitialCreate \
  --project src/backend/SaaSPlatform.Infrastructure \
  --startup-project src/backend/SaaSPlatform.WebApi \
  --output-dir Persistence/Migrations

Migration execution must be reviewed before:

dotnet ef database update

## 16. Health Check Design

The Web API will expose:

GET /health

The health endpoint must:

- Be tenant-independent
- Check application liveness
- Check PostgreSQL connectivity
- Return minimal information
- Avoid leaking connection strings or exception details

A future split may introduce:

- /health/live
- /health/ready

This sprint may begin with a single /health endpoint.

## 17. Testing Strategy

### 17.1 Unit Tests

Unit tests must cover:

- Tenant slug normalization
- Missing tenant behavior
- Tenant mismatch behavior
- Audit timestamp behavior where isolated testing is practical

### 17.2 Integration Tests

Integration tests must cover:

- Tenant A can query Tenant A data
- Tenant A cannot query Tenant B data
- Tenant-owned queries fail closed without tenant context
- TenantId is assigned server-side for new records
- Cross-tenant writes are rejected
- TenantId mutation is rejected
- Tenant entity is not tenant-filtered
- Inactive tenant requests are rejected
- Invalid X-Tenant-Id values are rejected

### 17.3 Test Database

Tenant isolation tests must use PostgreSQL-compatible behavior.

SQLite must not be treated as the authoritative database for PostgreSQL
integration behavior.

The initial implementation may use a local PostgreSQL test database.

Testcontainers may be evaluated later when Docker is introduced.

## 18. Security Considerations

Tenant isolation uses defense in depth:

1. Trusted tenant resolution
2. Scoped current tenant context
3. Global query filters
4. SaveChanges validation
5. Database constraints
6. Automated cross-tenant tests
7. Restricted filter bypass
8. Structured logging with TenantId

Global query filters alone are not sufficient.

The development header is not authentication.

The active tenant must never be selected solely from a client request once
production authentication is introduced.

## 19. Performance Considerations

All tenant-owned tables should normally include an index beginning with
TenantId when query patterns justify it.

Examples:

- TenantId
- TenantId + CreatedAtUtc
- TenantId + TaskNumber
- TenantId + ProjectCode

Indexes must be designed per table and query pattern.

A universal index policy must not be applied blindly.

Global query filters must translate to SQL and execute in PostgreSQL.

Tenant datasets must not be loaded into memory for filtering.

## 20. Implementation Sequence

The implementation must be divided into small Codex tasks.

Proposed sequence:

1. Add approved EF Core and PostgreSQL packages
2. Add base entity and tenant ownership contracts
3. Add Tenant entity and slug normalization
4. Add current tenant abstraction
5. Add scoped tenant context implementation
6. Add DbContext and entity configuration
7. Add query filter enforcement
8. Add SaveChanges tenant enforcement
9. Add audit timestamp handling
10. Add development tenant resolution middleware
11. Add ProblemDetails mapping
12. Add health checks
13. Add user-secrets configuration
14. Add initial migration
15. Add unit tests
16. Add PostgreSQL integration tests

Each task must build successfully before moving to the next task.

## 21. Open Questions

The following decisions remain open until later design steps:

- Exact Tenant table and column names
- Maximum lengths for Name and Slug
- Whether Tenant slug must be globally unique or environment-specific
- Exact exception hierarchy
- Endpoint metadata type for tenant-independent routes
- Whether the first migration includes a seeded development tenant
- Local integration test database naming
- Whether soft delete is introduced in a later sprint

These questions must be resolved in the Database Design and API Design
documents before implementation.

## 22. Acceptance of This Design

This design is approved for implementation when:

- Layer responsibilities are accepted
- Tenant context lifetime is accepted
- Query filter strategy is accepted
- Write enforcement strategy is accepted
- Development header restrictions are accepted
- Migration strategy is accepted
- Testing strategy is accepted
- Remaining open questions are resolved in subsequent design documents
