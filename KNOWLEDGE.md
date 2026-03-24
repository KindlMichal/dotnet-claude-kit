# Referenční znalostní báze dotnet-claude-kit

> Referenční dokumentace, průvodci migrací a záznamy o architektonických rozhodnutích pro vývoj v .NET 10.

## Co jsou znalosti?

Adresář `knowledge/` obsahuje **referenční materiály**, na které odkazují skills a agenti. Na rozdíl od skills (které představují názorové vzory), znalostní dokumenty jsou:

- **Faktické reference** — Breaking changes, verze balíčků, migrační kroky
- **Záznamy o rozhodnutích** — Proč dotnet-claude-kit volí konkrétní architektonické přístupy
- **Průvodci migrací** — Podrobné návody pro běžné přechody
- **Katalog anti-vzorů** — Co NEDĚLAT, s vysvětlením

**Znalostní dokumenty NEJSOU skills.** Nesledují formát frontmatter pro skills a nenačítají se automaticky. Odkazují na ně skills, agenti a příkazy podle potřeby.

---

## Znalostní dokumenty

| Dokument | Typ | Účel | Kdy odkazovat |
|----------|-----|------|---------------|
| `breaking-changes.md` | Průvodce migrací | Breaking changes .NET 9 → 10 | Upgrade projektů na .NET 10 |
| `common-antipatterns.md` | Katalog anti-vzorů | Vzory, kterým se vyhnout | Code review, pre-commit kontroly |
| `common-infrastructure.md` | Knihovna kódu | Infrastrukturní typy ke zkopírování | Implementace Result patternu, rozšíření |
| `dotnet-whats-new.md` | Reference funkcí | Funkce .NET 10 a C# 14 | Seznámení s novými možnostmi |
| `package-recommendations.md` | Katalog balíčků | Prověřené NuGet balíčky | Výběr závislostí |
| `mediatr-to-mediator-migration.md` | Průvodce migrací | Migrace MediatR → Mediator | Soulad s licencemi, výkon |
| `decisions/*.md` | ADR | Architectural Decision Records | Pochopení voleb dotnet-claude-kit |

---

## Průvodce breaking changes

**Soubor:** `knowledge/breaking-changes.md`
**Poslední aktualizace:** Únor 2026 (.NET 10 GA)

### Co je uvnitř

Komplexní průvodce migrací z .NET 9 na .NET 10, pokrývající:

1. **Změny TFM a SDK**
   - Aktualizace `TargetFramework` na `net10.0`
   - Aktualizace `global.json` na SDK 10.0.100
   - Jazyková verze C# 14

2. **Breaking changes v ASP.NET Core**
   - `WithOpenApi()` zastaralé → Použijte `AddOpenApi()` / `MapOpenApi()`
   - `Microsoft.OpenApi` upgradováno na 2.0
   - Změny ve filtrech endpointů Minimal API
   - Pořadí middleware pro autentizaci/autorizaci

3. **Breaking changes v EF Core**
   - Změny v chování rozdělování dotazů
   - Zpracování parametrů `FromSqlRaw`
   - Vylepšení generování migrací
   - Změny v podpoře temporálních tabulek

4. **Breaking changes v runtime**
   - `TimeProvider` nahrazuje `DateTime.Now` v nových API
   - Výchozí nastavení JSON serializace
   - Vylepšení výkonu Regex s breaking chováním

### Příklad použití

```csharp
// Before (.NET 9)
app.MapGet("/api/products", () => { })
    .WithOpenApi();

// After (.NET 10)
builder.Services.AddOpenApi();
app.MapOpenApi();
app.MapGet("/api/products", () => { });
```

**Kdy odkazovat:**
- Spuštění příkazu `/migrate`
- Upgrade existujících projektů
- Řešení problémů s buildem po upgradu

---

## Běžné anti-vzory

**Soubor:** `knowledge/common-antipatterns.md`

### Co je uvnitř

