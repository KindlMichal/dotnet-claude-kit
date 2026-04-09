# Blazor UI Template (Presentation Layer)

## When to Use

Use this template when your Blazor application is a **pure presentation layer** — it has no database, no domain entities, and no business logic of its own. It displays data and collects user input, delegating all decisions to a backend.

Typical scenarios:

- A Blazor frontend in a **Clean Architecture** solution that calls a separate API project
- A Blazor UI inside a **Modular Monolith** that dispatches commands and queries through Mediator to the application layer
- A Blazor app decoupled from persistence for security or scalability reasons

If your Blazor app owns its own database and domain logic, use the `blazor-app` template instead.

## Integration Modes

### API Mode

The UI calls a REST API (same solution or external) using typed `HttpClient` wrappers. Choose this when:

- The backend is deployed separately
- The UI runs as WebAssembly (must call an API — no direct code reference)
- You want a clear contract boundary between frontend and backend teams

### CQRS Mode

The UI dispatches queries and commands through Mediator to the application layer (direct project reference). Choose this when:

- You're building a Blazor Server app within a monolith
- You want type-safe, compile-time-checked communication with the application layer
- You want to avoid HTTP serialization overhead for in-process calls

You can use both modes simultaneously — for example, CQRS for reads (fast, type-safe) and API calls for file uploads or external integrations.

## How to Use

1. Copy `CLAUDE.md` into the root of your Blazor UI project
2. Replace `[ProjectName]` with your actual project name (e.g., `MyApp.UI`)
3. Set the **Active integration mode** in Project Context: `API` or `CQRS`
4. Choose your render mode and update the Project Context section
5. Remove the `Clients/` section from the architecture diagram if using CQRS mode only
6. Remove the `Queries/` section if using API mode only
7. If not using a separate `.Client` project (Server-only mode), remove `[ProjectName].UI.Client/` from the architecture diagram
8. Remove any skills that don't apply to your project

## What's Included

This template configures Claude Code to:

- Treat this project as a **presentation layer only** — no entities, no data access, no migrations
- Organize components by feature under `Components/Pages/`
- Use **view models** as the UI's data contract (never raw backend DTOs in templates)
- Implement typed HTTP clients or Mediator dispatch cleanly and consistently
- Map backend responses to view models in the client/query layer, not in components
- Apply render modes at the component level
- Write component tests with bUnit
- Avoid common Blazor pitfalls (JS interop overuse, render mode confusion, excessive `StateHasChanged`)
- Use structured logging with Serilog

## Customization

### Switching Integration Modes

To switch from API mode to CQRS mode (or vice versa), update:

1. The **Active integration mode** line in Project Context
2. The `@inject` directives in your components (`IOrderClient` → `IMediator`)
3. The `Clients/` vs `Queries/` folder in the architecture section
4. DI registration in `Program.cs`

### Adding Authentication

For cookie-based SSR auth (most Blazor Server apps), add the `authentication` skill and configure `AddAuthentication().AddCookie()` in `Program.cs`.

For OIDC/OAuth (common for WASM), add an OIDC provider and configure `AddOidcAuthentication()` in the `.Client` project.

### Adding Real-time Features

For SignalR beyond Blazor's circuit, add hub clients to a `Hubs/` folder. Document hub contracts in the architecture section. Note: hub connections must be managed carefully around component lifecycle to avoid leaks.

### Deploying Alongside the API

If the Blazor UI and the API are co-hosted (same ASP.NET Core process), the typed client's `BaseAddress` can be the same host — no CORS configuration needed. If deployed separately, configure CORS on the API and set the backend base URL in `appsettings.json`.
