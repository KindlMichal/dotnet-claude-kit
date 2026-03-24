# Směrování a orchestrace agentů

> Tento soubor definuje, jak Claude Code směruje dotazy na specializované agenty a jak agenti spolupracují.

## Seznam agentů

| Agent | Soubor | Primární doména |
|-------|--------|----------------|
| dotnet-architect | `agents/dotnet-architect.md` | Architektura, struktura projektu, hranice modulů |
| api-designer | `agents/api-designer.md` | Minimal APIs, OpenAPI, verzování, rate limiting |
| ef-core-specialist | `agents/ef-core-specialist.md` | Databáze, dotazy, migrace, vzory EF Core |
| test-engineer | `agents/test-engineer.md` | Testovací strategie, xUnit, WebApplicationFactory, Testcontainers |
| security-auditor | `agents/security-auditor.md` | Autentizace, autorizace, OWASP, tajné klíče |
| performance-analyst | `agents/performance-analyst.md` | Benchmarky, paměť, asynchronní vzory, cache |
| devops-engineer | `agents/devops-engineer.md` | Docker, CI/CD, Aspire, nasazení |
| code-reviewer | `agents/code-reviewer.md` | Vícerozměrná revize kódu |
| build-error-resolver | `agents/build-error-resolver.md` | Autonomní oprava chyb sestavení |
| refactor-cleaner | `agents/refactor-cleaner.md` | Systematické odstraňování mrtvého kódu a čištění |

## Směrovací tabulka

Přiřazení záměru uživatele k agentovi. Pokud dotaz může zpracovat více agentů, vyhrává první shoda.

| Vzor záměru uživatele | Primární agent | Podpůrný agent |
|---|---|---|
| "set up project", "folder structure", "architecture" | dotnet-architect | — |
| "add module", "split into modules", "bounded context" | dotnet-architect | — |
| "create endpoint", "API route", "OpenAPI", "swagger" | api-designer | — |
| "versioning", "rate limiting", "CORS" | api-designer | — |
| "database", "migration", "query", "DbContext", "EF" | ef-core-specialist | — |
| "write tests", "test strategy", "coverage" | test-engineer | — |
| "WebApplicationFactory", "Testcontainers", "xUnit" | test-engineer | — |
| "security", "authentication", "JWT", "OIDC", "authorize" | security-auditor | — |
| "performance", "benchmark", "memory", "profiling" | performance-analyst | — |
| "caching", "HybridCache", "output cache" | performance-analyst | — |
| "Docker", "container", "CI/CD", "pipeline", "deploy" | devops-engineer | — |
| "Aspire", "orchestration", "service discovery" | devops-engineer | — |
| "review this code", "PR review", "code quality" | code-reviewer | — |
| "choose architecture", "which architecture", "architecture decision" | dotnet-architect | — |
| "scaffold feature", "create feature", "add endpoint", "generate feature" | dotnet-architect | api-designer, ef-core-specialist |
| "init project", "setup project", "new project", "generate CLAUDE.md" | dotnet-architect | — |
| "health check", "analyze project", "project report" | code-reviewer | dotnet-architect |
| "review PR", "review changes", "code review", "PR review" | code-reviewer | — |
| "add migration", "ef migration", "update packages", "upgrade nuget" | ef-core-specialist | — |
| "conventions", "coding style", "detect patterns", "code consistency" | code-reviewer | — |
| "add feature" (odpovídající architektuře) | dotnet-architect | api-designer, ef-core-specialist |
| "refactor" | code-reviewer | dotnet-architect |
| "build errors", "fix build", "won't compile" | build-error-resolver | — |
| "clean up", "dead code", "unused code", "de-sloppify" | refactor-cleaner | — |

## Pořadí načítání dovedností

Agenti načítají dovednosti v pořadí závislostí. Základní dovednosti se načítají jako první.

### Výchozí pořadí načítání (všichni agenti)
1. `modern-csharp` — Načítá se vždy, základní znalosti C#
2. Dovednosti specifické pro agenta (viz soubory agentů)

### Mapy dovedností jednotlivých agentů

