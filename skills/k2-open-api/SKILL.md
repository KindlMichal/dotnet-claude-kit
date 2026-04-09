---
name: k2-open-api
description: >
  Dual OpenAPI setup for Nintex K2 integration: modern OpenAPI 3.x with Scalar UI
  as the primary documentation, plus a legacy Swagger 2.0 endpoint for K2 compatibility.
  Uses built-in .NET OpenAPI for 3.x and Swashbuckle solely for the 2.0 spec.
  Load this skill when building APIs consumed by Nintex K2, or when the user mentions
  "K2", "Nintex K2", "Swagger 2.0", "OpenAPI 2.0", "K2 connector", "K2 SmartObject",
  or "K2 integration".
---

# Nintex K2 OpenAPI

## Core Principles

1. **Dual-spec architecture** — Serve OpenAPI 3.x as the primary spec (with Scalar UI) and Swagger 2.0 as a legacy endpoint for K2. Modern clients and developers use `/openapi3/v1.json` + Scalar, K2 consumes `/openapi2/v1.json`. Both specs are generated from the same codebase.
2. **Built-in OpenAPI is primary, Swashbuckle is secondary** — .NET 9+ removed Swashbuckle from templates. Use `AddOpenApi()` + `MapOpenApi()` for the modern spec. Swashbuckle (`AddSwaggerGen`) is added solely to produce the OpenAPI 2.0 output that K2 requires.
3. **AdditionalProperties must be allowed** — K2 fails to parse schemas where `additionalProperties` is not set. A document filter must explicitly set `AdditionalPropertiesAllowed = true` on every schema that lacks an `AdditionalProperties` definition.
4. **Dynamic server URL for K2** — K2 reads the server URL from the spec. Use a `PreSerializeFilter` to resolve it from the incoming request so the spec works across environments without manual edits.

## Patterns

### Required NuGet Packages

```bash
dotnet add package Swashbuckle.AspNetCore
dotnet add package Scalar.AspNetCore
```

`Swashbuckle.AspNetCore` is needed only for the legacy OpenAPI 2.0 endpoint. `Scalar.AspNetCore` provides the modern API documentation UI.

### Service Registration (Dual Spec)

Register both the built-in OpenAPI and Swashbuckle side by side. Each serves a different audience.

```csharp
// OpenAPI 3.x — primary spec for modern clients and Scalar UI
builder.Services.AddOpenApi("v1");

// Swagger 2.0 — legacy spec for Nintex K2
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "My API | v1",
        Version = "1.0.0"
    });
    c.DocumentFilter<AdditionalParametersDocumentFilter>();
});
```

### AdditionalParametersDocumentFilter Implementation

K2 requires `additionalProperties` to be explicitly allowed on all schemas. Without this filter, K2 rejects the spec during service instance registration.

```csharp
public sealed class AdditionalParametersDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument openApiDoc, DocumentFilterContext context)
    {
        foreach (var schema in context.SchemaRepository.Schemas)
        {
            if (schema.Value.AdditionalProperties is null)
            {
                schema.Value.AdditionalPropertiesAllowed = true;
            }
        }
    }
}
```

### Middleware Configuration (Dual Endpoints)

Separate route templates keep the two specs at distinct, predictable URLs.

```csharp
// OpenAPI 3.x at /openapi3/v1.json
app.MapOpenApi("/openapi3/{documentName}.json");

// Scalar UI — points to the OpenAPI 3.x spec
app.MapScalarApiReference(options =>
{
    options.OpenApiRoutePattern = "/openapi3/{documentName}.json";
});

// Swagger 2.0 at /openapi2/v1.json — for K2 only
app.UseSwagger(c =>
{
    c.OpenApiVersion = OpenApiSpecVersion.OpenApi2_0;
    c.RouteTemplate = "openapi2/{documentName}.json";
    c.PreSerializeFilters.Add((swagger, httpReq) =>
    {
        swagger.Servers =
        [
            new OpenApiServer
            {
                Url = $"{httpReq.Scheme}://{httpReq.Host.Value}"
            }
        ];
    });
});
```

### Complete Program.cs Setup

```csharp
var builder = WebApplication.CreateBuilder(args);

// OpenAPI 3.x
builder.Services.AddOpenApi("v1");

// Swagger 2.0 for K2
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "My API | v1",
        Version = "1.0.0"
    });
    c.DocumentFilter<AdditionalParametersDocumentFilter>();
});

var app = builder.Build();

// OpenAPI 3.x endpoint
app.MapOpenApi("/openapi3/{documentName}.json");

// Scalar UI for developers
app.MapScalarApiReference(options =>
{
    options.OpenApiRoutePattern = "/openapi3/{documentName}.json";
});

// Swagger 2.0 endpoint for K2
app.UseSwagger(c =>
{
    c.OpenApiVersion = OpenApiSpecVersion.OpenApi2_0;
    c.RouteTemplate = "openapi2/{documentName}.json";
    c.PreSerializeFilters.Add((swagger, httpReq) =>
    {
        swagger.Servers =
        [
            new OpenApiServer
            {
                Url = $"{httpReq.Scheme}://{httpReq.Host.Value}"
            }
        ];
    });
});

app.MapEndpoints();
app.Run();
```

