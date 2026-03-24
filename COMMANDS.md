# Referenční příručka příkazů dotnet-claude-kit

> Slash příkazy pro orchestraci .NET vývojových workflow s Claude Code.

## Co jsou příkazy?

Příkazy jsou odlehčené orchestrátory workflow, které volají skills a agenty k provádění složitých úkolů. Každý příkaz se řídí konzistentním vzorem: detekovat kontext, zavolat správné nástroje, vytvořit strukturovaný výstup a ověřit výsledek.

**Použití:** Zadejte `/command-name` v Claude Code pro vyvolání příkazu.

## Rychlý přehled

| Příkaz | Kategorie | Účel | Kdy použít |
|--------|-----------|------|------------|
| `/dotnet-init` | Nastavení | Interaktivní inicializace projektu | Zakládání nového projektu nebo přidání dotnet-claude-kit |
| `/plan` | Plánování | Plánování s ohledem na architekturu | Před implementací netriviálních funkcí (3+ kroků) |
| `/scaffold` | Vývoj | Generování kompletních funkcí | Vytváření nových endpointů, entit nebo modulů |
| `/tdd` | Vývoj | Workflow řízeného vývoje testy | Budování funkcí s jasnými akceptačními kritérii |
| `/verify` | Kvalita | 7fázový ověřovací pipeline | Před PR, po dokončení funkce |
| `/build-fix` | Kvalita | Autonomní oprava chyb buildu | Když je build rozbitý, po refaktoringu nebo aktualizaci balíčků |
| `/code-review` | Kvalita | Revize kódu pomocí MCP | Revize PR, hodnocení kvality |
| `/health-check` | Kvalita | Hodnocení zdraví projektu | Pravidelný audit, stanovení základního stavu |
| `/de-sloppify` | Kvalita | Systematické čištění kódu | Před PR, po sprintu, pravidelná hygiena |
| `/security-scan` | Bezpečnost | Hloubkový bezpečnostní audit | Před produkčním nasazením, po implementaci autentizace/plateb |
| `/migrate` | Databáze | Řízená migrace EF Core | Po změnách doménového modelu |
| `/checkpoint` | Relace | Uložení postupu (commit + předání) | Zachycení stavu v průběhu relace, před rizikovými změnami |
| `/wrap-up` | Relace | Rituál na konci relace | Konec dne, přepínání projektů |
| `/instinct-status` | Učení | Zobrazení naučených vzorů | Přehled znalostí projektu |
| `/instinct-export` | Učení | Export instinktů | Sdílení vzorů s jinými projekty |
| `/instinct-import` | Učení | Import instinktů | Zavedení nového projektu s existujícími poznatky |

## Příkazy podle kategorií

### 🚀 Nastavení projektu a plánování

#### `/dotnet-init`
**Co:** Interaktivní inicializace projektu, která detekuje typ projektu, klade otázky o architektuře a generuje přizpůsobený `CLAUDE.md`.

**Kdy:**
- Zakládání nového .NET projektu
- Přidání dotnet-claude-kit do existujícího projektu
- "init project", "setup project", "generate CLAUDE.md"

**Příklad:**
```
User: /dotnet-init

Claude: Detected:
  - Solution: MyApp.slnx (3 projects)
  - SDK: Microsoft.NET.Sdk.Web → Web API

  1. Domain complexity? [CRUD-heavy / Moderate / Rich domain]
  > Moderate

  Recommendation: Vertical Slice Architecture
  Generated: ./CLAUDE.md (MyApp + VSA + PostgreSQL + JWT)
```

**Související:** `/plan`, `/health-check`, `/scaffold`

---

#### `/plan`
**Co:** Plánování s ohledem na architekturu, které mapuje úkoly na dotčené vrstvy a vytváří číslované implementační plány.

**Kdy:**
- Netriviální úkoly (3+ kroků)
- Funkce zasahující do více vrstev
- Architektonická rozhodnutí
- "plan", "think through", "design this"

**Příklad:**
```
User: /plan Add product catalog with search

Claude: ## Plan: Product Catalog Feature
**Architecture:** Vertical Slice Architecture
**Estimated steps:** 6

### Steps
1. Create Product entity + EF config
2. Add migration
3. Scaffold GetProducts feature (with filtering)
4. Scaffold CreateProduct feature
5. Add integration tests
6. Wire up OpenAPI metadata
```

**Související:** `/scaffold`, `/verify`

---

#### `/scaffold`
**Co:** Scaffolding funkcí s ohledem na architekturu, které generuje kompletní funkce se všemi potřebnými vrstvami.

