# Coding Standards

## 1. General

1. Target .NET 10.
2. Enable nullable reference types.
3. Use implicit usings where already configured.
4. Treat warnings as defects during review.
5. Write readable code before clever code.
6. Use English for code, identifiers, comments, and documentation.
7. Keep methods focused.
8. Avoid unnecessary comments.
9. Comments should explain why, not restate what.
10. Remove generated placeholder files.

## 2. Naming

Use PascalCase for:

- Types
- Public members
- Methods
- Properties
- Enums

Use camelCase for:

- Parameters
- Local variables

Private fields use an underscore prefix.

Example:

private readonly TimeProvider _timeProvider;

Interfaces use the I prefix.

Examples:

ICurrentTenant
ITenantOwned

Async methods end with Async.

Cancellation token parameters should normally be named:

cancellationToken

## 3. Namespaces

Use file-scoped namespaces.

Namespace names must match the project and logical folder.

Example:

namespace SaaSPlatform.Domain.Common;

Do not use deeply nested namespaces without need.

## 4. File Structure

Prefer one primary type per file.

The file name must match the primary type.

Examples:

BaseEntity.cs
ITenantOwned.cs
Tenant.cs

Keep small private implementation details inside the owning file only when
they are not reused elsewhere.

## 5. C# Style

Prefer:

- Pattern matching where clearer
- Guard clauses
- Expression-bodied members for simple members
- var when the type is obvious from the right-hand side
- Explicit types when they improve clarity
- Collection expressions where they improve readability
- sealed classes when inheritance is not intended

Avoid:

- Deep nesting
- Boolean parameters with unclear meaning
- Magic strings
- Magic numbers
- Empty catch blocks
- Catching Exception without a clear boundary reason
- Returning null when absence can be represented more safely

## 6. Domain Coding Rules

1. Keep constructors and factory methods aligned with domain invariants.
2. Do not create invalid domain objects and fix them later.
3. Validate required strings.
4. Normalize domain values through deterministic logic.
5. Do not add EF Core annotations to Domain types.
6. Do not expose mutable collections directly.
7. Do not add setters only to satisfy EF Core unless technically necessary.
8. Use private constructors for EF Core only where justified.
9. Domain exceptions should represent real domain failures.
10. Avoid an elaborate exception hierarchy without need.

## 7. Date and Time

Use UTC.

Prefer TimeProvider over direct calls to DateTime.UtcNow.

Use DateTime with UTC semantics unless an approved design requires
DateTimeOffset.

Names should include Utc where the UTC meaning is important.

Examples:

CreatedAtUtc
UpdatedAtUtc

## 8. Guid Rules

Use Guid for entity identifiers in the current architecture.

Do not use Guid.Empty as a valid persisted identifier.

Identifier generation belongs in application or domain creation logic unless a
later approved design changes this.

## 9. Async Rules

1. Use async APIs for database and network I/O.
2. Accept CancellationToken at application and infrastructure boundaries.
3. Pass CancellationToken through to downstream async calls.
4. Do not use .Result, .Wait(), or blocking-over-async.
5. Do not add Async to methods that do not perform asynchronous work.

## 10. Entity Framework Core Rules

1. Use separate IEntityTypeConfiguration classes.
2. Keep persistence configuration in Infrastructure.
3. Specify maximum lengths explicitly.
4. Specify required fields explicitly.
5. Name important indexes and constraints consistently.
6. Review generated SQL.
7. Avoid lazy loading.
8. Avoid Include chains without reviewing query impact.
9. Use AsNoTracking for read-only queries where appropriate.
10. Do not use IgnoreQueryFilters without explicit security justification.

## 11. API Rules

1. Return ProblemDetails for standardized errors.
2. Do not expose exception messages directly.
3. Use camelCase JSON.
4. Validate request input at the boundary.
5. Do not accept server-controlled fields from clients.
6. Do not return persistence entities directly as public API contracts.
7. Avoid leaking internal IDs unless they are part of the approved API design.
8. Include trace correlation for failures.
9. Do not expose database or infrastructure details.
10. Keep health responses minimal.

## 12. Testing Rules

1. Test behavior, not implementation details.
2. Use descriptive test names.
3. Follow Arrange, Act, Assert.
4. One primary behavior per test.
5. Avoid excessive mocking.
6. Prefer real PostgreSQL for persistence integration tests.
7. Unit tests must remain fast and deterministic.
8. Use TimeProvider or test doubles for time-sensitive behavior.
9. Always test cross-tenant access failure paths.
10. Do not leave placeholder tests.

Recommended naming style:

MethodName_WhenCondition_ShouldExpectedResult

Alternative behavior-focused naming is acceptable when consistently applied.

## 13. Formatting and Validation

All code must conform to .editorconfig.

Before reporting completion, run:

dotnet build SaaSPlatform.slnx
dotnet test SaaSPlatform.slnx
git diff --check

Only run tests relevant to the task when the full suite is unavailable, but
state this clearly.

