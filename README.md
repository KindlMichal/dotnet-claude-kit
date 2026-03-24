<p align="center">
  <h1 align="center">dotnet-claude-kit</h1>
  <p align="center">
    <strong>Claude Code expert na .NET vývoj.</strong>
    <br />
    47 skills &bull; 10 specializovaných agentů &bull; 15 slash příkazů &bull; 10 pravidel &bull; 5 šablon projektů &bull; 15 MCP nástrojů &bull; 7 hooků
    <br />
    Vytvořeno pro .NET 10 / C# 14. S povědomím o architektuře. Efektivní na tokeny.
  </p>
</p>

<p align="center">
  <a href="#instalace">Instalace</a> &bull;
  <a href="#rychlý-start">Rychlý start</a> &bull;
  <a href="#čím-je-to-10x-lepší">10x funkce</a> &bull;
  <a href="#slash-příkazy-16">Příkazy</a> &bull;
  <a href="#skills-47">Skills</a> &bull;
  <a href="#agenti-10">Agenti</a> &bull;
  <a href="#pravidla-10">Pravidla</a> &bull;
  <a href="#šablony-5">Šablony</a> &bull;
  <a href="#roslyn-mcp-server">MCP Server</a> &bull;
  <a href="#přispívání">Přispívání</a>
</p>

---

## Problém

Claude Code je silný nástroj, ale bez dalšího nastavení nezná **vaše** .NET konvence. Generuje `DateTime.Now` místo `TimeProvider`. Obaluje EF Core do zbytečných repository abstrakcí. Zvolí architekturu, aniž by se zeptal na vaši doménu. Čte celé zdrojové soubory, když by Roslyn dotaz stál 10x méně tokenů.

**dotnet-claude-kit tohle všechno řeší.**

## Co to je

Vrstva znalostí a akcí mezi Claude Code a .NET projektem. Stačí jediný `CLAUDE.md` v repo projektu a Claude okamžitě ví:

- Která architektura vyhovuje projektu (VSA, Clean Architecture, DDD, Modular Monolith)
- Jak psát moderní C# 14 s primárními konstruktory, výrazy kolekcí a záznamy (records)
- Jak vytvářet minimal APIs s `IEndpointGroup` auto-discovery, `TypedResults` a správnými OpenAPI metadaty
- Jak používat EF Core bez repository obalů, s kompilovanými dotazy a interceptory
- Jak testovat pomocí `WebApplicationFactory` + `Testcontainers` místo in-memory fakeů
- Jak procházet kódovou základnu přes sémantickou analýzu Roslyn místo drahého čtení souborů
- **Jak scaffoldovat kompletní funkce, spouštět kontroly zdraví projektu, reviewovat PR a vynucovat konvence**

**Žádná konfigurace. Žádní průvodci nastavením. Stačí zkopírovat jeden soubor a jet.**

## Čím je to 10x lepší

Nástroj přidává **akční vrstvu** nad znalostní vrstvu — Claude nejen zná správné vzory, ale aktivně je aplikuje:

| Schopnost | Co dělá |
|-----------|---------|
| **Scaffolding** | Jeden příkaz → kompletní funkce s Result patternem, validací (FluentValidation + propojení filtrů), OpenAPI metadaty, stránkováním, CancellationToken a testy. Vynucený 9bodový checklist. Všechny 4 architektury. |
| **Interaktivní nastavení** | Řízená inicializace projektu: dotazník architektury → výběr tech stacku → vygenerování přizpůsobeného `CLAUDE.md`. |
| **Kontrola zdraví** | Automatizovaná analýza kódové základny pomocí MCP nástrojů: sken anti-vzorů, diagnostika, detekce mrtvého kódu, pokrytí testy → hodnocení známkami. |
| **PR Review** | Vícerozměrné code review: anti-vzory, diagnostika, změny API rozhraní, dopadový rozsah, soulad s architekturou, pokrytí testy. |
| **Učení konvencí** | Detekuje vzory specifické pro projekt (pojmenování, struktura, modifikátory) a vynucuje je v novém kódu. Přizpůsobuje se vaší kódové základně. |
| **Chytré nástroje** | 15 MCP nástrojů poháněných Roslynem včetně grafů závislostí, detekce kruhových závislostí, hledání mrtvého kódu a mapování pokrytí testy. |
| **Aktivní hooky** | 6 hooků pro automatizovanou kvalitu: formátování při editaci, kontrola anti-vzorů při commitu, analýza výsledků testů, validace struktury. |