**Kdy:**
- "scaffold", "create feature", "add feature"
- Po schválení `/plan`
- Zahájení nových endpointů nebo modulů

**Příklad:**
```
User: /scaffold a Product Catalog feature with CRUD

Claude: Detected architecture: Vertical Slice Architecture

Created files:
  src/Features/Products/CreateProduct.cs
  src/Features/Products/GetProduct.cs
  src/Features/Products/ProductConfig.cs
  tests/Features/Products/CreateProductTests.cs

Build check: PASS
```

**Související:** `/plan`, `/tdd`

---

### 💻 Vývojový workflow

#### `/tdd`
**Co:** Řízený vývoj řízený testy podle striktních cyklů red-green-refactor.

**Kdy:**
- "TDD", "test-driven", "write the test first"
- Budování funkcí s jasnými akceptačními kritérii
- Oprava chyb (nejprve reprodukovat, pak opravit)

**Příklad:**
```
User: /tdd product search endpoint

Claude: Planning TDD cycles:
Cycle 1: Search with matching term returns products
Cycle 2: Search with no matches returns empty list
Cycle 3: Search with pagination

Starting Cycle 1: Red phase
Writing failing test...
[test fails]

Green phase: Implementing minimum code...
[test passes]

Refactor phase: Improving query projection...
[all tests pass]
```

**Související:** `/verify`, `/scaffold`

---

#### `/verify`
**Co:** 7fázový ověřovací pipeline (build, diagnostika, antivzory, testy, bezpečnost, formátování, revize diffu).

**Kdy:**
- Po dokončení funkce
- Před vytvořením PR
- Po rozsáhlém refaktoringu
- "verify", "check this", "is this ready"

**Příklad:**
```
User: /verify

Claude: Running 7-phase verification...

Phase 1: Build ............ PASS
Phase 2: Diagnostics ...... PASS
Phase 3: Antipatterns ..... WARN (1 DateTime.Now usage)
Phase 4: Tests ............ PASS (23 passed)
Phase 5: Security ......... PASS
Phase 6: Format ........... PASS
Phase 7: Diff Review ...... PASS

Verdict: READY FOR REVIEW (1 non-blocking warning)
```

**Související:** `/build-fix`, `/de-sloppify`, `/security-scan`

---

#### `/build-fix`
**Co:** Autonomní smyčka oprav buildu, která iterativně řeší chyby kompilátoru.

**Kdy:**
- Build je rozbitý
- Po rozsáhlém refaktoringu
- Po aktualizaci balíčků
- "fix the build", "make it compile"

**Příklad:**
```
User: /build-fix

Claude: Found 12 errors across 4 files.

Iteration 1/5:
- 6 errors: Missing namespace (CS0246) -- Adding using directives
- 3 errors: Type mismatch (CS1503)
Rebuilding... 3 errors remaining.

Iteration 2/5:
- 3 errors: Missing member (CS0117) in MassTransit 9.0
  Publish renamed to Send
Rebuilding... 0 errors. Build is green.
```

**Související:** `/verify`, `/de-sloppify`

---

### 🔍 Kvalita kódu a revize

#### `/code-review`
**Co:** Vícerozměrná revize kódu pomocí MCP s využitím analytických nástrojů Roslyn.

**Kdy:**
- "review", "PR review", "code review"
- Před sloučením pull requestů
- Po rozsáhlém refaktoringu
- Pravidelné kontroly kritických modulů

**Příklad:**
```
User: /code-review the changes in this PR

Claude: ## Code Review: Order Processing Feature

### Summary
Well-structured VSA feature. Two issues need attention.

### Critical
- **Missing CancellationToken** -- CreateOrder.cs:38
  Pass ct to SaveChangesAsync()

### Warnings
- **N+1 query** -- GetOrder.cs:25
  Add .Include(o => o.Items) or use projection

### Architecture Compliance
PASS -- All changes within Features/Orders/

### Test Coverage
Two integration tests. Consider validation failure tests.
```

**Související:** `/verify`, `/health-check`

---

#### `/health-check`
**Co:** Vícerozměrné hodnocení zdraví projektu s písmennými známkami (A-F).

**Kdy:**
- Začátek práce na novém kódu (stanovení základního stavu)
- Pravidelná kontrola kvality (měsíčně/za sprint)
- Před velkými iniciativami
- "health check", "quality report"

