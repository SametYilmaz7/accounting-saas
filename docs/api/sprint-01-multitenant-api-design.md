# Sprint 01 API Design: Multi-Tenant Foundation

- Status: Proposed
- Sprint: 01
- Scope: Web API tenant foundation
- Related ADR: ADR-001 Architecture Vision
- Related Requirements: Sprint 01 Multi-Tenant Foundation Requirements
- Related Technical Design: Sprint 01 Multi-Tenant Technical Design
- Related Database Design: Sprint 01 Multi-Tenant Database Design

## 1. Purpose

This document defines the HTTP API behavior required by the initial
multi-tenant foundation.

This sprint does not introduce business APIs.

The API scope is limited to:

- Application health
- Development-time tenant resolution
- Tenant-independent endpoint metadata
- Standardized tenant-related error responses
- An optional development-only tenant context diagnostic endpoint

The API must not treat the development tenant header as authentication or
authorization.

## 2. API Conventions

### 2.1 Base Path

The initial application does not require a global /api prefix for operational
endpoints.

Operational endpoint:

GET /health

Future business endpoints should normally use versioned API routes such as:

/api/v1/customers
/api/v1/invoices

API versioning infrastructure is outside Sprint 01.

### 2.2 Content Type

JSON responses must use:

application/json

Problem responses must use:

application/problem+json

### 2.3 Property Naming

JSON property names must use camelCase.

Examples:

- tenantId
- traceId
- errorCode
- isResolved

### 2.4 Identifiers

Tenant identifiers are represented as canonical Guid strings.

Example:

3f54449e-9b42-4e32-92ab-7f52b6e8724a

Clients must not depend on uppercase or lowercase Guid formatting.

### 2.5 Time Values

Future API timestamps must use ISO 8601 UTC format.

Example:

2026-07-13T18:30:00Z

## 3. Health Endpoint

### 3.1 Route

GET /health

### 3.2 Tenant Requirement

The health endpoint is tenant-independent.

It must not require:

X-Tenant-Id

Tenant resolution middleware must not reject this endpoint.

### 3.3 Purpose

The endpoint verifies:

- The application process is running
- Required services are registered
- PostgreSQL is reachable

### 3.4 Success Response

HTTP status:

200 OK

The exact health response body may follow the ASP.NET Core health check
format.

The response must remain minimal.

Example conceptual response:

{
  "status": "Healthy"
}

The endpoint must not expose:

- Connection strings
- Database credentials
- Stack traces
- Internal exception messages
- Database server paths
- Environment variables
- Detailed infrastructure topology

### 3.5 Failure Response

HTTP status:

503 Service Unavailable

Example conceptual response:

{
  "status": "Unhealthy"
}

Detailed database exceptions must not be returned to clients.

### 3.6 Future Extension

The application may later split health endpoints into:

GET /health/live
GET /health/ready

This split is not required in Sprint 01.

## 4. Development Tenant Header

### 4.1 Header Name

Development tenant resolution uses:

X-Tenant-Id

### 4.2 Header Format

The value must be a valid Guid.

Example:

X-Tenant-Id: 3f54449e-9b42-4e32-92ab-7f52b6e8724a

### 4.3 Environment Restriction

Header-based tenant resolution must only be enabled in the Development
environment.

It must not be registered as the production tenant resolution mechanism.

Production tenant resolution will later use:

- Authenticated user claims
- Tenant membership validation
- Authorization policies

### 4.4 Security Position

Possession of a valid tenant Guid does not prove:

- User identity
- Tenant membership
- Permission
- Authorization

The header exists only to support local development before authentication is
implemented.

## 5. Tenant Requirement Policy

### 5.1 Default Position

Future tenant-owned business endpoints must require resolved tenant context.

Tenant-independent endpoints must be explicitly marked.

The preferred design is fail closed:

- Tenant-owned endpoints require tenant context by default
- Tenant-independent endpoints opt out explicitly

### 5.2 Tenant-Independent Metadata