| Agent | Dovednosti |
|-------|------------|
| dotnet-architect | modern-csharp, architecture-advisor, project-structure, scaffolding, project-setup + podmíněně: vertical-slice, clean-architecture, ddd |
| api-designer | modern-csharp, minimal-api, api-versioning, authentication, error-handling |
| ef-core-specialist | modern-csharp, ef-core, configuration, migration-workflow |
| test-engineer | modern-csharp, testing |
| security-auditor | modern-csharp, authentication, configuration |
| performance-analyst | modern-csharp, caching |
| devops-engineer | modern-csharp, docker, ci-cd, aspire |
| code-reviewer | modern-csharp, code-review-workflow, convention-learner + kontextově (načítá relevantní dovednosti vč. clean-architecture, ddd na základě revidovaných souborů) |
| build-error-resolver | modern-csharp, autonomous-loops + kontextově: ef-core, dependency-injection |
| refactor-cleaner | modern-csharp, de-sloppify + kontextově: testing, ef-core |

## Preference MCP nástrojů

Agenti by měli **preferovat MCP nástroje Roslyn před skenováním souborů**, aby se snížila spotřeba tokenů.

| Úloha | Použijte MCP nástroj | Místo |
|-------|----------------------|-------|
| Zjistit, kde je typ definován | `find_symbol` | Grep/Glob přes všechny .cs soubory |
| Najít všechna použití typu | `find_references` | Grep pro název typu |
| Najít implementace rozhraní | `find_implementations` | Hledání `: IInterface` |
| Pochopit dědičnost | `get_type_hierarchy` | Čtení více souborů |
| Pochopit závislosti projektu | `get_project_graph` | Ruční parsování .csproj souborů |
| Zkontrolovat veřejné API typu | `get_public_api` | Čtení celého zdrojového souboru |
| Zkontrolovat chyby kompilace | `get_diagnostics` | Spuštění `dotnet build` a parsování výstupu |
| Najít nepoužívaný kód k vyčištění | `find_dead_code` | Ruční kontrola všech souborů |
| Zkontrolovat cyklické závislosti | `detect_circular_dependencies` | Ruční sledování referencí projektu |
| Pochopit řetězce volání metod | `get_dependency_graph` | Čtení více souborů a sledování volání |
| Zjistit, které typy mají testy | `get_test_coverage_map` | Ruční hledání testovacích souborů |

## Sdílené meta dovednosti

Těchto 10 meta a produktivních dovedností není vázáno na konkrétního agenta — jakýkoli agent je může načíst, když to kontext vyžaduje:

| Dovednost | Kdy načíst |
|-----------|------------|
| `self-correction-loop` | Po JAKÉKOLI opravě od uživatele — zaznamenat pravidlo do MEMORY.md |
| `wrap-up-ritual` | Uživatel signalizuje konec relace — zapsat předání do `.claude/handoff.md` |
| `context-discipline` | Dochází kontext, navigace ve velkém kódu, plánování strategie průzkumu |
| `model-selection` | Výběr mezi Opus/Sonnet/Haiku, přiřazování modelů subagentům |
| `80-20-review` | Revize kódu, revize PR, rozhodování co zkontrolovat do hloubky |
| `split-memory` | CLAUDE.md přesahuje 300 řádků, potřeba rozdělit instrukce do souborů |
| `learning-log` | Neočividný objev během vývoje — zaznamenat poznatek |
| `instinct-system` | Detekce vzorů napříč relacemi — cyklus pozorování-hypotéza-potvrzení pro konvence projektu |
| `session-management` | Začátek/konec relace — načíst předání, detekovat řešení, zapsat shrnutí relace |
| `autonomous-loops` | Iterativní opravné smyčky — sestavení-oprava, test-oprava, refaktoring s omezeným počtem iterací |

### Směrování meta dovedností

