# [Project Name] — Blazor UI (Presentation Layer)

> Copy this file into your project root and customize the sections below.

## Project Context

This is a .NET 10 Blazor application serving as the **presentation layer only**. It contains no domain logic, no entities, and no data access. It communicates with the backend via one of two integration modes:

- **API mode** — calls a REST API using typed `HttpClient` wrappers
- **CQRS mode** — dispatches queries and commands through Mediator (direct application layer reference in a monolith)

Active integration mode: **[API / CQRS]**

Render mode: **[Server / WebAssembly / Auto]**

## Tech Stack

- **.NET 10** / C# 14
- **Blazor** — [Server / WebAssembly / Auto] interactive render mode
- **ASP.NET Core** — hosting, authentication middleware
- **Serilog** — structured logging
- **xUnit v3** + **bUnit** — component and integration testing

### API mode only

- **`IHttpClientFactory`** — typed HTTP clients per backend resource group

### CQRS mode only

- **Mediator** (`Mediator` source-generator package) — zero-overhead CQRS dispatch
- **[ProjectName].Application** — project reference to the application layer contracts

## Architecture

```
src/
  [ProjectName].UI/
    Components/
      Layout/
        MainLayout.razor
        NavMenu.razor
      Pages/
        Home.razor
        [Feature]/
          [FeaturePage].razor
          [FeatureForm].razor
      Shared/
        [ReusableComponent].razor
        ErrorBoundaryWrapper.razor
      _Imports.razor
      App.razor
      Routes.razor
    ViewModels/
      [Feature]/
        [Feature]ViewModel.cs        # UI state, not domain model
    Clients/                         # API mode: typed HTTP clients
      [Feature]/
        I[Feature]Client.cs
        [Feature]Client.cs
    Queries/                         # CQRS mode: thin query/command wrappers if needed
      [Feature]/
        Get[Feature]ListQuery.cs
    Program.cs
  [ProjectName].UI.Client/           # Only for WebAssembly/Auto mode
    Pages/
      [InteractivePage].razor
    Clients/
      [ClientSideFeature]Client.cs
    _Imports.razor
tests/
  [ProjectName].UI.Tests/
    Components/
      [Component]Tests.cs
    Clients/                         # API mode
      [Feature]ClientTests.cs
    Fixtures/
      TestContext.cs
```

> **No `Models/` with entities. No `Data/` folder. No migrations.**
> All data contracts come from the backend — as DTOs, response records, or Mediator response types.

### Component Organization

Components follow feature-based folders under `Components/Pages/`. Each feature gets its own subfolder. Reusable UI elements live in `Components/Shared/`.

### Render Mode Strategy

Apply render modes at the component level, not globally:

```razor
@* Interactive server — use for forms, real-time UI *@
@rendermode InteractiveServer

@* Auto — server-first, transitions to WASM *@
@rendermode InteractiveAuto
```

Static SSR pages require no render mode directive.

### ViewModel Pattern

Components bind to view models, never to raw backend DTOs:

```csharp
// ViewModels/Orders/OrderListViewModel.cs
public sealed class OrderListViewModel
{
    public IReadOnlyList<OrderRowItem> Rows { get; init; } = [];
    public bool IsLoading { get; set; }
    public string? ErrorMessage { get; set; }
}

public sealed record OrderRowItem(Guid Id, string CustomerName, decimal Total);
```

Map from backend responses in the client/query layer, not in the component.

## Integration Patterns

### API Mode — Typed HTTP Client

```csharp
// Clients/Orders/IOrderClient.cs
public interface IOrderClient
{
    Task<Result<List<OrderRowItem>>> GetOrdersAsync(CancellationToken ct = default);
    Task<Result<OrderDetail>> GetOrderAsync(Guid id, CancellationToken ct = default);
    Task<Result<Guid>> CreateOrderAsync(CreateOrderRequest request, CancellationToken ct = default);
}

// Clients/Orders/OrderClient.cs
internal sealed class OrderClient(HttpClient http) : IOrderClient
{
    public async Task<Result<List<OrderRowItem>>> GetOrdersAsync(CancellationToken ct = default)
    {
        var response = await http.GetAsync("api/orders", ct);
        if (!response.IsSuccessStatusCode)
            return Result.Failure<List<OrderRowItem>>(await ReadProblemAsync(response, ct));

        var items = await response.Content.ReadFromJsonAsync<List<OrderRowItem>>(ct);
        return Result.Success(items!);
    }

    private static async Task<Error> ReadProblemAsync(HttpResponseMessage response, CancellationToken ct)
    {
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(ct);
        return new Error(problem?.Title ?? response.ReasonPhrase ?? "Unknown error");
    }
}

// Registration in Program.cs
builder.Services.AddHttpClient<IOrderClient, OrderClient>(client =>
    client.BaseAddress = new Uri(builder.Configuration["Backend:BaseUrl"]!));
```

### CQRS Mode — Mediator Dispatch

```csharp
// Component dispatches a query directly — no extra wrapper needed
@inject IMediator Mediator

@code {
    private OrderListViewModel _vm = new();

    protected override async Task OnInitializedAsync()
    {
        _vm = _vm with { IsLoading = true };
        var result = await Mediator.Send(new GetOrderListQuery(), CancellationToken.None);
        _vm = result.Match(
            orders => new OrderListViewModel { Rows = orders },
            error => new OrderListViewModel { ErrorMessage = error.Message }
        );
    }
}
```