**Příklad:**
```
User: /health-check

Claude: ## Project Health Report

| Dimension                | Grade | Score |
|--------------------------|-------|-------|
| Build & Compiler Health  | A     | 95    |
| Anti-Pattern Density     | B     | 85    |
| Architecture Compliance  | A     | 95    |
| Test Coverage            | C     | 75    |
| Dead Code                | B     | 85    |
| Type Dependency Health   | A     | 95    |

Overall: B (88/100)

Top 3 Actions:
1. Add tests for PaymentHandler (C -> B)
2. Replace 2x DateTime.Now with TimeProvider
3. Remove 4 unused methods
```

**Související:** `/verify`, `/de-sloppify`, `/code-review`

---

#### `/de-sloppify`
**Co:** Systematický pipeline pro čištění kódu (formátování, nepoužívané using direktivy, varování analyzátorů, mrtvý kód, TODO).

**Kdy:**
- Před otevřením PR
- Po sprintu plném vývoje funkcí
- "clean this up", "tidy the code"
- Pravidelný úklid

**Příklad:**
```
User: /de-sloppify

Claude: Starting cleanup pipeline...

Phase 1/7: Formatting -- Fixed 12 files
Phase 2/7: Unused Usings -- Removed 8 directives
Phase 3/7: Analyzer Warnings -- Resolved 8 warnings
Phase 4/7: Dead Code -- Found 2 unreferenced methods
Phase 5/7: TODOs -- 3 remaining (listed)
Phase 6/7: Sealed Classes -- Sealed 4 classes
Phase 7/7: CancellationToken -- Added to 2 methods

Summary: 23 files modified, warnings 14->6, all tests passing.
```

**Související:** `/verify`, `/health-check`

---

### 🔒 Bezpečnost

#### `/security-scan`
**Co:** Hloubkový bezpečnostní audit pokrývající OWASP Top 10, tajné klíče, zranitelné balíčky, konfiguraci autentizace, CORS.

**Kdy:**
- Před produkčním nasazením
- Po přidání funkcí autentizace/plateb
- Bezpečnostní revize nebo audit shody
- "security scan", "check for vulnerabilities"

**Příklad:**
```
User: /security-scan

Claude: Running security scan...

Phase 1: Vulnerable Dependencies
- CRITICAL: Newtonsoft.Json 12.0.3 has CVE-2024-XXXX

Phase 2: Secrets Detection
- HIGH: Hardcoded connection string in appsettings.Development.json:8

Phase 3: OWASP Top 10
- HIGH: FromSqlRaw with string concatenation (SearchService.cs:34)
- MEDIUM: No rate limiting on auth endpoints

Phase 4: Auth Configuration
- MEDIUM: JWT lifetime is 24 hours (recommend 15-30 min)

Summary: 5 findings (1 critical, 2 high, 1 medium, 1 low)
```

**Související:** `/verify`, `/health-check`

---

### 🗄️ Databáze

#### `/migrate`
**Co:** Řízený workflow migrace EF Core s revizí SQL a dokumentací pro rollback.

**Kdy:**
- Po změnách doménového modelu
- "add migration", "update database"
- Přidání/přejmenování/odebrání sloupců

**Příklad:**
```
User: /migrate

Claude: Checking migration state...
Model changes: new ShippingAddress property on Order

Creating migration: AddOrderShippingAddress
Reviewing SQL...
- ALTER TABLE "Orders" ADD "ShippingStreet" text NULL
No data loss. Safe to apply.

Applying migration... Done.
Build: green. Tests: 34/34 passed.

Rollback command:
dotnet ef database update AddOrderTable
```

**Související:** `/build-fix`

---

### 💾 Správa relací

#### `/checkpoint`
**Co:** Uložení postupu vytvořením git commitu a poznámky pro předání.

**Kdy:**
- Zachycení stavu v průběhu relace
- Před rizikovými změnami
- Přepínání úkolů
- "checkpoint", "save progress"

**Příklad:**
```
User: /checkpoint

Claude: Committing: "Add CreateOrder validation"
  - src/Orders/Features/CreateOrder.cs
  - tests/Orders.Tests/Features/CreateOrderTests.cs

Commit: a1b2c3d

Writing handoff to .claude/handoff.md:
## Completed
- [x] Added FluentValidation to CreateOrder

## Pending
- [ ] Add validation to UpdateOrder

Checkpoint saved.
```

**Související:** `/wrap-up`

---

#### `/wrap-up`
**Co:** Rituál na konci relace zachycující dokončenou práci, nevyřešené úkoly a poznatky.

**Kdy:**
- Konec pracovní relace
- Přepínání projektů
- "wrap up", "done for today"

