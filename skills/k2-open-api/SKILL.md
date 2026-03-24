---
name: k2-open-api
description: >
  OpenAPI 2.0 (Swagger) configuration for Nintex K2 integration. K2 requires
  Swagger 2.0 spec format with Swashbuckle, AdditionalProperties enabled on all
  schemas, and dynamic server URL resolution. Load this skill when building APIs
  consumed by Nintex K2, or when the user mentions "K2", "Nintex K2", "Swagger 2.0",
  "OpenAPI 2.0", "K2 connector", "K2 SmartObject", or "K2 integration".
---

# Nintex K2 OpenAPI

## Core Principles

1. **OpenAPI 2.0 is mandatory** — K2 does not support OpenAPI 3.x. You must configure Swashbuckle to emit the Swagger 2.0 spec explicitly via `OpenApiSpecVersion.OpenApi2_0`. The built-in .NET OpenAPI (`Microsoft.AspNetCore.OpenApi`) only generates 3.x and cannot be used.
2. **AdditionalProperties must be allowed** — K2 fails to parse schemas where `additionalProperties` is not set. A document filter must explicitly set `AdditionalPropertiesAllowed = true` on every schema that lacks an `AdditionalProperties` definition.
3. **Dynamic server URL** — K2 reads the server URL from the spec. Use a `PreSerializeFilter` to set it from the incoming request so the spec works across environments (localhost, staging, production) without manual edits.
4. **Swashbuckle is required here** — Unlike standard .NET 10 projects where Swashbuckle is deprecated, K2 integration requires it because the built-in OpenAPI package does not support the 2.0 spec format.

## Patterns

### Required NuGet Package

```bash
dotnet add package Swashbuckle.AspNetCore
```

### SwaggerGen Registration with Document Filter

Register `SwaggerGen` with the `AdditionalParametersDocumentFilter` to ensure all schemas are K2-compatible.

```csharp
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "My K2 API",
        Version = "v1",
        Description = "API consumed by Nintex K2 SmartObjects"
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

### Swagger Middleware with OpenAPI 2.0 and Dynamic Server URL

Configure the Swagger middleware to output OpenAPI 2.0 and resolve the server URL dynamically from the incoming request.

```csharp
app.UseSwagger(c =>
{
    c.OpenApiVersion = OpenApiSpecVersion.OpenApi2_0;
    c.PreSerializeFilters.Add((swagger, httpReq) =>
    {
        swagger.Servers =
        [
            new OpenApiServer
            {
                Url = $"{httpReq.Scheme}://{httpReq.Host.Value}{httpReq.PathBase}"
            }
        ];
    });
});
```

### Complete Program.cs Setup

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "My K2 API",
        Version = "v1"
    });
    c.DocumentFilter<AdditionalParametersDocumentFilter>();
});

var app = builder.Build();

app.UseSwagger(c =>
{
    c.OpenApiVersion = OpenApiSpecVersion.OpenApi2_0;
    c.PreSerializeFilters.Add((swagger, httpReq) =>
    {
        swagger.Servers =
        [
            new OpenApiServer
            {
                Url = $"{httpReq.Scheme}://{httpReq.Host.Value}{httpReq.PathBase}"
            }
        ];
    });
});

// Optional: Swagger UI for development testing
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUI();
}

app.MapEndpoints();
app.Run();
```

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

### Using Built-in .NET OpenAPI for K2

```csharp
// BAD — built-in OpenAPI only generates 3.x, K2 cannot parse it
builder.Services.AddOpenApi();
app.MapOpenApi();

// GOOD — Swashbuckle with explicit 2.0 version
builder.Services.AddSwaggerGen(c =>
{
    c.DocumentFilter<AdditionalParametersDocumentFilter>();
});
app.UseSwagger(c =>
{
    c.OpenApiVersion = OpenApiSpecVersion.OpenApi2_0;
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

### Hardcoded Server URL

```csharp
// BAD — breaks when deployed to a different environment
swagger.Servers = [new OpenApiServer { Url = "https://localhost:5001" }];

// GOOD — resolved dynamically from the request
swagger.Servers =
[
    new OpenApiServer
    {
        Url = $"{httpReq.Scheme}://{httpReq.Host.Value}{httpReq.PathBase}"
    }
];
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
| New API for K2 consumption | Use this skill — Swashbuckle + OpenAPI 2.0 |
| API consumed by both K2 and modern clients | Serve Swagger 2.0 for K2, consider a separate OpenAPI 3.x doc for others |
| API not consumed by K2 | Use the `openapi` skill with built-in .NET OpenAPI instead |
| Complex domain models | Flatten into K2-friendly DTOs at the API boundary |
| Multiple environments | Use `PreSerializeFilters` for dynamic server URL — never hardcode |
| K2 SmartObject registration fails | Check: OpenAPI version is 2.0, `AdditionalPropertiesAllowed` is set, DTOs are flat |

## Required Usings

```csharp
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
```