A small endpoint metadata marker should identify tenant-independent endpoints.

Proposed semantic name:

AllowAnonymousTenant

or:

TenantIndependent

The final implementation should prefer a clear marker type over hard-coded
path comparisons.

The metadata marker may be applied through:

- Endpoint metadata
- An endpoint convention
- A custom attribute where appropriate

The exact type name will be finalized during implementation.

### 5.3 Initially Tenant-Independent Endpoints

Sprint 01:

- GET /health

Future examples:

- Login
- Tenant onboarding
- Password reset
- Public operational endpoints explicitly approved as tenant-independent

## 6. Tenant Resolution Flow

For endpoints requiring tenant context, the request pipeline must execute the
following behavior:

1. Read endpoint metadata.
2. Skip tenant resolution when the endpoint is explicitly tenant-independent.
3. Read X-Tenant-Id in Development.
4. Reject a missing header.
5. Reject multiple conflicting header values.
6. Reject malformed Guid values.
7. Query the tenant table using a tenant-independent lookup.
8. Reject an unknown tenant.
9. Reject an inactive tenant.
10. Set the resolved TenantId in the scoped current tenant context.
11. Continue the request pipeline.

Tenant resolution must occur before tenant-aware persistence operations.

## 7. Error Response Standard

Tenant-related HTTP failures must use RFC 7807-compatible ProblemDetails.

The response must use:

application/problem+json

The standard fields are:

- type
- title
- status
- detail
- instance

The application may add these extensions:

- errorCode
- traceId

The application must not expose:

- Stack traces
- SQL statements
- Connection information
- Internal type names
- Sensitive tenant data

## 8. Error Code Convention

Error codes must be stable machine-readable identifiers.

Use uppercase snake case.

Sprint 01 tenant error codes:

- TENANT_HEADER_MISSING
- TENANT_HEADER_INVALID
- TENANT_HEADER_MULTIPLE_VALUES
- TENANT_NOT_FOUND
- TENANT_INACTIVE
- TENANT_CONTEXT_MISSING
- TENANT_MISMATCH
- TENANT_OWNERSHIP_VIOLATION

Clients may use errorCode for deterministic error handling.

Human-readable text may change without being considered a breaking API
change.

## 9. Missing Tenant Header

### 9.1 Condition

A tenant-required endpoint is called without X-Tenant-Id in Development.

### 9.2 Response

HTTP status:

400 Bad Request

Content type:

application/problem+json

Example:

{
  "type": "https://errors.accounting-saas.local/tenant-header-missing",
  "title": "Tenant identifier is required.",
  "status": 400,
  "detail": "The X-Tenant-Id request header is required for this endpoint.",
  "instance": "/api/v1/example",
  "errorCode": "TENANT_HEADER_MISSING",
  "traceId": "00-example-trace-id"
}

The domain used in the type URI is conceptual and must not be treated as a
production hostname decision.

## 10. Invalid Tenant Header

### 10.1 Condition

X-Tenant-Id is present but cannot be parsed as a Guid.

### 10.2 Response

HTTP status:

400 Bad Request

Error code:

TENANT_HEADER_INVALID

Example:

{
  "type": "https://errors.accounting-saas.local/tenant-header-invalid",
  "title": "Tenant identifier is invalid.",
  "status": 400,
  "detail": "The X-Tenant-Id request header must contain a valid identifier.",
  "instance": "/api/v1/example",
  "errorCode": "TENANT_HEADER_INVALID",
  "traceId": "00-example-trace-id"
}

The response should not echo the invalid header value.

## 11. Multiple Tenant Header Values

### 11.1 Condition

The request contains multiple X-Tenant-Id values.

### 11.2 Response

HTTP status:

400 Bad Request

Error code:

TENANT_HEADER_MULTIPLE_VALUES

The server must not silently choose the first value.

## 12. Tenant Not Found

### 12.1 Condition

The header contains a valid Guid, but no tenant exists with that identifier.