## Proč dotnet-claude-kit?

| Metrika | Bez kitu | S kitem | Dopad |
|---------|----------|---------|-------|
| **Rozhodování o architektuře** | Claude volí náhodně | Ptá se, doporučuje s odůvodněním | Správná architektura od prvního dne |
| **Kvalita kódu** | Generický C#, zastaralé vzory | Moderní C# 14 s idiomatickým .NET 10 | Nulové revizní cykly typu „oprav tento vzor" |
| **Navigace v kódu** | Čte celé soubory (500–2000+ tokenů každý) | Roslyn MCP dotazy (30–150 tokenů každý) | **~10x úspora tokenů** při průzkumu |
| **Generované anti-vzory** | `DateTime.Now`, repository nad EF, `new HttpClient()` | `TimeProvider`, přímý DbContext, `IHttpClientFactory` | Produkční kvalita na první generování |
| **Přístup k testování** | In-memory faky, mockování všeho | `WebApplicationFactory` + `Testcontainers` | Testy, které chytí skutečné chyby |
| **Odolnost v produkci** | Žádné retry, žádné circuit breakery | Polly v8 pipelines s telemetrií | Automatické zvládání přechodných selhání |

**Výsledek**: Méně času na review a opravování výstupu Claude. Více času na dodávání funkcí.

## Instalace

### Instalace pluginu (doporučeno)

Nainstalujte jako Claude Code plugin — všech 47 skills, 10 agentů, 16 příkazů, 10 pravidel, hooky a MCP konfigurace se aktivují globálně:

```bash
# V terminálu — nainstalovat Roslyn MCP server
dotnet tool install -g CWM.RoslynNavigator
```

Poté uvnitř Claude Code relace:

**Pro lokální vývoj/testování** (načítá přímo z disku, bez instalace):

```bash
claude --plugin-dir /path/to/dotnet-claude-kit
```

### Nastavení pro konkrétní projekt

Začátek projektu:

```bash
/dotnet-init
```

**Existující projekt?** Detekuje  solution, prohledá .csproj SDK, přečte tech stack z konfigurace, položí otázky o architektuře a vygeneruje přizpůsobený `CLAUDE.md`.

**Nový projekt od nuly?** Zeptá se na typ projektu a vytvoří celou strukturu solution (`dotnet new sln`, projekty, `Directory.Build.props`, složky `src/` a `tests/`), poté vygeneruje `CLAUDE.md`. Příkaz `/scaffold` je další krok pro přidání první funkce.

Není potřeba ruční kopírování šablon.

<details>
<summary><strong>Ruční kopírování šablony (alternativa)</strong></summary>

Pro ruční nastavení stačí zkopírovat šablonu odpovídající typu projektu:

```bash
cp templates/web-api/CLAUDE.md ./CLAUDE.md           # REST API
cp templates/modular-monolith/CLAUDE.md ./CLAUDE.md   # Multi-modulový systém
cp templates/blazor-app/CLAUDE.md ./CLAUDE.md          # Blazor aplikace
cp templates/worker-service/CLAUDE.md ./CLAUDE.md      # Background workers
cp templates/class-library/CLAUDE.md ./CLAUDE.md       # NuGet balíčky
```

Nahradit `[ProjectName]`, aktualizovat tech stack, vybrat architekturu.

</details>

Po spuštění Claude Code — 47 skills, 10 agentů, 16 příkazů, 10 pravidel a 15 MCP nástrojů se aktivuje automaticky.

To je vše. Claude nyní píše .NET kód tak, jak by to dělal zkušený .NET inženýr.

<details>
<summary><strong>Ruční instalace (alternativa)</strong></summary>

Pokud preferujete naklonovat repozitář a propojit vše ručně ta, že se:

```bash
# 1. Nainstaluje MCP server globálně
dotnet tool install -g CWM.RoslynNavigator

# 2. Zaregistruje v Claude Code na úrovni uživatele (dostupný ve VŠECH projektech)
claude mcp add --scope user cwm-roslyn-navigator -- cwm-roslyn-navigator --solution ${workspaceFolder}

# 3. Naklonuje kit
git clone https://github.com/codewithmukesh/dotnet-claude-kit.git

# 4. Načte jako lokální plugin (nebo zkopíruje šablona ručně)
claude --plugin-dir ./dotnet-claude-kit
```

</details>

## Co to přinese

### Před dotnet-claude-kit

```csharp
// Claude vygeneruje toto
public class OrderService
{
    private readonly IOrderRepository _repo;  // zbytečná abstrakce nad EF Core

    public async Task<Order> CreateOrder(CreateOrderDto dto)
    {
        var order = new Order();
        order.CreatedAt = DateTime.Now;  // špatně — použijte TimeProvider
        order.Items = dto.Items.ToList();
        await _repo.AddAsync(order);
        return order;  // únik doménové entity do API
    }
}
```

### Po dotnet-claude-kit

```csharp
// Claude vygeneruje toto
public static class CreateOrder
{
    public record Command(string CustomerId, List<OrderItemDto> Items) : IRequest<Result<Response>>;
    public record Response(Guid Id, decimal Total, DateTimeOffset CreatedAt);

    internal sealed class Handler(AppDbContext db, TimeProvider clock)
        : IRequestHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken ct)
        {
            var order = Order.Create(request.CustomerId, request.Items, clock.GetUtcNow());
            db.Orders.Add(order);
            await db.SaveChangesAsync(ct);
            return Result.Success(new Response(order.Id, order.Total, order.CreatedAt));
        }
    }
}
```

```csharp
// Každá skupina endpointů je auto-discovered — Program.cs se nikdy nemění
public sealed class OrderEndpoints : IEndpointGroup
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/orders").WithTags("Orders");
        group.MapPost("/", CreateOrderHandler)
            .WithName("CreateOrder").Produces<CreateOrder.Response>(201)
            .ProducesValidationProblem()
            .AddEndpointFilter<ValidationFilter<CreateOrder.Command>>();
    }
}
```

**Result pattern. FluentValidation s endpoint filtry. IEndpointGroup auto-discovery. TypedResults s OpenAPI metadaty. CancellationToken všude. Zapečetěné handlery. TimeProvider injekce. DbContext přímo.** Každý vzor pochází ze skills v tomto kitu.

---

## Slash příkazy (16)

Zkratkové workflow, které orchestrují skills a agenty. Zadejte příkaz a Claude se postará o zbytek.

| Příkaz | Účel | Vyvolává |
|--------|------|----------|
| `/dotnet-init` | Nastavení projektu (existující nebo nový) — detekuje nebo scaffolduje, poté generuje CLAUDE.md | project-setup skill, dotnet-architect agent |
| `/plan` | Plánování s povědomím o architektuře pro netriviální úlohy | architecture-advisor skill, dotnet-architect agent |
| `/verify` | 7fázová verifikace: build → analyzátory → anti-vzory → testy → bezpečnost → formátování → diff | verification-loop skill |
| `/tdd` | Red-green-refactor s xUnit + Testcontainers | testing skill, test-engineer agent |
| `/scaffold` | Scaffolding funkcí s povědomím o architektuře (všechny 4 architektury) | scaffolding skill, dotnet-architect agent |
| `/code-review` | Vícerozměrné code review poháněné MCP | code-review-workflow skill, code-reviewer agent |
| `/build-fix` | Autonomní oprava build chyb (iterativní smyčka) | autonomous-loops skill, build-error-resolver agent |
| `/checkpoint` | Uložení průběhu: commit + poznámka k předání | wrap-up-ritual skill |
| `/security-scan` | OWASP + secrets + audit zranitelných závislostí | security-scan skill, security-auditor agent |
| `/migrate` | Bezpečný EF Core migrační workflow | migration-workflow skill, ef-core-specialist agent |
| `/health-check` | Hodnocení zdraví projektu písmennými známkami (A–F) | health-check skill, code-reviewer agent |
| `/de-sloppify` | Systematický úklid: formátování → mrtvý kód → analyzátory → sealed | de-sloppify skill, refactor-cleaner agent |
| `/wrap-up` | Rituál ukončení relace s poznámkou k předání | wrap-up-ritual skill |
| `/instinct-status` | Zobrazení naučených instinktů se skóre spolehlivosti | instinct-system skill |
| `/instinct-export` | Export instinktů do sdílitelného formátu | instinct-system skill |
| `/instinct-import` | Import instinktů z jiného projektu | instinct-system skill |

