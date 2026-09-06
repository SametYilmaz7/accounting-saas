# Codex Rules

## 1. Purpose

This document defines the mandatory working rules for AI-assisted development
inside the SaaSPlatform repository.

Codex must read this document before implementing any coding task.

These rules apply unless a task explicitly states otherwise.

## 2. General Working Rules

1. Inspect the existing repository before making changes.
2. Do not assume files, types, packages, or conventions exist.
3. Keep every task narrowly scoped.
4. Do not implement unrelated improvements.
5. Do not modify the frontend unless explicitly requested.
6. Do not modify documentation unless explicitly requested.
7. Do not commit or push changes.
8. Do not create branches.
9. Do not change package versions unless explicitly requested.
10. Do not add dependencies without explicit approval.
11. Do not add speculative abstractions.
12. Do not generate placeholder code that is not required.
13. Do not leave commented-out code.
14. Do not suppress warnings without justification.
15. Preserve existing architecture and naming conventions.

## 3. Task Execution Process

For every implementation task:

1. Read:
   - docs/ai/codex-rules.md
   - docs/ai/architecture-rules.md
   - docs/ai/coding-standards.md
   - Any task-specific requirements or design documents

2. Inspect:
   - Relevant projects
   - Existing references
   - Existing namespaces
   - Existing tests
   - Current Git diff

3. State the planned files before editing when practical.

4. Implement only the requested scope.

5. Run the required validation commands.

6. Report:
   - Files created
   - Files modified
   - Files deleted
   - Important design choices
   - Build result
   - Test result
   - Warning count
   - Vulnerability scan result when packages change
   - Any assumptions or unresolved issues

## 4. Scope Discipline

Codex must not combine unrelated responsibilities in one task.

Allowed examples:

- Add one domain base type
- Add one interface
- Add one entity
- Add one EF Core configuration
- Add one middleware
- Add one migration
- Add focused tests for one behavior

Not allowed unless explicitly requested:

- Entity + DbContext + middleware + API + migration in one task
- Authentication + tenancy + authorization in one task
- Refactoring unrelated projects
- Renaming existing architecture
- Adding broad helper libraries

## 5. Validation Rules

For source-code changes, run at minimum:

dotnet build SaaSPlatform.slnx

When tests are affected, also run:

dotnet test SaaSPlatform.slnx

When packages are added or updated, also run:

dotnet restore SaaSPlatform.slnx
dotnet list SaaSPlatform.slnx package --vulnerable --include-transitive

For every task, run:

git diff --check
git status

Final expectations:

- 0 build errors
- 0 build warnings unless explicitly documented and approved
- Passing relevant tests
- No known vulnerable packages introduced
- No secrets committed
- No unrelated file changes

## 6. Git Rules

Codex must not:

- Commit
- Push
- Rebase
- Merge
- Reset
- Force checkout
- Delete branches
- Modify Git history

Codex may inspect:

git status
git diff
git diff --check

The project owner performs commits after review.

## 7. Package Rules

Do not add packages unless the task explicitly names and approves them.

When adding a package:

- Use stable releases only
- Do not use preview, beta, RC, nightly, or deprecated packages
- Keep package versions compatible
- Avoid duplicate direct references
- Use PrivateAssets where appropriate for design-time packages
- Run a vulnerability scan
- Report direct and transitive dependency effects

## 8. Security Rules

1. Tenant isolation is a security boundary.
2. Never trust TenantId supplied by client payloads.
3. Do not expose stack traces or database details to clients.
4. Do not commit passwords, connection strings, tokens, or private keys.
5. Do not log sensitive tenant data.
6. Treat IgnoreQueryFilters as security-sensitive.
7. Treat raw SQL as security-sensitive.
8. Fail closed when tenant context is required but unavailable.
9. Do not weaken validation for convenience.
10. Security-sensitive behavior requires automated tests.

## 9. Output Rules

At the end of every task, provide a concise report containing:

- Scope completed
- Files changed
- Validation commands executed
- Build and test result
- Warnings
- Deviations from the task, if any
- Open questions, if any

Do not claim success if validation failed.