### 12.2 Response

HTTP status:

404 Not Found

Error code:

TENANT_NOT_FOUND

Example:

{
  "type": "https://errors.accounting-saas.local/tenant-not-found",
  "title": "Tenant was not found.",
  "status": 404,
  "detail": "The requested tenant could not be resolved.",
  "instance": "/api/v1/example",
  "errorCode": "TENANT_NOT_FOUND",
  "traceId": "00-example-trace-id"
}

The response must not reveal additional tenant records.

### 12.3 Future Security Review

After authentication is introduced, returning 404 versus 403 must be reviewed
to avoid tenant enumeration.

## 13. Inactive Tenant

### 13.1 Condition

The tenant exists but is inactive.

### 13.2 Response

HTTP status:

403 Forbidden

Error code:

TENANT_INACTIVE

Example:

{
  "type": "https://errors.accounting-saas.local/tenant-inactive",
  "title": "Tenant is inactive.",
  "status": 403,
  "detail": "The tenant is not permitted to perform this operation.",
  "instance": "/api/v1/example",
  "errorCode": "TENANT_INACTIVE",
  "traceId": "00-example-trace-id"
}

The response should not disclose internal suspension or billing details.

## 14. Missing Server-Side Tenant Context

### 14.1 Condition

Application or persistence code requires an active tenant, but the scoped
tenant context is unresolved.

This represents either:

- A controlled request validation failure
- Incorrect middleware ordering
- Incorrect endpoint metadata
- A programming defect

### 14.2 Response

For a client request that legitimately omitted required development context:

400 Bad Request

Error code:

TENANT_CONTEXT_MISSING

For an unexpected internal pipeline defect:

500 Internal Server Error

The implementation must distinguish expected input failures from internal
configuration defects where practical.

Internal details must be logged but not returned.

## 15. Tenant Mismatch

### 15.1 Condition

A write operation attempts to persist an entity whose TenantId differs from
the active server-side tenant context.

### 15.2 Response

HTTP status:

403 Forbidden

Error code:

TENANT_MISMATCH

Example:

{
  "type": "https://errors.accounting-saas.local/tenant-mismatch",
  "title": "Tenant ownership mismatch.",
  "status": 403,
  "detail": "The requested operation is not permitted for the active tenant.",
  "instance": "/api/v1/example",
  "errorCode": "TENANT_MISMATCH",
  "traceId": "00-example-trace-id"
}

The response must not disclose the owning tenant identifier.

## 16. Tenant Ownership Violation

### 16.1 Condition

The system detects an attempt to read, update, or delete tenant-owned data
outside the active tenant boundary.

### 16.2 Response

Preferred public behavior:

404 Not Found

or:

403 Forbidden

The exact status may depend on the use case.

Default security preference:

Do not reveal whether a cross-tenant resource exists.

The internal error code may be:

TENANT_OWNERSHIP_VIOLATION

The specific mapping must be finalized when the first tenant-owned business
endpoint is designed.

Sprint 01 must define the internal failure category even if no public
business endpoint uses it yet.

## 17. Trace Identifier

ProblemDetails responses should include:

traceId

The value should come from the current request tracing activity or
HttpContext trace identifier.

The trace identifier allows support and logs to correlate a client-visible
failure with server-side diagnostics.

Trace IDs are not secrets.

## 18. Optional Development Diagnostic Endpoint

A development-only endpoint may be added if needed to verify tenant
resolution.

Proposed route:

GET /_development/tenant-context

This endpoint is optional.

It must only be mapped in Development.

It must require tenant resolution.

Potential response:

{
  "isResolved": true,
  "tenantId": "3f54449e-9b42-4e32-92ab-7f52b6e8724a"
}

The endpoint must not return:

- Tenant name
- Tenant users
- Accounting data
- Database details
- Authorization information

This endpoint must not exist in production.

The preferred approach is to avoid adding it unless integration testing or
manual verification clearly benefits from it.

