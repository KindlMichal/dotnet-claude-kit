# dotnet-claude-kit — Pokyny pro vývoj

> Tyto pokyny jsou určeny pro vývoj TOHOTO repozitáře. Uživatelské šablony projektů najdete v `templates/`.

## Účel repozitáře

dotnet-claude-kit je názorový společník Claude Code pro .NET vývojáře. Poskytuje skills, agents, šablony, znalostní dokumenty a Roslyn MCP server, které výrazně zvyšují efektivitu Claude Code při vývoji v .NET.

## Filosofie

- **Vedení místo předepisování** — Položíme správné otázky a poté doporučíme nejlepší přístup s jasným zdůvodněním
- **Pouze moderní .NET** — Cílíme na .NET 10 a C# 14. Žádné zastaralé vzory, žádná zpětná kompatibilita s .NET Framework
- **S ohledem na architekturu** — Podporujeme VSA, Clean Architecture, DDD a Modular Monolith s poradním skillem, který doporučí nejlepší volbu (viz ADR-005)
- **S ohledem na tokeny** — Každý soubor respektuje limity kontextového okna. Skills mají maximálně 400 řádků
- **Praktičnost nad teorií** — Každé doporučení obsahuje příklad kódu a zdůvodnění „proč"

## Struktura skills

Skills se řídí otevřeným standardem Agent Skills. Každý skill se nachází v `skills/<skill-name>/SKILL.md`.

### Schéma frontmatteru (povinné)

```yaml
---
name: skill-name           # kebab-case, odpovídá názvu adresáře
description: >
  What this skill does and when Claude should load it.
  Include trigger keywords and specific scenarios.
---
```

### Povinné sekce

1. **Core Principles** — 3–5 očíslovaných názorových výchozích nastavení se zdůvodněním
2. **Patterns** — Příklady kódu s vysvětlením. Každý vzor obsahuje:
   - Popisný nadpis
   - Funkční C# kód (musí být konceptuálně kompilovatelný)
   - Stručné vysvětlení, proč je toto doporučený přístup
3. **Anti-patterns** — Co NEDĚLAT, s porovnáním BAD/GOOD kódu
4. **Decision Guide** — Markdown tabulka: Scénář → Doporučení

### Kvalitativní standardy

- **Maximálně 400 řádků** — Každý řádek si musí zasloužit své místo. Respektujte rozpočty tokenů.
- **Každé doporučení má své „proč"** — Žádná holá pravidla bez zdůvodnění
- **Příklady kódu musí být v moderním C#** — Primary constructors, collection expressions, file-scoped namespaces, records
- **Žádný Swashbuckle** — Použijte vestavěnou podporu .NET OpenAPI
- **Žádný repository pattern nad EF Core** — Používejte DbContext přímo
- **`TimeProvider` místo `DateTime.Now`** — Vždy

## Struktura agents

Agents se nacházejí v `agents/<agent-name>.md`. Každý agent obsahuje:

1. **Definice role** — V čem je tento agent expertem
2. **Závislosti na skills** — Které skills tento agent načítá (podle názvu)
3. **Použití MCP nástrojů** — Kdy použít nástroje cwm-roslyn-navigator vs. čtení souborů
4. **Vzory odpovědí** — Jak strukturovat vedení
5. **Hranice** — Co tento agent NEŘEŠÍ

## Struktura šablon

Šablony se nacházejí v `templates/<template-name>/`. Každá obsahuje:

- `CLAUDE.md` — Soubor připravený k vložení do uživatelských projektů
- `README.md` — Kdy a jak tuto šablonu použít

Šablony odkazují na skills podle názvu a měly by být samostatné — uživatel zkopíruje pouze CLAUDE.md do svého projektu.

## Znalostní dokumenty

Znalostní soubory v `knowledge/` NEJSOU skills. Jsou to referenční materiály, na které odkazují agents a šablony. Neřídí se formátem frontmatteru pro skills.

- `dotnet-whats-new.md` — Aktualizováno s každým vydáním .NET
- `common-antipatterns.md` — Vzory, které by Claude neměl nikdy generovat
- `package-recommendations.md` — Prověřené NuGet balíčky
- `breaking-changes.md` — Úskalí při migraci
- `decisions/*.md` — ADR podle formátu šablony

## Struktura commands

Commands se nacházejí v `commands/<command-name>.md`. Každý command je lehký orchestrátor, který vyvolává skills a agents.

### Schéma frontmatteru (povinné)

```yaml
---
description: >
  What this command does. Displayed in command listings.
---
```

### Povinné sekce

1. **What** — Co command dělá
2. **When** — Kdy ho použít (spouštěcí fráze)
3. **How** — Krok za krokem postup vykonávání (vyvolává skills/agents)
4. **Example** — Příklad výstupu nebo použití
5. **Related** — Související commands

### Kvalitativní standardy

- **Maximálně 200 řádků** — Commands jsou orchestrátory, ne encyklopedie
- **Vyvolávejte, neimplementujte** — Commands odkazují na skills a agents pro vlastní logiku
- **Jasné spouštěcí fráze** — Uživatelé by měli vědět, kdy po tomto commandu sáhnout