Katalog vzorů, které by Claude Code **nikdy neměl generovat**, s porovnáním ŠPATNĚ/SPRÁVNĚ:

1. **async void** — Pohlcuje výjimky, nelze awaitovat
2. **Task.Result / Task.Wait()** — Riziko deadlocku
3. **new HttpClient()** — Vyčerpání soketů
4. **DateTime.Now** — Netestovatelné, problémy s časovými zónami
5. **Široké catch bloky** — Skrývají skutečné chyby
6. **Interpolace řetězců v logování** — Náklady na výkon
7. **Chybějící CancellationToken** — Nelze zrušit operace
8. **EF Core bez AsNoTracking** — Úniky paměti u dotazů pouze pro čtení
9. **Repository nad EF Core** — Zbytečná abstrakce
10. **Anemické doménové modely** — Veškerá logika v službách

### Příklad

```csharp
// BAD — async void swallows exceptions
public async void ProcessOrder(Order order)
{
    await _repository.SaveAsync(order);
}

// GOOD — always return Task
public async Task ProcessOrderAsync(Order order)
{
    await _repository.SaveAsync(order);
}
```

**Kdy odkazovat:**
- Code review (`/code-review`)
- Pre-commit hooky (`pre-commit-antipattern.sh`)
- Kontroly stavu (`/health-check`)
- Zaškolení nových členů týmu

---

## Společná infrastruktura

**Soubor:** `knowledge/common-infrastructure.md`

### Co je uvnitř

Implementace infrastrukturních typů ke zkopírování, na které odkazují skills:

1. **Result Pattern**
   - Třídy `Result` a `Result<T>`
   - Tovární metody Success/Failure
   - Kolekce chyb

2. **Rozšíření Result na ProblemDetails**
   - Extension metoda `ToProblemDetails()`
   - Mapuje chyby Result na RFC 7807 Problem Details

3. **Základní třída Entity**
   - `Entity` s `Guid` ID
   - Časové značky `CreatedAt` / `UpdatedAt`
   - Porovnání podle ID

4. **Rozhraní IEndpointGroup**
   - Vzor automatického vyhledávání pro endpointy Minimal API
   - `EndpointExtensions.MapEndpoints()`

5. **Pipeline Behavior pro validaci**
   - Integrace FluentValidation s Mediator
   - Vrací validační chyby jako Result

### Příklad

```csharp
// Copy from common-infrastructure.md
public class Result<T> : Result
{
    public T Value { get; }

    internal Result(T value) : base(true) => Value = value;
    internal Result(IEnumerable<string> errors) : base(false, [..errors])
        => Value = default!;
}

// Use in your handlers
public async ValueTask<Result<OrderDto>> Handle(GetOrderQuery request, CancellationToken ct)
{
    var order = await _db.Orders.FindAsync(request.OrderId, ct);
    return order is not null
        ? Result.Success(new OrderDto(order.Id, order.Total))
        : Result.Failure<OrderDto>("Order not found");
}
```

**Kdy odkazovat:**
- Scaffolding nových projektů (`/dotnet-init`)
- Implementace vzorů pro zpracování chyb
- Nastavení validačního pipeline

---

## Co je nového v .NET 10

**Soubor:** `knowledge/dotnet-whats-new.md`
**Poslední aktualizace:** Únor 2026

### Co je uvnitř

Reference funkcí pro .NET 10 a C# 14:

1. **Jazykové funkce C# 14**
   - Extension members (vlastnosti, statické metody)
   - Klíčové slovo `field` v přístupových metodách vlastností
   - `allows ref struct` v genericích
   - `params` kolekce (nejen pole)
   - Vylepšené pattern matching
   - Částečné vlastnosti a indexery

2. **ASP.NET Core 10**
   - Nativní podpora OpenAPI (`AddOpenApi()`)
   - Vylepšené filtry endpointů
   - Vylepšení vestavěného rate limiting
   - Keyed services v DI