## 19. Middleware Ordering

The conceptual request pipeline order is:

1. Exception handling
2. HTTPS redirection where enabled
3. Routing and endpoint selection
4. Future authentication
5. Future authorization
6. Tenant resolution
7. Endpoint execution

Because endpoint metadata is needed, tenant resolution must run after an
endpoint has been selected.

When authentication is introduced, tenant resolution should occur after
authentication and before tenant-owned endpoint execution.

The exact ASP.NET Core ordering must be verified during implementation.

## 20. OpenAPI Decision

OpenAPI or Swagger is not required in Sprint 01.

It was removed from the initial empty application shell because the generated
package version produced a vulnerability warning.

OpenAPI may be reintroduced later using an approved and secure package
version when business endpoints exist.

The API design documentation remains the source of truth until then.

## 21. Logging Requirements

Tenant resolution logs may include:

- TraceId
- Resolved TenantId
- Resolution outcome
- Request method
- Request path

Logs must not include:

- Database passwords
- Complete request bodies by default
- Sensitive accounting data
- Invalid raw tenant header values when unnecessary
- Stack traces in client responses

Expected invalid client input should not always be logged as an application
error.

Security-sensitive mismatches should be logged with an appropriate severity.

## 22. Rate Limiting

Rate limiting is outside Sprint 01.

Future public authentication and onboarding endpoints must be reviewed for
rate limiting.

The current development tenant resolver must not be presented as a public
production API.

## 23. API Test Cases

Integration tests must cover:

1. GET /health succeeds without X-Tenant-Id when PostgreSQL is available.
2. GET /health does not invoke tenant rejection behavior.
3. A tenant-required endpoint rejects a missing header.
4. A tenant-required endpoint rejects an invalid Guid.
5. A tenant-required endpoint rejects multiple header values.
6. An unknown tenant is rejected.
7. An inactive tenant is rejected.
8. A valid active tenant resolves successfully.
9. ProblemDetails uses application/problem+json.
10. ProblemDetails includes status, title, errorCode, and traceId.
11. Client responses do not expose stack traces or database details.
12. Development header resolution is not enabled outside Development.

If no business endpoint exists, test-only or development-only endpoint
mechanisms may be used carefully to exercise tenant resolution.

## 24. Out of Scope

The following API concerns are outside Sprint 01:

- Authentication endpoints
- Registration
- Login
- Refresh tokens
- Tenant membership
- Role and permission APIs
- Tenant administration APIs
- Customer APIs
- Invoice APIs
- OCR APIs
- Document upload APIs
- Billing APIs
- Subscription APIs
- Audit log APIs
- API versioning infrastructure
- OpenAPI generation
- Rate limiting
- CORS production policy
- Idempotency keys
- Pagination conventions

## 25. Resolved Decisions

This document resolves:

- Health route: GET /health
- Health endpoint is tenant-independent
- Development tenant header: X-Tenant-Id
- Header format: Guid
- Tenant errors use ProblemDetails
- Problem content type: application/problem+json
- Machine-readable errors use errorCode
- Error codes use uppercase snake case
- Responses include traceId
- Tenant-independent behavior uses endpoint metadata
- Header-based resolution is Development-only
- OpenAPI is deferred

## 26. Remaining Open Questions

The following remain for implementation or later feature designs:

- Final metadata marker type name
- Exact ProblemDetails type URI production domain
- Whether a development diagnostic endpoint is necessary
- Final 404 versus 403 policy for cross-tenant resource access
- Future API versioning library or strategy
- Production tenant claim names
- Production tenant membership authorization policy
- CORS policy
- Rate limiting policy

## 27. Acceptance of This Design

This API design is approved when:

- Health endpoint behavior is accepted
- Development header behavior is accepted
- Tenant-independent metadata strategy is accepted
- ProblemDetails contract is accepted
- Error codes are accepted
- Status code mappings are accepted
- Middleware ordering is accepted
- OpenAPI deferral is accepted