### URL Summary

| URL | Format | Consumer |
|---|---|---|
| `/openapi3/v1.json` | OpenAPI 3.x | Scalar UI, modern clients, code generators |
| `/scalar/v1` | Scalar UI | Developers browsing API docs |
| `/openapi2/v1.json` | Swagger 2.0 | Nintex K2 service instance registration |

### Endpoint Design for K2 SmartObjects

K2 SmartObjects map best to simple CRUD endpoints with flat DTOs. Avoid nested objects where possible — K2 handles flat structures more reliably.

```csharp
public sealed class OrderEndpoints : IEndpointGroup
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/orders")
            .WithTags("Orders");

        group.MapGet("/", ListOrders)
            .WithName("ListOrders")
            .WithSummary("List all orders")
            .Produces<List<OrderDto>>();

        group.MapGet("/{id:guid}", GetOrder)
            .WithName("GetOrder")
            .WithSummary("Get order by ID")
            .Produces<OrderDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateOrder)
            .WithName("CreateOrder")
            .WithSummary("Create a new order")
            .Produces<OrderDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem();
    }
}
```

### K2-Friendly DTO Design

Keep DTOs flat with primitive types. K2 SmartObject properties map 1:1 to DTO properties.

```csharp
// DO — flat DTO with primitive types, K2-friendly
public sealed record OrderDto(
    Guid Id,
    string CustomerName,
    string ProductName,
    int Quantity,
    decimal TotalPrice,
    string Status,
    DateTimeOffset CreatedAt);

// DON'T — nested objects cause K2 mapping issues
public sealed record OrderDto(
    Guid Id,
    CustomerDto Customer,
    List<OrderItemDto> Items);
```

## Anti-patterns

### Using Only Swashbuckle for Everything

```csharp
// BAD — Swashbuckle is deprecated in .NET 9+, don't use it as your primary spec
builder.Services.AddSwaggerGen();
app.UseSwagger();
app.UseSwaggerUI();

// GOOD — built-in OpenAPI + Scalar as primary, Swashbuckle only for K2 legacy endpoint
builder.Services.AddOpenApi("v1");
builder.Services.AddSwaggerGen(c =>
{
    c.DocumentFilter<AdditionalParametersDocumentFilter>();
});
app.MapOpenApi("/openapi3/{documentName}.json");
app.MapScalarApiReference();
app.UseSwagger(c =>
{
    c.OpenApiVersion = OpenApiSpecVersion.OpenApi2_0;
    c.RouteTemplate = "openapi2/{documentName}.json";
});
```

### Using Only Built-in OpenAPI (No K2 Support)

```csharp
// BAD — built-in OpenAPI only generates 3.x, K2 cannot parse it
builder.Services.AddOpenApi();
app.MapOpenApi();

// GOOD — add Swashbuckle alongside for the 2.0 endpoint
builder.Services.AddOpenApi("v1");
builder.Services.AddSwaggerGen(c =>
{
    c.DocumentFilter<AdditionalParametersDocumentFilter>();
});
```

### Missing AdditionalProperties Filter

```csharp
// BAD — K2 rejects schemas without additionalProperties
builder.Services.AddSwaggerGen();

// GOOD — filter ensures all schemas are K2-compatible
builder.Services.AddSwaggerGen(c =>
{
    c.DocumentFilter<AdditionalParametersDocumentFilter>();
});
```

### Same Route for Both Specs

```csharp
// BAD — route collision between OpenAPI 3.x and 2.0
app.MapOpenApi("/openapi/{documentName}.json");
app.UseSwagger(c =>
{
    c.RouteTemplate = "openapi/{documentName}.json";
});

// GOOD — separate routes make the purpose of each endpoint clear
app.MapOpenApi("/openapi3/{documentName}.json");
app.UseSwagger(c =>
{
    c.RouteTemplate = "openapi2/{documentName}.json";
});
```

### Complex Nested DTOs

```csharp
// BAD — K2 SmartObjects struggle with nested object hierarchies
public sealed record OrderDto(
    Guid Id,
    AddressDto ShippingAddress,
    List<LineItemDto> Items);

// GOOD — flatten for K2 compatibility
public sealed record OrderDto(
    Guid Id,
    string ShippingStreet,
    string ShippingCity,
    string ShippingZip,
    int ItemCount,
    decimal TotalPrice);
```

## Decision Guide

| Scenario | Recommendation |
|---|---|
| New API consumed by K2 | Use this skill — dual spec with OpenAPI 3.x + Swagger 2.0 |
| API not consumed by K2 | Use the `openapi` + `scalar` skills instead — no Swashbuckle needed |
| K2 SmartObject registration fails | Check: `/openapi2/v1.json` returns 2.0 spec, `AdditionalPropertiesAllowed` is set, DTOs are flat |
| Complex domain models | Flatten into K2-friendly DTOs at the API boundary |
| Multiple environments | `PreSerializeFilters` resolves the server URL dynamically — never hardcode |
| Removing K2 support later | Delete `AddSwaggerGen`, `UseSwagger`, the document filter, and the Swashbuckle package — the OpenAPI 3.x + Scalar setup remains untouched |

## Required Usings

```csharp
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;
using Swashbuckle.AspNetCore.SwaggerGen;
```