3. **EF Core 10**
   - Komplexní typy (value objekty bez samostatných tabulek)
   - Vylepšená podpora JSON sloupců
   - Výkon hromadného update/delete
   - Vylepšení temporálních tabulek

4. **Vylepšení runtime**
   - Abstrakce `TimeProvider`
   - Vylepšení výkonu kolekcí
   - Rozšíření podpory Native AOT

### Příklad

```csharp
// C# 14: field keyword
public string Name
{
    get => field;
    set => field = value?.Trim() ?? throw new ArgumentNullException(nameof(value));
}

// C# 14: params collections
public void Log(params ReadOnlySpan<string> messages) { }
```

**Kdy odkazovat:**
- Seznámení s novými možnostmi .NET 10
- Modernizace existujícího kódu
- Volba mezi starými a novými vzory

---

## Doporučení balíčků

**Soubor:** `knowledge/package-recommendations.md`
**Poslední aktualizace:** Březen 2026 (.NET 10 / C# 14)

### Co je uvnitř

Katalog prověřených NuGet balíčků s odůvodněním a doporučením „kdy NEPOUŽÍVAT":

**Kategorie:**
1. **Webový framework** — ASP.NET Core (vestavěný)
2. **Mediátor** — Mediator (doporučený), Wolverine, MediatR (komerční)
3. **Validace** — FluentValidation
4. **ORM** — EF Core, Dapper
5. **Cachování** — HybridCache (vestavěný v .NET 10)
6. **Testování** — xUnit, NSubstitute, FluentAssertions, Testcontainers
7. **Messaging** — MassTransit, Wolverine
8. **Serializace** — System.Text.Json (vestavěný)
9. **HTTP klienti** — Refit, RestSharp
10. **Observabilita** — OpenTelemetry, Serilog
11. **Bezpečnost** — IdentityModel, Duende IdentityServer

### Klíčové pravidlo

**Nikdy nespoléhejte na čísla verzí z trénovacích dat.** Vždy používejte:
```bash
# Preferred: Let NuGet resolve latest stable
dotnet add package <name>

# If specifying version: Verify against NuGet.org first
dotnet package search <name>
```

### Příklad záznamu

```markdown
### Mediator (Recommended Default)

- **Package:** `Mediator.Abstractions` + `Mediator.SourceGenerator` (3.x)
- **License:** MIT (free, no commercial restrictions)
- **Rationale:** Source-generated mediator with near-identical API to MediatR.
  No reflection, Native AOT compatible, significantly faster.
- **When NOT to use:** If your app has <5 features and indirection adds
  complexity. If you need message durability — use Wolverine instead.
```

**Kdy odkazovat:**
- Výběr závislostí pro nové projekty
- Hodnocení alternativ k existujícím balíčkům
- Kontroly souladu s licencemi
- Optimalizace výkonu

---

## Migrace z MediatR na Mediator

**Soubor:** `knowledge/mediatr-to-mediator-migration.md`

### Co je uvnitř

Podrobný průvodce migrací z MediatR na Mediator:

1. **Proč migrovat**
   - Licence MIT vs. komerční licence MediatR
   - Generováno ze zdrojového kódu (nulová reflexe)
   - Podobnost API (minimální změny kódu)

2. **Tabulka porovnání API**
   - Porovnání rozhraní vedle sebe
   - Zvýrazněné klíčové rozdíly

3. **Kroky migrace**
   - Náhrada balíčků
   - Změny signatur handlerů (`Task<T>` → `ValueTask<T>`)
   - Aktualizace pipeline behavior
   - Změny registrace DI

4. **Časté úskalí**
   - Zapomenutí změnit návratové typy
   - Signatura `next()` v pipeline behavior
   - Rozdíly ve skenování assemblies

### Příklad

```csharp
// MediatR
public class CreateOrderHandler : IRequestHandler<CreateOrderCommand, OrderResponse>
{
    public async Task<OrderResponse> Handle(CreateOrderCommand request, CancellationToken ct)
    {
        // ...
    }
}

// Mediator (change: Task → ValueTask)
public sealed class CreateOrderHandler : IRequestHandler<CreateOrderCommand, OrderResponse>
{
    public async ValueTask<OrderResponse> Handle(CreateOrderCommand request, CancellationToken ct)
    {
        // ...
    }
}
```

**Kdy odkazovat:**
- Migrace z MediatR
- Požadavky na soulad s licencemi
- Iniciativy optimalizace výkonu

---

## Architectural Decision Records (ADR)

**Adresář:** `knowledge/decisions/`

### Co jsou ADR?

Architectural Decision Records dokumentují **proč** dotnet-claude-kit dělá konkrétní volby. Každý ADR má standardní formát:

- **Status** — Proposed, Accepted, Deprecated, Superseded
- **Context** — Problém, alternativy, kritéria hodnocení
- **Decision** — Zvolený přístup s příklady
- **Consequences** — Pozitivní, negativní, zmírnění

### Dostupné ADR

#### ADR-001: Vertical Slice Architecture as the Default
**Status:** Superseded by ADR-005
**Shrnutí:** VSA byla původní výchozí architektura. Nyní skill `architecture-advisor` doporučuje nejlepší variantu na základě kontextu projektu.

**Klíčové body:**
- VSA snižuje využití kontextového okna (1-2 soubory na funkci vs. 4-6 napříč vrstvami)
- Rychlejší dodávání funkcí (soběstačné složky)
- Méně merge konfliktů (izolované funkce)
- Přirozená shoda s Minimal API

**Kdy se odchýlit:**
- Komplexní domény s bohatou business logikou → Zaveďte doménovou vrstvu
- Modulární monolity → Každý modul interně používá VSA

---

#### ADR-002: Result Pattern Over Exceptions
**Status:** Accepted

**Shrnutí:** Použijte `Result<T>` pro očekávané chyby, výjimky pro neočekávané chyby.

**Odůvodnění:**
- Výjimky jsou drahé (odvíjení zásobníku)
- Result činí cesty selhání explicitními
- Lepší pro zpracování chyb API (mapuje se na ProblemDetails)

**Příklad:**
```csharp
// GOOD — expected failure
public async ValueTask<Result<Order>> GetOrderAsync(Guid id, CancellationToken ct)
{
    var order = await _db.Orders.FindAsync(id, ct);
    return order is not null
        ? Result.Success(order)
        : Result.Failure<Order>("Order not found");
}

// GOOD — unexpected error (still throw)
if (connectionString is null)
    throw new InvalidOperationException("Connection string not configured");
```

---

#### ADR-003: EF Core as Default ORM
**Status:** Accepted

**Shrnutí:** EF Core je výchozí ORM. Dapper pro výkonově kritické dotazy.

**Odůvodnění:**
- First-party podpora od Microsoftu
- Vynikající nástroje (migrace, LINQ)
- Sledování změn pro aktualizace
- Podpora Native AOT v .NET 10

**Kdy použít Dapper:**
- Dotazy náročné na čtení se složitými joiny
- Reportingové/analytické dotazy
- Výkonově kritické cesty (10x rychlejší než EF Core pro čtení)

---

#### ADR-004: HybridCache as Default Caching
**Status:** Accepted

**Shrnutí:** Použijte vestavěný `HybridCache` (.NET 10) místo `IMemoryCache` nebo `IDistributedCache`.

**Odůvodnění:**
- Kombinuje in-memory + distribuované cachování
- Ochrana před stampede (prevence cache avalanche)
- Invalidace na základě tagů
- Vestavěný, žádné extra balíčky

**Příklad:**
```csharp
var product = await _cache.GetOrCreateAsync(
    $"product:{id}",
    async ct => await _db.Products.FindAsync(id, ct),
    tags: ["products"]);
```

---

#### ADR-005: Multi-Architecture Support
**Status:** Accepted

**Shrnutí:** dotnet-claude-kit podporuje VSA, Clean Architecture a DDD. Skill `architecture-advisor` doporučuje nejlepší variantu.

**Rozhodovací matice:**

| Typ projektu | Doporučená architektura |
|--------------|------------------------|
| API náročné na CRUD | Vertical Slice Architecture |
| Střední složitost | Vertical Slice Architecture |
| Bohatá doménová logika | Clean Architecture nebo DDD |
| Modulární monolit | VSA na modul + sdílená Domain |
| Mikroslužby | VSA (každá služba je malá) |

**Doporučení:**
- Začněte s VSA
- Zaveďte doménovou vrstvu, když se zvýší složitost business logiky
- Přejděte na Clean Architecture, pokud jsou potřeba striktní hranice

---

### Šablona ADR

**Soubor:** `knowledge/decisions/template.md`

Tuto šablonu použijte při vytváření nových ADR:

```markdown
# ADR-NNN: [Short Title]

## Status
[Proposed | Accepted | Deprecated | Superseded by ADR-NNN]

## Context
Describe the problem, alternatives, and evaluation criteria.

## Decision
State the chosen approach with code examples.

## Consequences

### Positive
Benefits of this decision.

### Negative
Drawbacks and tradeoffs.

### Mitigations
How negative consequences are addressed.
```

---

## Jak se znalostní dokumenty používají

### Skills
Skills odkazují na znalostní dokumenty pro detailní informace:
- Skill `vertical-slice` → `decisions/001-vsa-default.md`
- Skill `error-handling` → `common-infrastructure.md` (Result pattern)
- Skill `ef-core` → `breaking-changes.md` (změny EF Core 10)

### Agenti
Agenti konzultují znalosti pro rozhodování:
- `dotnet-architect` → `decisions/*.md` (architektonické volby)
- `package-advisor` → `package-recommendations.md` (výběr závislostí)

### Příkazy
Příkazy odkazují na znalosti pro validaci:
- `/verify` → `common-antipatterns.md` (detekce anti-vzorů)
- `/migrate` → `breaking-changes.md` (průvodce upgradem)
- `/security-scan` → `package-recommendations.md` (zranitelné balíčky)

### Hooky
Hooky vynucují pravidla ze znalostí:
- `pre-commit-antipattern.sh` → `common-antipatterns.md`
- `post-edit-format.sh` → `.editorconfig` (pravidla stylů)

---

## Údržba znalostních dokumentů

### Kdy aktualizovat

1. **breaking-changes.md** — Po každém hlavním vydání .NET
2. **package-recommendations.md** — Měsíčně (kontrola nových stabilních vydání)
3. **common-antipatterns.md** — Když se objeví nové vzory
4. **dotnet-whats-new.md** — Po každém vydání .NET
5. **ADR** — Když se změní architektonická rozhodnutí

### Kontrolní seznam aktualizace

- [ ] Aktualizujte datum „Poslední aktualizace"
- [ ] Ověřte, že příklady kódu se zkompilují
- [ ] Zkontrolujte verze balíčků oproti NuGet.org
- [ ] Aktualizujte odkazy na dokumentaci Microsoftu
- [ ] Otestujte průvodce migrací na reálných projektech
- [ ] Aktualizujte související skills/agenty/příkazy

### Pravidla pro přispívání

1. **Faktická přesnost** — Ověřte oproti oficiálním zdrojům
2. **Příklady kódu** — Musí se zkompilovat a dodržovat konvence dotnet-claude-kit
3. **Specifikace verze** — Vždy uveďte verzi .NET
4. **Kdy NEPOUŽÍVAT** — Zahrňte anti-doporučení
5. **Odkazy** — Odkazujte na oficiální dokumentaci, ne na blogové příspěvky

---

## Viz také

- **Skills** — Viz `skills/` pro názorové vzory kódování
- **Příkazy** — Viz `COMMANDS-README.md` pro orchestraci pracovních postupů
- **Hooky** — Viz `HOOKS-README.md` pro automatizované kontroly kvality
- **Agenti** — Viz `agents/` pro specializované AI experty