| Vzor záměru uživatele | Dovednost |
|---|---|
| "learn from mistakes", "remember this", "don't do that again" | self-correction-loop |
| "wrap up", "done for today", "save progress", "handoff" | wrap-up-ritual |
| "context", "running out of tokens", "too many files" | context-discipline |
| "which model", "use Opus", "use Sonnet", "switch model" | model-selection |
| "review this", "what should I review", "blast radius" | 80-20-review |
| "split CLAUDE.md", "too long", "organize instructions" | split-memory |
| "log this", "document this finding", "gotcha" | learning-log |
| "show instincts", "what have you learned", "confidence scores" | instinct-system |
| "start session", "load handoff", "session start" | session-management |
| "fix build loop", "keep fixing", "auto-fix" | autonomous-loops |

## Lomítkové příkazy

Příkazy se mapují na dovednosti a agenty. Používejte je jako zkratky pro běžné pracovní postupy.

| Příkaz | Primární dovednost | Primární agent | Účel |
|--------|-------------------|----------------|------|
| `/dotnet-init` | project-setup | dotnet-architect | Interaktivní inicializace projektu |
| `/plan` | architecture-advisor | dotnet-architect | Plánování s ohledem na architekturu |
| `/verify` | verification-loop | — | 7fázový ověřovací pipeline |
| `/tdd` | testing | test-engineer | Pracovní postup red-green-refactor |
| `/scaffold` | scaffolding | dotnet-architect | Scaffolding funkcí s ohledem na architekturu |
| `/code-review` | code-review-workflow | code-reviewer | Revize kódu s využitím MCP |
| `/build-fix` | autonomous-loops | build-error-resolver | Iterativní oprava chyb sestavení |
| `/checkpoint` | wrap-up-ritual | — | Uložení pokroku (commit + předání) |
| `/security-scan` | security-scan | security-auditor | Audit OWASP + tajných klíčů + závislostí |
| `/migrate` | migration-workflow | ef-core-specialist | Bezpečný pracovní postup EF Core migrace |
| `/health-check` | health-check | code-reviewer | Hodnocená zpráva o zdraví projektu |
| `/de-sloppify` | de-sloppify | refactor-cleaner | Systematické čištění kódu |
| `/wrap-up` | wrap-up-ritual | — | Rituál ukončení relace |
| `/instinct-status` | instinct-system | — | Zobrazení naučených instinktů |
| `/instinct-export` | instinct-system | — | Export instinktů do sdílitelného formátu |
| `/instinct-import` | instinct-system | — | Import instinktů z jiného projektu |

## Řešení konfliktů

Když dotaz může zpracovat dva agenti:

1. **Otázky architektury mají přednost před implementací** — "Jak bych měl strukturovat platební modul?" → dotnet-architect, i když api-designer by mohl zpracovat část s endpointy
2. **Konkrétní má přednost před obecným** — "Jak optimalizuji tento EF dotaz?" → ef-core-specialist, ne performance-analyst
3. **Bezpečnostní obavy se vždy vyzdvihnou** — I když je primární jiný agent, bezpečnostní problémy se označí pro security-auditor
4. **Revize kódu je holistická** — code-reviewer načítá dovednosti kontextově na základě obsahu PR

## Doporučení pro rozpočet tokenů

Podrobné strategie správy kontextu najdete v dovednosti **`context-discipline`**.

- **Malé dotazy** (jeden vzor/oprava): Načtěte 1–2 dovednosti, použijte MCP nástroje pro kontext
- **Střední dotazy** (implementace funkce): Načtěte 3–4 dovednosti, použijte MCP nástroje k pochopení existujícího kódu
- **Velké dotazy** (revize architektury): Načtěte všechny relevantní dovednosti, nejdříve použijte `get_project_graph` k pochopení tvaru řešení

## Vzory odpovědí

Všichni agenti by měli:
1. **Začít doporučeným přístupem** — Nevyjmenovávat všechny možnosti rovnocenně
2. **Ukázat kód jako první, vysvětlit poté** — Vývojáři preferují vidět řešení a poté pochopit proč
3. **Proaktivně upozorňovat na anti-vzory** — Pokud má existující kód uživatele problémy, zmínit je
4. **Odkazovat na dovednosti** — Odkázat na relevantní dovednosti pro hlubší čtení
5. **Používat MCP nástroje před čtením souborů** — Snížit spotřebu tokenů