## Struktura pravidel

Pravidla se nacházejí v `rules/dotnet/<rule-name>.md`. Pravidla jsou vždy načtena do kontextu.

### Schéma frontmatteru (povinné)

```yaml
---
alwaysApply: true
description: >
  What this rule enforces.
---
```

### Kvalitativní standardy

- **Maximálně 100 řádků** — Pravidla jsou vždy v kontextu, takže každý řádek stojí tokeny
- **Předepisující se zdůvodněním** — Každé pravidlo má stručné „proč"
- **Formát DO/DON'T** — Jasná, snadno čitelná pravidla
- **Celkový rozpočet pravidel: ~600 řádků** — Všechna pravidla dohromady musí zůstat úsporná

## Roslyn MCP Server

MCP server se nachází v `mcp/CWM.RoslynNavigator/`. Je to aplikace v .NET 10 využívající ModelContextProtocol SDK.

### Sestavení

```bash
dotnet build mcp/CWM.RoslynNavigator/CWM.RoslynNavigator.slnx
dotnet test mcp/CWM.RoslynNavigator/CWM.RoslynNavigator.slnx
```

### Klíčová pravidla

- Nástroje jsou **pouze pro čtení** — Žádné generování kódu, žádné úpravy
- Odpovědi jsou **optimalizované na tokeny** — Vracejte cesty k souborům, čísla řádků a krátké úryvky, nikdy celé obsahy souborů
- Workspace musí zvládat **postupné načítání** — Při inicializaci vracejte stav „loading" místo chyb

## Standardy pracovního postupu

Jak by měl Claude pracovat na tomto repozitáři (a na jakémkoli projektu používajícím šablony dotnet-claude-kit).

### Plánujte před stavbou

- Vstupte do režimu plánování pro JAKÝKOLI netriviální úkol (3+ kroky nebo architektonická rozhodnutí)
- Iterujte na plánu, dokud není solidní, než začnete psát kód
- Pokud se něco v průběhu implementace pokazí, ZASTAVTE SE a přeplánujte — nepokračujte v nefunkčním přístupu
- Pište podrobné specifikace předem, aby se snížila nejednoznačnost — vágní plány produkují vágní kód

### Ověřte před dokončením

- Nikdy neoznačujte úkol jako dokončený bez důkazu, že funguje
- Po změnách spusťte `dotnet build` a `dotnet test` — zelený build je minimální laťka
- Použijte `get_diagnostics` přes Roslyn MCP k zachycení varování po úpravách
- Zeptejte se sami sebe: „Schválil by to senior .NET inženýr?" — pokud ne, iterujte
- Když je to relevantní, porovnejte chování mezi main a vašimi změnami

### Opravujte chyby autonomně

- Při obdržení hlášení o chybě: prozkoumejte a opravte ji. Nežádejte o vedení za ruku
- Ukažte na logy, chyby, padající testy — a pak je vyřešte
- Opravte padající CI testy bez toho, abyste čekali na pokyny
- Nulový context switching vyžadovaný od uživatele

### Vyžadujte eleganci (s mírou)

- U netriviálních změn: zastavte se a zeptejte se „existuje elegantnější způsob?"
- Pokud oprava působí jako hack, ustupte: „Se vším, co nyní vím, implementuji elegantní řešení"
- U jednoduchých, zřejmých oprav toto přeskočte — nepřeinženýrujte. Tři řádky jasného kódu porazí předčasnou abstrakci
- Zpochybněte vlastní práci, než ji prezentujete

### Používejte subagents pro paralelní práci

- Používejte subagents hojně, aby hlavní kontextové okno zůstalo čisté
- Přeneste výzkum, průzkum a paralelní analýzu na subagents
- Jeden úkol na jednoho subagenta pro soustředěné vykonávání
- U složitých problémů nasaďte více výpočetního výkonu přes subagents místo sekvenční práce

### Učte se z oprav

- Po JAKÉKOLI opravě od uživatele zachyťte vzor do automatické paměti (`MEMORY.md`)
- Pište pravidla, která zabrání opakování stejné chyby
- Na začátku relace si projděte paměť pro poučení relevantní k projektu
- Toto je systém s kumulativním efektem — míra chyb by měla postupně klesat

## Pracovní postup pro přispívání

1. Zkontrolujte specifikaci v `docs/dotnet-claude-kit-SPEC.md` pro celkovou vizi
2. Dodržujte strukturu skill/agent/template/command/rule definovanou výše
3. Před commitem spusťte `dotnet format --verify-no-changes`
4. Ujistěte se, že soubory skills mají pod 400 řádků, commands pod 200, pravidla pod 100
5. Každý nový vzor potřebuje porovnání BAD/GOOD kódu v Anti-patterns
6. Ujistěte se, že všechny křížové odkazy (commands → skills, agents → skills) vedou na existující soubory
7. Nové commands musí mít YAML frontmatter s `description`
8. Nová pravidla musí mít `alwaysApply: true` ve frontmatteru
