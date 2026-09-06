# Codex Prompt Template

## Purpose

This template defines the standard format for every future implementation task.

Every implementation prompt should reference:

- docs/ai/codex-rules.md
- docs/ai/architecture-rules.md
- docs/ai/coding-standards.md

rather than repeating those rules.

---

# Standard Prompt

You are working in the SaaSPlatform repository.

Before implementing anything, read:

- docs/ai/codex-rules.md
- docs/ai/architecture-rules.md
- docs/ai/coding-standards.md
- Any sprint requirement, technical design, database design or API design documents related to this task.

Task ID:

[Task ID]

Task:

[One clear responsibility]

Allowed changes:

[List of files/projects that may be modified]

Requirements:

- Requirement 1
- Requirement 2
- Requirement 3

Do not:

- Modify unrelated code
- Add packages unless explicitly requested
- Add migrations unless explicitly requested
- Modify frontend unless explicitly requested
- Commit
- Push

Validation:

Run:

dotnet build SaaSPlatform.slnx

Run tests when required.

Run:

git diff --check

Run:

git status

Report:

- Files created
- Files modified
- Files deleted
- Build result
- Test result
- Warnings
- Assumptions
- Remaining questions

---

# Good Task Examples

Example:

Task:
Create the abstract BaseEntity class inside SaaSPlatform.Domain.

Requirements:

- Add Guid Id property.
- Do not add TenantId.
- Do not add audit fields.
- Do not add EF Core attributes.
- Do not add tests.
- Do not modify other projects.

---

Example:

Task:
Create the ITenantOwned interface.

Requirements:

- Contain only Guid TenantId.
- Place inside Domain.
- No implementation.
- No other interfaces.

---

Example:

Task:
Create Tenant entity.

Requirements:

- Follow Sprint 01 database design.
- No EF Core configuration.
- No DbContext.
- No migrations.

---

## Prompt Design Rules

A good prompt should:

- Have one responsibility.
- Explicitly state allowed files.
- Explicitly state forbidden work.
- Reference existing design documents.
- Require validation.
- Keep implementation scope small.

Avoid prompts such as:

"Build the entire authentication system."

Prefer prompts such as:

"Create the Tenant entity according to Sprint 01 Technical Design."