**Příklad:**
```
User: /wrap-up

Claude: ## Session Summary
- Implemented OrderService with validation
- Added integration tests
- Fixed N+1 query

## Pending
- [ ] Add retry logic to payment gateway
- [ ] Wire up FluentValidation for UpdateOrder

## Learnings
- User prefers explicit mapping over AutoMapper

Written to .claude/handoff.md.
```

**Související:** `/checkpoint`

---

### 🧠 Systém instinktů

#### `/instinct-status`
**Co:** Zobrazení všech naučených instinktů s hodnocením spolehlivosti.

**Kdy:**
- Přehled znalostí projektu
- Před exportem instinktů
- "show instincts", "what have you learned"

**Příklad:**
```
User: /instinct-status

Claude: ## Project Instincts (12 total, avg: 0.72)

| Instinct                     | Confidence | Category     |
|------------------------------|------------|--------------|
| Use vertical slice arch      | 0.9        | architecture |
| Explicit mapping over AutoM  | 0.8        | style        |
| Always add CancellationToken | 0.8        | performance  |

2 instincts reinforced this session.
```

**Související:** `/instinct-export`, `/instinct-import`

---

#### `/instinct-export`
**Co:** Export instinktů s vysokou spolehlivostí (>0.7) do sdílitelného formátu.

**Kdy:**
- Sdílení vzorů s novým projektem
- Přenos znalostí v týmu
- Vytvoření týmového základu

**Příklad:**
```
User: /instinct-export

Claude: Exported 7 of 12 instincts (confidence > 0.7):
- Use vertical slice architecture (0.9)
- Explicit mapping over AutoMapper (0.8)
- Always add CancellationToken (0.8)

Written to: .claude/instincts-export.md
```

**Související:** `/instinct-status`, `/instinct-import`

---

#### `/instinct-import`
**Co:** Import instinktů z jiného projektu s řešením konfliktů.

**Kdy:**
- Zakládání nového projektu s existujícími poznatky
- Zavedení repozitáře
- Křížové přenášení vzorů

**Příklad:**
```
User: /instinct-import .claude/instincts-export.md

Claude: Importing instincts...

Imported (5 new, confidence decayed by 0.2):
- Use vertical slice architecture (0.9 -> 0.7)
- Explicit mapping over AutoMapper (0.8 -> 0.6)

Merged (2 existing, kept higher confidence):
- Prefer records for DTOs (kept 0.7)

Total instincts: 14
```

**Související:** `/instinct-status`, `/instinct-export`

---

## Tipy pro efektivní používání příkazů

### Řetězení příkazů
Příkazy fungují nejlépe v sekvenci:
```
/dotnet-init → /plan → /scaffold → /tdd → /verify → /checkpoint
```

### Kdy použít který příkaz

| Situace | Příkaz |
|---------|--------|
| Zakládání nového projektu | `/dotnet-init` |
| Před implementací funkce | `/plan` |
| Potřebuji kostru funkce | `/scaffold` |
| Chci přístup test-first | `/tdd` |
| Build je rozbitý | `/build-fix` |
| Připraveno pro PR | `/verify` |
| Kód potřebuje vyčistit | `/de-sloppify` |
| Potřebuji hodnocení kvality | `/health-check` |
| Revize PR | `/code-review` |
| Bezpečnostní obavy | `/security-scan` |
| Změna databázového schématu | `/migrate` |
| Uložení v průběhu relace | `/checkpoint` |
| Konec dne | `/wrap-up` |

### Integrace s MCP nástroji
Příkazy využívají MCP nástroje Roslyn pro tokenově efektivní analýzu:
- `get_diagnostics` — Varování/chyby kompilátoru
- `detect_antipatterns` — Detekce code smells
- `find_references` — Analýza dopadu
- `get_project_graph` — Porozumění architektuře

### Přizpůsobení
Příkazy respektují konvence vašeho projektu v `CLAUDE.md` a přizpůsobují se:
- Zvolené architektuře (VSA, Clean, DDD)
- Vzorům pojmenování
- Organizaci testů
- Přístupu k validaci

---

## Vývoj příkazů

Příkazy jsou definovány v `commands/*.md` s YAML frontmatter:

```yaml
---
description: >
  What this command does and when to invoke it.
---
```

Každý dokument příkazu obsahuje:
- **What** — Účel a rozsah
- **When** — Spouštěcí fráze a scénáře
- **How** — Postup provedení krok za krokem
- **Example** — Ukázkový výstup
- **Related** — Související příkazy

Podrobnosti implementace naleznete v adresáři `commands/`.