## Pravidla (10)

Vždy načtené konvence, které se aplikují na každou interakci. Nulová konfigurace — jsou aktivní ihned po instalaci pluginu.

| Pravidlo | Vynucuje |
|----------|----------|
| [coding-style](.claude/rules/coding-style.md) | Konvence C# 14, file-scoped namespaces, primární konstruktory, sealed, records |
| [architecture](.claude/rules/architecture.md) | Ptát se před doporučením, žádný repository nad EF, feature složky, směr závislostí |
| [security](.claude/rules/security.md) | Žádná natvrdo zapsaná tajemství, parametrizované dotazy, explicitní autorizace, HTTPS |
| [testing](.claude/rules/testing.md) | Integrace na prvním místě, WebApplicationFactory + Testcontainers, AAA vzor |
| [performance](.claude/rules/performance.md) | Propagace CancellationToken, TimeProvider, IHttpClientFactory, HybridCache |
| [error-handling](.claude/rules/error-handling.md) | Result pattern, ProblemDetails, žádný široký catch, validace na hranicích |
| [git-workflow](.claude/rules/git-workflow.md) | Conventional commits, atomické commity, nikdy force-push na main |
| [agents](.claude/rules/agents.md) | MCP-first, směrování subagentů, pořadí načítání skills |
| [hooks](.claude/rules/hooks.md) | Automatické přijímání formátování, nikdy přeskakovat pre-commit hooky |
| [packages](.claude/rules/packages.md) | Vždy používat nejnovější stabilní verze NuGet, nikdy se spoléhat na verze z trénovacích dat |

## Skills (47)

Soubory s referenčním kódem, které učí Claude osvědčené postupy .NET. Každý skill má méně než 400 řádků s konkrétními příklady kódu, anti-vzory (porovnání ŠPATNĚ/DOBŘE) a rozhodovacími průvodci.

