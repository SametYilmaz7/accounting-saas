# ADR-001: Architecture Vision

- Status: Accepted
- Date: 2026-07-12
- Decision Owners: Project Owner and Software Architecture
- Scope: Entire platform

## Context

The product is an AI-supported, web-based, multi-tenant accounting SaaS
initially intended to support approximately 100 independent accounting
offices and later scale to thousands of tenants.

The system is being developed primarily by a single developer with AI-assisted
coding tools. Therefore, the architecture must balance maintainability,
security, development speed, operational simplicity, and future scalability.

Each accounting office is a tenant. Tenant data must remain isolated from
all other tenants.

The platform will initially include core accounting workflows and will later
support OCR, document processing, reporting, subscriptions, billing, audit
logging, and background jobs.

## Decision

The platform will use the following architectural approach:

- Modular Monolith
- Pragmatic Clean Architecture
- Feature-first organization inside modules
- ASP.NET Core and .NET 10 for the backend
- Next.js and TypeScript for the frontend
- PostgreSQL as the primary relational database
- Entity Framework Core as the ORM
- ASP.NET Core Identity for authentication
- S3-compatible object storage for uploaded documents
- Shared database and shared schema multi-tenancy
- TenantId-based tenant isolation

The initial backend layers are:

- Domain
- Application
- Infrastructure
- WebApi

Modules must remain logically isolated while running inside one deployable
backend application.

## Multi-Tenancy Decision

The initial tenancy model will use:

- One shared PostgreSQL database
- One shared schema
- Tenant-owned records containing a mandatory TenantId
- Tenant-aware database queries
- Global tenant query filters where appropriate
- Tenant validation during write operations
- Defense-in-depth protections against cross-tenant access

Tenant isolation must never rely only on values supplied by the client.

The active TenantId must be resolved from trusted authenticated request
context after authentication is implemented.

## Architectural Principles

The following principles apply throughout the project:

1. Prefer simple and explicit implementations.
2. Avoid speculative abstractions.
3. Keep module boundaries clear.
4. Business rules belong in the Domain and Application layers.
5. Infrastructure details must not leak into the Domain layer.
6. All tenant-owned data must be tenant-scoped.
7. Security and correctness take priority over development convenience.
8. Database migrations must be reviewed before execution.
9. Each feature must include appropriate automated tests.
10. The application must remain deployable as a single backend unit.

## Explicitly Rejected Approaches

The following approaches will not be used at the current scale:

- Microservices
- Kubernetes
- Event Sourcing
- Database-per-tenant
- Schema-per-tenant
- Generic repository abstraction over Entity Framework Core
- Excessive shared kernel or building-block projects
- Premature distributed messaging infrastructure

These decisions may only be reconsidered when supported by measured
operational or business requirements.

## Consequences

### Positive

- Lower operational complexity
- Faster development for a single developer
- Easier local debugging and testing
- Strong transactional consistency
- Clear path to modular growth
- Lower hosting and maintenance costs
- Ability to extract modules later if required by measured scale

### Negative

- Shared-schema tenant isolation requires strict engineering discipline
- A tenant-filtering defect could expose data across tenants
- All modules share one deployment lifecycle
- Poor module boundaries could degrade the monolith over time
- Large reporting workloads may eventually require separate read models or
  services

## Security Requirements

- TenantId received directly from request bodies, route parameters, or query
  strings must not be trusted as the source of the active tenant.
- Authorization must verify tenant membership.
- Tenant-owned entities must be filtered by the active tenant.
- Write operations must reject tenant mismatches.
- Background jobs must execute with an explicit tenant context.
- Audit logs must record tenant-sensitive operations.
- Automated tests must cover cross-tenant access attempts.

## Scalability Position

The architecture is designed to support the initial target of approximately
100 tenants and provide a path toward thousands of tenants without rewriting
the application.

Scaling will initially use:

- Vertical application scaling
- Multiple stateless application instances
- PostgreSQL connection pooling
- Database indexing
- Caching where justified
- Background job processing
- Object storage for documents
- Query optimization and read-model strategies for reporting

Microservices will only be considered when a specific module demonstrates
independent scaling, reliability, deployment, or organizational requirements
that cannot be adequately handled within the modular monolith.

## Review Triggers

This decision must be reviewed if one or more of the following occurs:

- Tenant count or workload exceeds measured database capacity
- A module requires independent deployment or scaling
- Regulatory requirements mandate physical tenant separation
- Availability requirements differ significantly between modules
- The development team grows and module ownership changes
- Cross-module coupling becomes difficult to control

## Related Decisions

Future ADRs will define:

- Tenant resolution strategy
- Tenant data isolation enforcement
- Entity base classes and auditing
- Authentication and authorization
- Database migration strategy
- Module communication
- Background job tenant context
- Object storage organization