### Component Using Either Client or Mediator

```razor
@page "/orders"
@inject IOrderClient OrderClient   @* API mode *@
@* @inject IMediator Mediator     @*  CQRS mode *@

<PageTitle>Orders</PageTitle>

@if (_vm.IsLoading)
{
    <p>Loading…</p>
}
else if (_vm.ErrorMessage is not null)
{
    <p class="error">@_vm.ErrorMessage</p>
}
else
{
    <OrderTable Rows="_vm.Rows" />
}

@code {
    private OrderListViewModel _vm = new() { IsLoading = true };

    protected override async Task OnInitializedAsync()
    {
        var result = await OrderClient.GetOrdersAsync();
        _vm = result.Match(
            rows => new OrderListViewModel { Rows = rows },
            err => new OrderListViewModel { ErrorMessage = err.Message }
        );
    }
}
```

## Coding Standards

- **C# 14 features** — Primary constructors, collection expressions, `field` keyword, records, pattern matching
- **File-scoped namespaces** — Always
- **Naming** — PascalCase for public members, camelCase for local variables, async suffix on all async methods
- **No regions** — Ever
- **One component per file** — Except tiny render fragments
- **`@code` at the bottom** of every `.razor` file
- **`@inject` at the top**, one per line
- **Prefer `EventCallback<T>`** over `Action<T>` for component events

## Skills

Load these dotnet-claude-kit skills for context:

- `modern-csharp` — C# 14 language features and idioms
- `architecture-advisor` — Architecture guidance for the overall system
- `authentication` — ASP.NET Core Identity, cookie auth, authorization policies
- `error-handling` — Result pattern, error boundaries, ProblemDetails mapping
- `testing` — xUnit v3, bUnit component testing
- `configuration` — Options pattern, secrets management
- `dependency-injection` — Service registration, scoped vs transient lifetimes
- `logging` — Serilog, OpenTelemetry
- `workflow-mastery` — Parallel worktrees, verification loops, subagent patterns
- `self-correction-loop` — Capture corrections as permanent rules in MEMORY.md
- `wrap-up-ritual` — Structured session handoff to `.claude/handoff.md`
- `context-discipline` — Token budget management, MCP-first navigation

## MCP Tools

> **Setup:** Install once globally with `dotnet tool install -g CWM.RoslynNavigator` and register with `claude mcp add --scope user cwm-roslyn-navigator -- cwm-roslyn-navigator --solution ${workspaceFolder}`.

- **Before modifying a type** — Use `find_symbol` to locate it, `get_public_api` to understand its surface
- **Before adding a reference** — Use `find_references` to understand existing usage
- **To understand architecture** — Use `get_project_graph` to see project dependencies
- **To check for errors** — Use `get_diagnostics` after changes

## Commands

```bash
# Build
dotnet build

# Run (development with hot reload)
dotnet watch --project src/[ProjectName].UI

# Run without hot reload
dotnet run --project src/[ProjectName].UI

# Run tests
dotnet test

# Run bUnit component tests only
dotnet test --filter "Category=Component"

# Format check
dotnet format --verify-no-changes
```

## Workflow

- **Plan first** — Enter plan mode for any non-trivial task. Iterate until the plan is solid before writing code.
- **Verify before done** — Run `dotnet build` and `dotnet test` after changes. Ask: "Would a staff engineer approve this?"
- **Fix bugs autonomously** — Investigate and fix without hand-holding. Check logs, errors, failing tests.
- **Stop and re-plan** — If implementation goes sideways, STOP and re-plan.
- **Use subagents** — Offload research and parallel analysis to subagents.

## Anti-patterns

Do NOT generate code that:

- **Accesses a database directly** — This layer has no DbContext. All data comes from the backend.
- **Defines domain entities or business logic** — No `Order`, `Customer`, `Product` entity classes here; only view models and DTOs.
- **Creates `new HttpClient()`** — Always use `IHttpClientFactory` or typed clients registered via DI.
- **Uses `async void`** — Always return `Task`; the sole exception is component lifecycle handlers where the framework requires it.
- **Blocks with `.Result` or `.Wait()`** — Await instead.
- **Injects a client or mediator at the layout level** — Data fetching belongs in page components, not layout.
- **Uses `JSRuntime` for things CSS or Blazor can do natively** — Prefer CSS classes, `@bind`, `NavigationManager`, `FocusAsync()`.
- **Mixes render modes without understanding the boundary** — Interactive Server and WASM cannot share DI state; data must cross via parameters or API calls.
- **Puts heavy logic in `OnInitializedAsync`** — Use `[StreamRendering]` or load data progressively.
- **Uses `StateHasChanged()` excessively** — Manual calls indicate a design problem.
- **Returns raw backend DTOs to the template** — Always map to view models first.
- **Uses `[CascadingParameter]` as a state management solution** — Prefer scoped DI services.
- **Catches bare `Exception`** — Catch specific types; use `ErrorBoundary` for UI-level handling.
- **Uses string interpolation in log messages** — Use structured logging templates: `logger.LogInformation("Loaded {Count} orders", count)`.
- **Hardcodes the backend base URL** — Always read from `IConfiguration` or typed options.