| Kategorie | Skills | Co se Claude naučí |
|-----------|--------|--------------------|
| **Architektura** | [architecture-advisor](skills/architecture-advisor/SKILL.md), [vertical-slice](skills/vertical-slice/SKILL.md), [clean-architecture](skills/clean-architecture/SKILL.md), [ddd](skills/ddd/SKILL.md), [project-structure](skills/project-structure/SKILL.md) | Ptát se před doporučením. VSA pro CRUD, CA pro střední složitost, DDD pro bohaté domény, Modular Monolith pro bounded contexts. |
| **Jádro jazyka** | [modern-csharp](skills/modern-csharp/SKILL.md) | Primární konstruktory, výrazy kolekcí, klíčové slovo `field`, records, pattern matching, spans |
| **Web / API** | [minimal-api](skills/minimal-api/SKILL.md), [api-versioning](skills/api-versioning/SKILL.md), [authentication](skills/authentication/SKILL.md) | `MapGroup`, `TypedResults`, endpoint filtry, JWT/OIDC, Asp.Versioning |
| **Data** | [ef-core](skills/ef-core/SKILL.md) | Žádné repository obaly. Kompilované dotazy, interceptory, `ExecuteUpdateAsync`, value convertery |
| **Odolnost** | [error-handling](skills/error-handling/SKILL.md), [resilience](skills/resilience/SKILL.md), [caching](skills/caching/SKILL.md), [messaging](skills/messaging/SKILL.md) | Result pattern, Polly v8 pipelines, `HybridCache`, Wolverine/MassTransit, outbox, ságy |
| **Pozorovatelnost** | [logging](skills/logging/SKILL.md) | Serilog strukturované logování, OpenTelemetry, korelační ID |
| **Testování** | [testing](skills/testing/SKILL.md) | xUnit v3, `WebApplicationFactory`, `Testcontainers`, Verify snapshoty |
| **DevOps** | [docker](skills/docker/SKILL.md), [ci-cd](skills/ci-cd/SKILL.md), [aspire](skills/aspire/SKILL.md) | Multi-stage buildy, GitHub Actions, .NET Aspire orchestrace |
| **Průřezové** | [dependency-injection](skills/dependency-injection/SKILL.md), [configuration](skills/configuration/SKILL.md) | Keyed services, Options pattern, správa tajemství |
| **Workflow** | [workflow-mastery](skills/workflow-mastery/SKILL.md) | Paralelní worktrees, strategie plan mode, verifikační smyčky, auto-format hooky, nastavení oprávnění, vzory subagentů |
| **Workflow a automatizace** | [scaffolding](skills/scaffolding/SKILL.md), [project-setup](skills/project-setup/SKILL.md), [code-review-workflow](skills/code-review-workflow/SKILL.md), [migration-workflow](skills/migration-workflow/SKILL.md), [convention-learner](skills/convention-learner/SKILL.md) | Scaffolding funkcí pro všechny architektury, interaktivní init projektu, MCP-řízené PR review, bezpečné migrační workflow, detekce a vynucování konvencí |
| **Verifikace a kvalita** | [verification-loop](skills/verification-loop/SKILL.md), [de-sloppify](skills/de-sloppify/SKILL.md), [health-check](skills/health-check/SKILL.md), [security-scan](skills/security-scan/SKILL.md) | 7fázová verifikační pipeline, systematický úklid, hodnocení zdraví známkami, hloubkové bezpečnostní skenování |
| **Inteligence a učení** | [instinct-system](skills/instinct-system/SKILL.md), [session-management](skills/session-management/SKILL.md), [autonomous-loops](skills/autonomous-loops/SKILL.md) | Učení vzorů se skóre spolehlivosti, kontinuita relací, ohraničené iterativní opravné smyčky |
| **Meta a produktivita** | [self-correction-loop](skills/self-correction-loop/SKILL.md), [wrap-up-ritual](skills/wrap-up-ritual/SKILL.md), [context-discipline](skills/context-discipline/SKILL.md), [model-selection](skills/model-selection/SKILL.md), [80-20-review](skills/80-20-review/SKILL.md), [split-memory](skills/split-memory/SKILL.md), [learning-log](skills/learning-log/SKILL.md) | Sebezdokonalující zachycení korekcí, strukturované předávání relací, správa tokenového rozpočtu, strategický výběr modelu, zaměřené code review, modulární CLAUDE.md, dokumentace poznatků |

## Agenti (10)

Specializovaní agenti, na které Claude automaticky směruje dotazy. Každý agent načítá správné skills, používá MCP nástroje pro kontext a zná své hranice.

| Agent | Kdy se aktivuje | Co dělá |
|-------|-----------------|---------|
| [dotnet-architect](agents/dotnet-architect.md) | „nastav projekt", „architektura", „scaffold funkce", „init projekt" | Spouští dotazník architektury, scaffolduje funkce, inicializuje projekty |
| [api-designer](agents/api-designer.md) | „vytvoř endpoint", „OpenAPI", „verzování" | Navrhuje minimal API endpointy se správnými metadaty, verzováním a autorizací |
| [ef-core-specialist](agents/ef-core-specialist.md) | „databáze", „migrace", „dotaz", „DbContext" | Optimalizuje dotazy, konfiguruje entity, bezpečně spravuje migrace |
| [test-engineer](agents/test-engineer.md) | „napiš testy", „testovací strategie", „pokrytí" | Integrace na prvním místě s reálnými databázemi přes Testcontainers |
| [security-auditor](agents/security-auditor.md) | „bezpečnost", „autentizace", „JWT" | OWASP top 10, konfigurace autorizace, správa tajemství |
| [performance-analyst](agents/performance-analyst.md) | „výkon", „benchmark", „cachování" | Identifikuje horká místa, konfiguruje HybridCache, optimalizace async |
| [devops-engineer](agents/devops-engineer.md) | „Docker", „CI/CD", „Aspire", „nasazení" | Multi-stage Dockerfiles, GitHub Actions pipelines, Aspire orchestrace |
| [code-reviewer](agents/code-reviewer.md) | „zkontroluj kód", „PR review", „kontrola zdraví", „konvence" | MCP-řízené vícerozměrné review, detekce a vynucování konvencí |
| [build-error-resolver](agents/build-error-resolver.md) | „oprav build", „chyby buildu", „nekompiluje se" | Autonomní smyčka opravy buildu: parsování chyb → kategorizace → oprava → rebuild |
| [refactor-cleaner](agents/refactor-cleaner.md) | „uklidit", „mrtvý kód", „de-sloppify" | Systematický úklid: odstranění mrtvého kódu, formátování, sealed, CancellationToken |

