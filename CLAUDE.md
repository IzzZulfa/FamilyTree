# Family Tree App

A learning project: a family tree web app with an ASP.NET Core API and a Next.js frontend.
The full product spec lives in `docs/spec.md` — read it before starting any feature.

## Purpose of this project
This is a **learning project**. The owner wants to understand the code, not just have it work.
- After each change, briefly explain what you changed and why.
- When there's a meaningful design choice, present the options and trade-offs before implementing.
- Prefer clear, idiomatic code over clever code.

## Tech stack
- Backend: ASP.NET Core Web API (.NET 10), EF Core with the Npgsql provider, PostgreSQL (local install for dev)
- Frontend: Next.js (App Router), TypeScript, React Flow + ELK for tree layout
- Tests: xUnit for the API

## Repository layout
```
/CLAUDE.md
/docs/spec.md                   Product spec, data model, phases
/family_tree.sln                 Solution file
/family_tree/                    ASP.NET Core Web API project (family_tree.csproj)
/family_tree.Tests/              xUnit tests (to be created in Phase 1)
/web/                           Next.js frontend
/seed/                          Sample family data for development
```

## Commands
- Database: local PostgreSQL install (must be running before starting the API); dev database name `familytree`
- Build API: `dotnet build`
- Run API: `dotnet run --project family_tree`
- Test API: `dotnet test`
- Add migration: `dotnet ef migrations add <Name> --project family_tree`
- Apply migrations: `dotnet ef database update --project family_tree`
- Run frontend: `cd web && npm run dev`

## Conventions
- Controllers (not minimal APIs), grouped by resource.
- Use `record` types for request/response DTOs; never expose EF entities directly from the API.
- Use EF Core directly from services; no generic repository layer.
- Business rules (validation, cycle checks) live in services, not controllers.
- Async all the way down (`async`/`await`, `CancellationToken` on endpoints).
- Relationships are edges (`ParentChild`, `Partnership`). Never store derived relationships such as siblings or grandparents.
- Use snake_case table and column names in Postgres (via the EFCore.NamingConventions package).
- Store enums as strings in the database for readability.
- Keep the connection string in `appsettings.Development.json` / user secrets, never hard-coded.
- Configure all self-referencing foreign keys with `DeleteBehavior.Restrict` and handle deletes explicitly in the service layer.

## Working rules
- Work on one phase (or one feature within a phase) at a time. Don't build ahead of the current phase.
- Anything listed as out of scope in `docs/spec.md` stays out unless asked.
- Write tests for validation rules and tree-traversal logic before moving on.
- Run `dotnet build` and `dotnet test` after changes and fix failures before reporting done.
- Ask before adding a new NuGet or npm package.
