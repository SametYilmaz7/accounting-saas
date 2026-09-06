# Sprint 01 Requirements: Multi-Tenant Foundation

- Status: Approved
- Sprint: 01
- Scope: Backend foundation
- Related ADR: ADR-001 Architecture Vision

## 1. Sprint Goal

Establish the minimum secure multi-tenant foundation required for all
future tenant-owned business modules.

The sprint must provide a reliable basis for tenant identification,
tenant-aware persistence, tenant isolation, and database connectivity.

This sprint does not include authentication screens, future product workflows,
billing, subscriptions, OCR, reporting, or document storage.

## 2. Business Context

A Tenant represents an organization or workspace using the SaaS platform.

A tenant may later contain multiple users, roles, projects, tasks,
documents, reports, and subscription records.

Data belonging to one tenant must never be readable or writable by another
tenant.

The initial implementation targets approximately 100 tenants but must support
future growth to thousands of tenants without redesigning the tenancy model.

## 3. Functional Requirements

### FR-001 Tenant Representation

The system must provide a Tenant domain entity.

A tenant must include at least:

- Id
- Name
- Slug
- IsActive
- CreatedAtUtc
- UpdatedAtUtc

Tenant identifiers must use Guid values.

Tenant names must be required.

Tenant slugs must be required and unique.

Tenant slugs must be normalized before persistence.

### FR-002 Tenant-Owned Entities

The system must define a consistent mechanism for identifying tenant-owned
entities.

Every tenant-owned entity must contain a non-null TenantId.

TenantId must use the same data type as Tenant.Id.

The model must make tenant ownership explicit and easy to review.

### FR-003 Current Tenant Context

The application must expose a current tenant abstraction that can provide:

- Whether a tenant has been resolved
- The active TenantId

The abstraction must not depend directly on ASP.NET Core HTTP types.

The initial implementation may use a development-only tenant resolution
mechanism until authentication is implemented.

### FR-004 Tenant Resolution

The Web API must resolve the current tenant before tenant-owned operations
are executed.

The development-time resolver may use a request header.

The initial development header name must be:

X-Tenant-Id

The header value must be parsed as a Guid.

Missing or invalid tenant identifiers must produce a controlled client error.

The development header mechanism must be clearly marked as temporary and must
not be treated as production authentication or authorization.

### FR-005 Tenant Query Isolation

Queries for tenant-owned entities must automatically be restricted to the
active tenant where technically appropriate.

The implementation must reduce the risk of developers forgetting tenant
filters in individual queries.

Tenant isolation must not rely solely on manually written Where clauses.

Tenant-independent entities must not be filtered by TenantId.

### FR-006 Tenant Write Protection

New tenant-owned records must be assigned to the active tenant.

The active TenantId must not be accepted from untrusted client payloads as the
authoritative tenant context.

Write operations must reject tenant mismatches.

Existing records belonging to another tenant must not be updated or deleted.

### FR-007 Tenant Activation

Inactive tenants must not be allowed to perform normal tenant-owned
operations.

The initial foundation must support checking whether a tenant is active.

A full tenant lifecycle management API is outside this sprint.

### FR-008 Database Connectivity

The backend must connect to PostgreSQL through Entity Framework Core.

The connection string must be read from configuration.

Secrets must not be committed to source control.

A development configuration example may be provided without real credentials.

### FR-009 Initial Migration

The sprint must create an initial EF Core migration containing the tenant
foundation schema.

The migration must be reviewable before execution.

The migration must not be applied automatically during normal application
startup.

### FR-010 Health Checks

The Web API must expose a health endpoint.

The health check must include PostgreSQL connectivity.

The endpoint must not reveal secrets or detailed internal exception data.

### FR-011 Automated Tests

The implementation must include automated tests for tenant isolation.

At minimum, tests must cover:

- Tenant-owned data is visible to its owning tenant
- Tenant-owned data is not visible to another tenant
- Missing tenant context is rejected for tenant-owned operations
- Invalid tenant identifiers are rejected
- Tenant mismatch during write operations is rejected
- Tenant-independent entities are not tenant-filtered

## 4. Non-Functional Requirements

### NFR-001 Security

Tenant isolation is a security boundary.

Cross-tenant access must fail safely.

Tenant context must come from trusted server-side resolution logic.

