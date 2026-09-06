# SaaS Platform

A reusable, domain-agnostic, multi-tenant SaaS platform built with ASP.NET Core, PostgreSQL, and Next.js.

A Tenant represents an organization or workspace using the SaaS platform.

## Architecture

- Modular Monolith
- Shared Database
- Shared Schema
- TenantId-based isolation
- Pragmatic Clean Architecture

## Technology Stack

### Frontend

- Next.js
- TypeScript
- Tailwind CSS

### Backend

- ASP.NET Core
- .NET 10
- C#
- Entity Framework Core
- ASP.NET Core Identity

### Database

- PostgreSQL

### Storage

- S3-compatible object storage

## Repository Structure

```text
src/backend   Backend source code
src/frontend  Frontend source code
docs          Architecture and technical documentation
scripts       Development and automation scripts
tests         Automated tests