## Šablony (5)

Připravené `CLAUDE.md` soubory, které konfigurují Claude pro konkrétní typy projektů. Zkopírujte jeden soubor, nahraďte zástupné texty, hotovo.

| Šablona | Pro | Zahrnuje |
|---------|-----|----------|
| [web-api](templates/web-api/) | REST API, mikroservisy | Možnosti architektury (VSA/CA/DDD), minimal APIs, EF Core, testování |
| [modular-monolith](templates/modular-monolith/) | Multi-modulové systémy | Hranice modulů, per-modul DbContext, Wolverine/MassTransit integrační události |
| [blazor-app](templates/blazor-app/) | Blazor Server / WASM / Auto | Organizace komponent, strategie render mode, bUnit testování |
| [worker-service](templates/worker-service/) | Background zpracování | Vzory BackgroundService, Wolverine/MassTransit consumery, správná cancellation |
| [class-library](templates/class-library/) | NuGet balíčky, sdílené knihovny | Design veřejného API, XML dokumentace, sémantické verzování, SourceLink |

## Roslyn MCP Server

Tokenově efektivní navigace kódovou základnou přes sémantickou analýzu Roslyn. Místo toho, aby Claude četl celé zdrojové soubory (500–2000+ tokenů každý), dotazuje MCP server přesně na to, co potřebuje (30–150 tokenů).

| Nástroj | Co dělá | Nahrazuje |
|---------|---------|-----------|
| `find_symbol` | Lokalizace definic typů/metod | Grep/Glob přes všechny .cs soubory |
| `find_references` | Nalezení všech použití symbolu | Grep názvu typu |
| `find_implementations` | Nalezení implementátorů rozhraní | Hledání `: IInterface` |
| `find_callers` | Nalezení všech metod volajících metodu | Ruční grep názvu metody |
| `find_overrides` | Nalezení přepsání virtual/abstract metod | Hledání klíčového slova `override` |
| `get_type_hierarchy` | Řetězec dědičnosti + rozhraní | Čtení více souborů |
| `get_project_graph` | Strom závislostí solution | Ruční parsování .csproj souborů |
| `get_public_api` | Veřejné API bez celého souboru | Čtení celých zdrojových souborů |
| `get_symbol_detail` | Plná signatura, parametry, XML dokumentace | Čtení celých zdrojových souborů |
| `get_diagnostics` | Varování/chyby kompilátoru | Spuštění `dotnet build` a parsování |
| `detect_antipatterns` | 10 pravidel .NET anti-vzorů | Ruční code review |
| `find_dead_code` | Nepoužívané typy, metody, vlastnosti | Ruční kontrola všech souborů |
| `detect_circular_dependencies` | Cykly na úrovni projektů a typů | Ruční trasování referencí |
| `get_dependency_graph` | Vizualizace řetězce volání metod | Čtení více souborů a trasování |
| `get_test_coverage_map` | Heuristické mapování pokrytí testy | Ruční hledání testových souborů |

MCP server se spouští automaticky přes `.mcp.json`. Není potřeba ruční nastavení.

Viz [mcp/CWM.RoslynNavigator/README.md](mcp/CWM.RoslynNavigator/README.md) pro podrobnosti.

## Znalostní báze

Živé referenční dokumenty aktualizované s každým vydáním .NET:

| Dokument | Účel |
|----------|------|
| [dotnet-whats-new](knowledge/dotnet-whats-new.md) | Funkce .NET 10 / C# 14 a jak je používat |
| [common-antipatterns](knowledge/common-antipatterns.md) | Vzory, které by Claude nikdy neměl generovat |
| [package-recommendations](knowledge/package-recommendations.md) | Prověřené NuGet balíčky s odůvodněním a „kdy NEPOUŽÍVAT" |
| [breaking-changes](knowledge/breaking-changes.md) | Úskalí migrace .NET |
| [decisions/](knowledge/decisions/) | Záznamy rozhodnutí o architektuře vysvětlující každý výchozí stav |