Production authorization must not rely on the development tenant header.

### NFR-002 Performance

Tenant-owned tables must support efficient filtering by TenantId.

Database indexes must be considered for TenantId and tenant-scoped unique
constraints.

The design must avoid loading complete tenant datasets into memory.

### NFR-003 Maintainability

Tenant rules must be centralized where practical.

The implementation must remain explicit and understandable.

The design must not introduce a generic repository abstraction over
Entity Framework Core.

The implementation must avoid unnecessary reflection-heavy frameworks.

### NFR-004 Testability

Tenant context must be replaceable in automated tests.

Domain and application logic must not require a live HTTP request.

Database isolation behavior must be verifiable using integration tests.

### NFR-005 Observability

Tenant-aware operations must be capable of including TenantId in structured
logs.

Secrets and sensitive tenant data must not be logged.

Audit logging implementation is outside this sprint but the design must not
block it.

### NFR-006 Scalability

The tenant model must support shared-database, shared-schema operation for
thousands of tenants.

The implementation must not require a separate DbContext type or database
connection per tenant.

### NFR-007 Reliability

The application must fail closed when tenant context is required but missing.

Database migrations must be deterministic and reproducible.

Application startup must not silently modify the production database.

## 5. Data Requirements

The initial tenant table must support:

- Guid primary key
- Required tenant name
- Required normalized slug
- Active/inactive status
- UTC creation timestamp
- UTC update timestamp

Slug uniqueness must be enforced at the database level.

Future tenant-owned tables must include TenantId.

Tenant-scoped uniqueness must use composite constraints when appropriate.

Example:

- TenantId + TaskNumber
- TenantId + ProjectCode

## 6. API Requirements

This sprint does not require a tenant administration API.

A minimal tenant-aware diagnostic endpoint may be implemented only if needed
to verify tenant resolution.

The health endpoint must be tenant-independent.

Development tenant resolution must use:

X-Tenant-Id: {tenant-guid}

Any diagnostic endpoint added for this sprint must not expose tenant data.

## 7. Out of Scope

The following are explicitly outside this sprint:

- User registration
- Login
- ASP.NET Core Identity integration
- Tenant membership
- Role and permission management
- Project management
- Task management
- OCR
- AI document extraction
- Billing
- Subscription plans
- Audit log persistence
- Background jobs
- Object storage
- Production tenant resolution from authenticated claims
- Tenant administration UI
- Frontend implementation

## 8. Acceptance Criteria

The sprint is accepted when all of the following are true:

1. The Tenant entity and tenant ownership contracts exist.
2. PostgreSQL and EF Core are configured.
3. A current tenant abstraction exists.
4. Development tenant resolution works through X-Tenant-Id.
5. Missing and invalid tenant context produce controlled errors.
6. Tenant query isolation is enforced automatically.
7. Tenant write mismatches are rejected.
8. Inactive tenants cannot perform tenant-owned operations.
9. An initial migration exists and is reviewed.
10. The database migration can be applied manually.
11. PostgreSQL health checks pass.
12. Tenant isolation tests pass.
13. The full solution builds with zero errors.
14. No secrets are committed.
15. No unrelated business modules are introduced.

## 9. Risks

### Risk: Cross-Tenant Data Exposure

Mitigation:

- Central tenant context
- Global query filters
- Write validation
- Integration tests
- Code review checklist

### Risk: Development Header Used in Production

Mitigation:

- Clearly isolate the development resolver
- Document it as temporary
- Replace it when authentication is implemented
- Disable or restrict it outside development

### Risk: Query Filters Are Bypassed

Mitigation:

- Limit IgnoreQueryFilters usage
- Review raw SQL carefully
- Add integration tests
- Treat bypasses as security-sensitive code

### Risk: Incorrect Background Tenant Context

Mitigation:

- Background jobs are outside this sprint
- Future jobs must carry explicit TenantId
- No ambient HTTP dependency in tenant abstractions

## 10. Definition of Done

- Requirements reviewed
- Technical design approved
- Database design approved
- API design approved where applicable
- Code implemented through small Codex tasks
- Automated tests passing
- Solution builds with zero errors
- Vulnerable package scan clean
- Migration reviewed
- Documentation updated
- Changes committed to the sprint branch
