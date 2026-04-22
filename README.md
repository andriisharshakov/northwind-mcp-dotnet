# northwind-mcp-dotnet

A practical tutorial showing how to build a **Model Context Protocol (MCP) Server** on top of a real REST API, using the well-known Northwind database as a domain foundation.

By the end of this tutorial you will have:
- A CRUD REST API (ASP.NET Core 8 + EF Core + PostgreSQL)
- An MCP Server exposing that API as Claude-callable tools (SSE transport)
- Auth0-based authentication securing both the API and the MCP Server
- An Audit Log with Correlation IDs linking every Claude action to a DB change
- A Docker Compose setup for one-command local startup

> **Why Northwind?**  
> The schema is universally known — customers, orders, products, employees.  
> Readers can skip the domain explanation and focus on what matters: MCP, auth, and audit.

---

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Prerequisites](#prerequisites)
3. [Part 1 — Database Setup](#part-1--database-setup)
4. [Part 2 — CRUD API](#part-2--crud-api)
5. [Part 3 — MCP Server](#part-3--mcp-server)
6. [Part 4 — Auth0 Authentication & Authorization](#part-4--auth0-authentication--authorization)
7. [Part 5 — Audit Log & Correlation IDs](#part-5--audit-log--correlation-ids)
8. [Part 6 — AI-Assisted Quality Control](#part-6--ai-assisted-quality-control)
9. [Part 7 — Docker Compose](#part-7--docker-compose)
10. [Security Considerations for MCP Servers](#security-considerations-for-mcp-servers)

---

## Architecture Overview

```
┌─────────────────────────────────────────────────┐
│                  Claude (AI client)              │
└───────────────────────┬─────────────────────────┘
                        │ MCP Protocol (SSE)
                        ▼
┌─────────────────────────────────────────────────┐
│            NorthwindCrm.Mcp                     │
│         MCP Server — ASP.NET Core 8             │
│                                                 │
│  Tools:                                         │
│  • search_customers                             │
│  • get_order                                    │
│  • create_order                                 │
│  • update_product_price                         │
│  • get_employee_sales                           │
│                                                 │
│  Auth: Auth0 JWT validation                     │
│  Audit: Correlation ID → Audit Log              │
└───────────────────────┬─────────────────────────┘
                        │ HTTP + Bearer token
                        ▼
┌─────────────────────────────────────────────────┐
│            NorthwindCrm.Api                     │
│         REST API — ASP.NET Core 8               │
│                                                 │
│  Endpoints: Customers, Orders, Products,        │
│             Employees                           │
│                                                 │
│  Auth: Auth0 JWT + scope-based authorization    │
│  ORM:  EF Core 8 + Npgsql                       │
└───────────────────────┬─────────────────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────┐
│            PostgreSQL                           │
│  • Northwind schema (customers, orders, ...)    │
│  • audit_log table                              │
│  • JSONB custom_fields on key tables            │
└─────────────────────────────────────────────────┘
```

---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [PostgreSQL 15+](https://www.postgresql.org/download/) (or Docker)
- [Auth0 account](https://auth0.com/) (free tier is sufficient)
- Claude Desktop or any MCP-compatible client

Optional (for Docker setup):
- [Rancher Desktop](https://rancherdesktop.io/) or Docker Desktop

---

## Part 1 — Database Setup

### 1.1 Download Northwind for PostgreSQL

```bash
# Clone the PostgreSQL port of Northwind
git clone https://github.com/pthom/northwind_psql.git
```

### 1.2 Create the database and import

```bash
psql -U postgres -c "CREATE DATABASE northwind;"
psql -U postgres -d northwind -f northwind_psql/northwind.sql
```

Or use DBeaver: open `northwind.sql` → Execute.

### 1.3 Add our extensions (Audit Log + JSONB)

```bash
psql -U postgres -d northwind -f db/audit_log.sql
psql -U postgres -d northwind -f db/custom_fields_migration.sql
```

See [`db/`](./db/) for the full scripts with comments.

---

## Part 2 — CRUD API

**Project:** `src/NorthwindCrm.Api`

### Stack
- ASP.NET Core 8 Web API
- EF Core 8 + Npgsql
- Auth0 JWT authentication
- Swagger / OpenAPI

### Key endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/customers` | List customers (supports `?search=`) |
| GET | `/api/customers/{id}` | Get customer by ID |
| GET | `/api/orders/{id}` | Get order with line items |
| POST | `/api/orders` | Create new order |
| PATCH | `/api/products/{id}/price` | Update product price |
| GET | `/api/employees/{id}/sales` | Sales summary for employee |

### Running locally

```bash
cd src/NorthwindCrm.Api
dotnet run
# Swagger UI: https://localhost:5001/swagger
```

---

## Part 3 — MCP Server

**Project:** `src/NorthwindCrm.Mcp`

### What is MCP?

Model Context Protocol (MCP) is an open standard that lets AI models like Claude call external tools in a structured, type-safe way. Instead of writing custom integration code for every AI feature, you define **tools** with **input schemas** — and Claude decides when and how to call them based on natural language.

### Transport: SSE vs stdio

| Transport | When to use |
|-----------|-------------|
| **stdio** | Local tools — MCP server runs as a child process of the client |
| **SSE** | Remote/cloud servers — persistent HTTP connection, server pushes results |

We use **SSE** because our MCP server is a deployed service, not a local process.

### Tool definitions

Each tool has a name, description, and JSON Schema for its inputs.  
Claude reads the descriptions to decide which tool to call.

Example — `search_customers`:

```csharp
[McpTool("search_customers", "Search Northwind customers by name, contact, or city")]
public async Task<string> SearchCustomers(
    [McpParameter("query", "Customer name, contact person, or city")] string query,
    [McpParameter("limit", "Max results to return")] int limit = 10)
{
    var result = await _httpClient.GetFromJsonAsync<List<CustomerDto>>(
        $"/api/customers?search={query}&limit={limit}");
    return JsonSerializer.Serialize(result);
}
```

### Full tool list

| Tool | Description |
|------|-------------|
| `search_customers` | Search by name / contact / city |
| `get_order` | Full order details with line items |
| `create_order` | Place a new order for a customer |
| `update_product_price` | Change product unit price |
| `get_employee_sales` | Sales summary for an employee |

### Running locally

```bash
cd src/NorthwindCrm.Mcp
dotnet run
# MCP SSE endpoint: https://localhost:5002/sse
```

---

## Part 4 — Auth0 Authentication & Authorization

This is one of the most important sections — securing an MCP Server has nuances that differ from securing a regular API.

### 4.1 How auth flows through the stack

```
Claude client
    │
    │  1. User authenticates with Auth0
    │     → receives JWT access token
    │
    ▼
MCP Server  ──── validates JWT ────► Auth0 JWKS endpoint
    │
    │  2. MCP Server calls API
    │     → forwards JWT (or exchanges for service token)
    │
    ▼
REST API  ──── validates JWT ────► Auth0 JWKS endpoint
    │
    ▼
PostgreSQL  (no direct external access)
```

### 4.2 Auth0 setup

1. Create an **API** in Auth0 dashboard
   - Identifier (audience): `https://northwind-crm-api`
   - Signing algorithm: RS256

2. Create a **Machine-to-Machine Application** for the MCP Server
   - Grants: `client_credentials`
   - Authorized scopes: `read:customers write:orders read:products`

3. Create a **Regular Web Application** for the frontend / Claude Desktop
   - Callback URLs, logout URLs as needed

### 4.3 Scopes and permissions

Define granular scopes — MCP tools should only request what they need:

| Scope | Tools that require it |
|-------|-----------------------|
| `read:customers` | `search_customers` |
| `read:orders` | `get_order` |
| `write:orders` | `create_order` |
| `write:products` | `update_product_price` |
| `read:reports` | `get_employee_sales` |

### 4.4 Validating JWT in ASP.NET Core

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = $"https://{builder.Configuration["Auth0:Domain"]}/";
        options.Audience = builder.Configuration["Auth0:Audience"];
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("read:orders", policy =>
        policy.RequireClaim("scope", "read:orders"));
});
```

### 4.5 Security considerations specific to MCP Servers

MCP Servers introduce security challenges that regular APIs don't have:

**Prompt injection attacks**  
A malicious user can craft natural language input that tricks Claude into calling destructive tools. Mitigations:
- Never expose tools like `delete_all_customers` — keep write tools narrow and specific
- Validate tool inputs server-side regardless of what Claude sends
- Log all tool calls with the original user prompt for audit

**Scope creep via chaining**  
Claude may chain multiple tool calls to achieve something no single tool would allow. Mitigations:
- Apply rate limiting per user/session on the MCP Server
- Set a max tool calls per session limit
- Require re-authentication for sensitive write operations

**Token forwarding**  
Decide whether the MCP Server forwards the user's token to the API (delegation) or uses its own service token (machine-to-machine). Each has trade-offs:

| Approach | Pro | Con |
|----------|-----|-----|
| Forward user token | Full user context in audit log | Token exposure risk |
| M2M service token | Simpler, no token exposure | Loses user identity downstream |
| Token exchange (RFC 8693) | Best of both | More complex setup |

For this tutorial we use **M2M with user context passed as a claim header** — a pragmatic middle ground.

---

## Part 5 — Audit Log & Correlation IDs

Every action Claude takes must be traceable. The audit system links:

```
User prompt → MCP tool call(s) → API request(s) → DB change(s)
```

### 5.1 Correlation ID middleware

Added to both MCP Server and API:

```csharp
app.Use(async (context, next) =>
{
    var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
        ?? Guid.NewGuid().ToString();
    context.Items["CorrelationId"] = correlationId;
    context.Response.Headers["X-Correlation-ID"] = correlationId;
    await next();
});
```

The MCP Server generates a `correlationId` per user prompt and passes it to every API call it makes.

### 5.2 Audit log schema

```sql
CREATE TABLE audit_log (
    id              BIGSERIAL PRIMARY KEY,
    correlation_id  UUID NOT NULL,
    user_prompt     TEXT,           -- original natural language request
    tool_name       TEXT,           -- which MCP tool was called
    tool_input      JSONB,          -- parameters Claude passed
    entity_type     TEXT,           -- e.g. 'order', 'product'
    entity_id       TEXT,
    action          TEXT,           -- INSERT / UPDATE / DELETE
    old_values      JSONB,
    new_values      JSONB,
    performed_by    TEXT,           -- user identity
    performed_at    TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_audit_correlation ON audit_log(correlation_id);
CREATE INDEX idx_audit_performed_at ON audit_log(performed_at DESC);
```

### 5.3 Querying suspicious pairs

Find cases where the prompt and the actual DB action don't match:

```sql
SELECT
    correlation_id,
    user_prompt,
    tool_name,
    tool_input,
    entity_type,
    action,
    new_values
FROM audit_log
WHERE performed_at >= NOW() - INTERVAL '24 hours'
ORDER BY performed_at DESC;
```

---

## Part 6 — AI-Assisted Quality Control

Once audit log is in place, we can feed it to Claude for automated review.

### Concept

```
Audit Log (last N hours)
        │
        ▼
  Claude API call
  "Review these prompt/action pairs.
   Flag any where the action seems
   inconsistent with the user intent."
        │
        ▼
  Flagged pairs → Slack / email to reviewers
```

### What Claude looks for

- Prompt says "find customer" but `create_order` was called
- Prompt mentions one customer but a different entity was modified
- Unusually large quantity or price values
- Multiple destructive operations in a short session

### Why this matters

This creates a feedback loop for catching hallucinations early — especially important during the rollout phase when prompt engineering is still being refined.

See [`docs/mcp-qa-concept.md`](./docs/mcp-qa-concept.md) for full design.

---

## Part 7 — Docker Compose

For readers with Docker available:

```yaml
# docker-compose.yml
services:
  postgres:
    image: postgres:16
    environment:
      POSTGRES_DB: northwind
      POSTGRES_PASSWORD: postgres
    ports:
      - "5432:5432"
    volumes:
      - ./db/northwind.sql:/docker-entrypoint-initdb.d/01-northwind.sql
      - ./db/audit_log.sql:/docker-entrypoint-initdb.d/02-audit.sql

  api:
    build: ./src/NorthwindCrm.Api
    ports:
      - "5001:80"
    depends_on:
      - postgres

  mcp:
    build: ./src/NorthwindCrm.Mcp
    ports:
      - "5002:80"
    depends_on:
      - api
```

```bash
docker compose up
```

---

## Security Considerations for MCP Servers

A dedicated overview of the security landscape for MCP-based systems.  
See [`docs/mcp-security.md`](./docs/mcp-security.md) for the full document.

**Key topics covered:**
- Authentication: OAuth2 / JWT for MCP (the emerging MCP auth spec)
- Authorization: scope-based vs relationship-based (ReBAC with OpenFGA)
- Prompt injection and tool abuse mitigation
- Token handling: delegation vs M2M vs token exchange
- Audit and compliance requirements
- Rate limiting and session management

---

## Project Status

- [ ] Part 1 — Database setup
- [ ] Part 2 — CRUD API
- [ ] Part 3 — MCP Server (tools)
- [ ] Part 4 — Auth0 integration
- [ ] Part 5 — Audit Log
- [ ] Part 6 — AI QC concept
- [ ] Part 7 — Docker Compose

---

## License

MIT
