# MediatR to PP.Mediator Migration Guide

> Step-by-step guide for teams migrating from MediatR to [PP.Mediator](https://github.com/ProcessPoint/PP.Mediator).
> PP.Mediator is a runtime reflection-based mediator — no source generation required.

## Why Migrate

- **License** — PP.Mediator is under proprietary license of Process Point.
- **Familiar API** — Intentionally close to MediatR's, making migration straightforward.
- **Explicit CQRS** — Dedicated `ICommand<T>` and `IQuery<T>` message types with their own handler interfaces.
- **Advanced pipeline** — Built-in `MessagePreProcessor`, `MessagePostProcessor`, and `MessageExceptionHandler` base classes.
- **Lightweight** — Single dependency: `Microsoft.Extensions.DependencyInjection.Abstractions`.

## API Comparison

| Concept | MediatR | PP.Mediator |
|---------|---------|-------------|
| Namespace | `MediatR` | `PP.Mediator` |
| Request interface | `IRequest<TResponse>` | `IRequest<TResponse>` |
| Request (no response) | `IRequest` | `IRequest` (returns `Unit`) |
| Command interface | *(not built-in)* | `ICommand<TResponse>` / `ICommand` |
| Query interface | *(not built-in)* | `IQuery<TResponse>` |
| Notification | `INotification` | `INotification` |
| Request handler | `IRequestHandler<TRequest, TResponse>` | `IRequestHandler<TRequest, TResponse>` |
| Command handler | *(not built-in)* | `ICommandHandler<TCommand, TResponse>` |
| Query handler | *(not built-in)* | `IQueryHandler<TQuery, TResponse>` |
| Notification handler | `INotificationHandler<T>` | `INotificationHandler<T>` |
| Pipeline behavior | `IPipelineBehavior<TRequest, TResponse>` | `IPipelineBehavior<TMessage, TResponse>` |
| Mediator facade | `IMediator` | `IMediator` (= `ISender` + `IPublisher`) |
| Send dispatch | `ISender.Send(request, ct)` | `ISender.Send(request, ct)` |
| Publish dispatch | `IPublisher.Publish(notification, ct)` | `IPublisher.Publish(notification, ct)` |
| DI registration | `services.AddMediatR(cfg => ...)` | `services.AddMediator()` |
| Handler return type | `Task<TResponse>` | `ValueTask<TResponse>` |
| Pipeline `next()` call | `next()` | `next(message, ct)` |
| Void return type | `Unit` (MediatR) | `Unit` (PP.Mediator) |

## Key Differences

### 1. `Task<T>` → `ValueTask<T>`

All handler methods return `ValueTask<T>` instead of `Task<T>`.

```csharp
// MediatR
public class CreateOrderHandler : IRequestHandler<CreateOrderCommand, OrderResponse>
{
    public async Task<OrderResponse> Handle(CreateOrderCommand request, CancellationToken ct)
    {
        // ...
    }
}

// PP.Mediator
public sealed class CreateOrderHandler : IRequestHandler<CreateOrderCommand, OrderResponse>
{
    public async ValueTask<OrderResponse> Handle(CreateOrderCommand request, CancellationToken ct)
    {
        // ...
    }
}
```

### 2. Pipeline Behavior Delegate Signature

The `next` delegate requires `message` and `ct` parameters. The generic constraint uses `IMessage` instead of `IRequest<TResponse>`.

```csharp
// MediatR
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        // Before
        var response = await next(); // <-- no args
        // After
        return response;
    }
}

// PP.Mediator
public sealed class LoggingBehavior<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>
    where TMessage : notnull, IMessage
{
    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken ct)
    {
        // Before
        var response = await next(message, ct); // <-- pass message + ct
        // After
        return response;
    }
}
```

### 3. Namespace Changes

```csharp
// MediatR
using MediatR;

// PP.Mediator
using PP.Mediator;
```

### 4. Registration

```csharp
// MediatR
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

// PP.Mediator — scans assemblies via reflection, defaults to calling assembly
builder.Services.AddMediator();

// Or with explicit assemblies and lifetime
builder.Services.AddMediator(ServiceLifetime.Scoped, typeof(Program).Assembly);
```

### 5. CQRS Message Types (Optional Enhancement)

PP.Mediator provides dedicated `ICommand<T>` and `IQuery<T>` interfaces. You can optionally migrate `IRequest<T>` usages to the more explicit CQRS types:

```csharp
// MediatR — everything is IRequest<T>
public record CreateOrderCommand(string Item) : IRequest<OrderResponse>;
public record GetOrderQuery(int Id) : IRequest<OrderResponse>;

// PP.Mediator — explicit CQRS types (optional, IRequest<T> still works)
public record CreateOrderCommand(string Item) : ICommand<OrderResponse>;
public record GetOrderQuery(int Id) : IQuery<OrderResponse>;

// Handlers use the matching interface
public sealed class CreateOrderHandler : ICommandHandler<CreateOrderCommand, OrderResponse> { ... }
public sealed class GetOrderHandler : IQueryHandler<GetOrderQuery, OrderResponse> { ... }
```

### 6. Pre/Post Processors and Exception Handlers

PP.Mediator provides built-in base classes for common pipeline patterns:

```csharp
// Pre-processor — runs before the handler
public sealed class ValidationProcessor<TMessage, TResponse>
    : MessagePreProcessor<TMessage, TResponse>
    where TMessage : notnull, IMessage
{
    protected override ValueTask Handle(TMessage message, CancellationToken ct)
    {
        // Validate message before handler runs
    }
}

// Post-processor — runs after the handler
public sealed class AuditProcessor<TMessage, TResponse>
    : MessagePostProcessor<TMessage, TResponse>
    where TMessage : notnull, IMessage
{
    protected override ValueTask Handle(TMessage message, TResponse response, CancellationToken ct)
    {
        // Audit after handler completes
    }
}

// Exception handler — catches and optionally handles exceptions
public sealed class DbExceptionHandler<TMessage, TResponse>
    : MessageExceptionHandler<TMessage, TResponse, DbUpdateException>
    where TMessage : notnull, IMessage
{
    protected override ValueTask<ExceptionHandlingResult<TResponse>> Handle(
        TMessage message, DbUpdateException exception, CancellationToken ct)
    {
        // Return NotHandled to rethrow, or Handled(response) to swallow
        return ValueTask.FromResult(NotHandled);
    }
}
```

## Migration Checklist

1. **Swap NuGet packages**
   ```bash
   dotnet remove package MediatR
   dotnet remove package MediatR.Extensions.Microsoft.DependencyInjection
   dotnet add package PP.Mediator
   ```

2. **Find-and-replace namespaces**
   - `using MediatR;` → `using PP.Mediator;`

3. **Update handler return types**
   - `Task<TResponse>` → `ValueTask<TResponse>` on all `Handle` methods
   - `Task` → `ValueTask` on notification handlers

4. **Update pipeline behaviors**
   - `RequestHandlerDelegate<TResponse>` → `MessageHandlerDelegate<TMessage, TResponse>`
   - `await next()` → `await next(message, ct)`
   - Generic constraint `where TRequest : IRequest<TResponse>` → `where TMessage : notnull, IMessage`
   - Rename `TRequest` to `TMessage` in pipeline behavior type parameters

5. **Update DI registration**
   - `services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(...))` → `services.AddMediator()`
   - Pass assemblies explicitly if handlers are in other assemblies: `services.AddMediator(assemblies: typeof(MyHandler).Assembly)`

6. **(Optional) Adopt CQRS types**
   - `IRequest<T>` → `ICommand<T>` for write operations, `IQuery<T>` for read operations
   - `IRequestHandler<,>` → `ICommandHandler<,>` or `IQueryHandler<,>` accordingly

7. **Seal handler classes** — Use `sealed` on handler classes for performance.

8. **Build and fix** — The compiler will report errors for any handlers with incorrect signatures.

9. **Run tests** — Verify all handler and behavior tests pass.

## Common Gotchas

- **`ValueTask` cannot be awaited multiple times** — If you were caching or branching on `Task<T>` in behaviors, use `.AsTask()` to convert.
- **Generic constraints on behaviors** — PP.Mediator uses `where TMessage : notnull, IMessage` (not `IRequest<TResponse>`). This means behaviors apply to commands, queries, and requests.
- **`IMediator` is available** — Unlike some alternatives, PP.Mediator provides `IMediator` as a convenience facade combining `ISender` + `IPublisher`. You can inject either.
- **No source generation** — PP.Mediator uses runtime reflection. There are no source generator analyzers — incorrect registrations will fail at runtime, not compile time.
- **Assembly scanning** — If handlers are in multiple assemblies, pass all assemblies to `AddMediator()`. By default, only the calling assembly is scanned.