# AGENTS.md

## Project overview
Tutorial project: building an MCP Server in .NET 8 on top of a Northwind PostgreSQL CRUD API,
with Auth0 authentication and an AI-assisted audit/QC system.

## Key commands
- `dotnet build` — build all projects
- `dotnet run` — run API on http://localhost:5185 (from src/NorthwindCrm.Api)
- `psql -U postgres -d northwind` — connect to DB

## SDK pinning
global.json is located in src/NorthwindCrm.Api/ and pins .NET SDK to 8.0.403 with rollForward: disable.
Do not change target framework without updating global.json first.

## Architecture
- src/NorthwindCrm.Api — REST CRUD API (ASP.NET Core 8, EF Core, PostgreSQL)
- src/NorthwindCrm.Mcp — MCP Server (SSE transport) — in progress
- db/northwind.sql — Northwind schema + seed data
- db/audit_log.sql — audit_log table + JSONB custom_fields extensions

## Key constraints
- PostgreSQL uses snake_case column names — all EF Core models require [Column] attributes
- Connection string lives in appsettings.Development.json (gitignored) — never in appsettings.json
- OrderDetail has composite PK (order_id + product_id) — configured in OnModelCreating
- Northwind PostgreSQL port uses `real` for float columns and `integer` for boolean — models reflect this

## Patterns
- Correlation ID flows via X-Correlation-ID header through MCP Server → API → audit_log
- AuditLogService is scoped — inject into controllers for write operations
- X-User-Prompt header carries original natural language prompt from MCP Server