## Hooky (7)

Automatizovaná integrace workflow:

| Hook | Událost | Co dělá |
|------|---------|---------|
| `pre-bash-guard.sh` | PreToolUse (Bash) | Blokuje destruktivní git operace (force push, reset --hard), varuje u rizikových příkazů |
| `pre-commit-format.sh` | Pre-commit | `dotnet format --verify-no-changes` zajišťuje konzistentní formátování |
| `pre-commit-antipattern.sh` | Pre-commit | Detekuje DateTime.Now, async void, new HttpClient() ve stagovaných souborech |
| `post-scaffold-restore.sh` | Post-file-edit (*.csproj) | `dotnet restore` po změnách projektového souboru |
| `post-edit-format.sh` | Post-file-edit (*.cs) | Automatické formátování C# souborů po editaci |
| `post-test-analyze.sh` | Post-test | Parsuje výsledky testů a výstupuje akční shrnutí |
| `pre-build-validate.sh` | Pre-build | Validuje strukturu projektu (solution soubor, Directory.Build.props, testovací projekty) |

## Výchozí hodnoty a rozhodnutí

Každý výchozí stav je dokumentován ADR vysvětlujícím **proč**:

| Rozhodnutí | Výchozí | Proč |
|------------|---------|------|
| Architektura | Řízená poradcem | Nejdřív klade otázky, poté doporučí VSA, CA, DDD nebo Modular Monolith ([ADR-005](knowledge/decisions/005-multi-architecture.md)) |
| Zpracování chyb | Result pattern | Výjimky jsou pro výjimečné případy ([ADR-002](knowledge/decisions/002-result-over-exceptions.md)) |
| ORM | EF Core | Nejlepší vývojářská zkušenost pro většinu scénářů ([ADR-003](knowledge/decisions/003-ef-core-default-orm.md)) |
| Cachování | HybridCache | Vestavěná ochrana proti stampede, L1+L2 ([ADR-004](knowledge/decisions/004-hybrid-cache-default.md)) |
| API | Minimal APIs | Lehčí, kompozitní, nezávislé na architektuře |
| Testování | Integrace na prvním místě | `WebApplicationFactory` + `Testcontainers` místo in-memory fakeů |
| Čas | `TimeProvider` | Testovatelné, injektovatelné, konec s `DateTime.Now` |
| HTTP klienti | `IHttpClientFactory` | Konec s `new HttpClient()` a vyčerpáním socketů |

## Struktura repozitáře

```
dotnet-claude-kit/
├── CLAUDE.md                    # Instrukce pro vývoj TOHOTO repozitáře
├── AGENTS.md                    # Směrování a orchestrace agentů
├── agents/                      # 10 specializovaných agentů
├── skills/                      # 47 skills
├── commands/                    # 15 slash příkazů
├── .claude/rules/               # 10 vždy načtených pravidel
├── templates/                   # 5 připravených CLAUDE.md šablon
├── knowledge/                   # Živé referenční dokumenty + ADR
├── mcp/CWM.RoslynNavigator/     # Roslyn MCP server (15 nástrojů)
├── mcp-configs/                 # Šablony konfigurace MCP serverů
├── hooks/                       # 7 Claude Code hooků
├── docs/                        # Zkrácené + podrobné průvodce
├── .mcp.json                    # Registrace MCP serveru
├── .claude-plugin/              # Manifesty pro plugin marketplace
├── .cursor/rules/               # Kompatibilita s Cursor IDE
├── .codex/                      # Kompatibilita s Codex CLI
└── .github/workflows/           # CI validace
```

## Podpora více platforem

dotnet-claude-kit funguje s více nástroji pro AI kódování:

| Platforma | Konfigurační soubor | Co poskytuje |
|-----------|---------------------|--------------|
| **Claude Code** | `.claude-plugin/plugin.json` | Plná integrace: skills, agenti, příkazy, pravidla, hooky, MCP |
| **Cursor** | `.cursor/rules/dotnet-rules.md` | Konsolidovaná .NET pravidla pro Cursor IDE |
| **Codex CLI** | `.codex/AGENTS.md` | Konfigurace agentů odkazující na skills a agenty |

