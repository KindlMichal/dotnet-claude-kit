# Referenční příručka hooků dotnet-claude-kit

> Automatizované kontroly kvality a automatizace pracovního postupu pro .NET vývoj s Claude Code.

## Co jsou hooky?

Hooky jsou automatizované skripty, které se spouštějí v konkrétních bodech vývojového pracovního postupu. Vynucují kvalitu kódu, předcházejí běžným chybám a automatizují opakující se úkoly jako formátování a obnovu balíčků.

**Hooky se spouštějí automaticky** — nevoláte je ručně. Jsou spouštěny systémem pluginů Claude Code na základě vzorů použití nástrojů.

## Typy hooků

### Pre-Tool hooky
Spouštějí se **před** provedením nástroje. Mohou operaci zablokovat, pokud validace selže.

### Post-Tool hooky
Spouštějí se **po** dokončení nástroje. Provádějí úklid, formátování nebo analýzu.

## Dostupné hooky

| Hook | Typ | Spouštěč | Účel |
|------|-----|----------|------|
| `pre-bash-guard.sh` | Pre | Bash tool | Blokování destruktivních git/souborových operací |
| `pre-build-validate.sh` | Pre | Manuální | Validace struktury projektu před buildem |
| `pre-commit-format.sh` | Pre | Git commit | Ověření formátování kódu |
| `pre-commit-antipattern.sh` | Pre | Git commit | Detekce anti-vzorů ve staged souborech |
| `post-edit-format.sh` | Post | Edit/Write | Automatické formátování změněných C# souborů |
| `post-scaffold-restore.sh` | Post | Edit/Write | Obnova NuGet po změnách .csproj |
| `post-test-analyze.sh` | Post | Manuální | Analýza výsledků testů |

---

## Pre-Tool hooky

### `pre-bash-guard.sh`
**Spouštěč:** Před jakýmkoli spuštěním Bash nástroje
**Účel:** Blokování destruktivních operací, které by mohly způsobit ztrátu práce nebo poškodit repozitář

**Blokuje:**
- `git push --force` / `git push -f` — Vynucené push operace
- `git reset --hard` — Zahození necommitovaných změn
- `git clean -f` — Smazání nesledovaných souborů
- `git checkout .` — Zahození nestaged změn
- `rm -rf` (kromě bezpečných cílů: `node_modules`, `bin`, `obj`, `TestResults`, `.vs`)

**Povoluje s varováním:**
- `dotnet run` — Varuje ohledně kontroly launchSettings.json

**Příklad:**
```bash
# User: Run git push --force
# Hook: BLOCKED: Force push detected. Use regular push or discuss with the user first.
```

**Návratové kódy:**
- `0` — Povolení příkazu
- `2` — Zablokování příkazu

---

### `pre-build-validate.sh`
**Spouštěč:** Manuální (spustit před `dotnet build`)
**Účel:** Validace struktury projektu vůči očekávané architektuře

**Kontroluje:**
- Existenci souboru řešení (`.sln` nebo `.slnx`)
- `Directory.Build.props` pro řešení s více projekty
- `global.json` pro fixaci verze SDK
- `.editorconfig` pro konzistenci stylu kódu
- Existenci testovacích projektů
- Sladění cílových frameworků

**Použití:**
```bash
bash hooks/pre-build-validate.sh
```

**Příklad výstupu:**
```
Validating project structure...
⚠️  No global.json found — consider pinning the SDK version
⚠️  No test projects found — consider adding tests
⚠️  2 warning(s) — consider addressing these
```

**Návratové kódy:**
- `0` — Validace prošla (nebo pouze varování)
- `1` — Nalezeny chyby, opravte před buildem

---

### `pre-commit-format.sh`
**Spouštěč:** Git pre-commit hook
**Účel:** Ověření formátování kódu před commitem

**Kontroluje:**
- Spouští `dotnet format --verify-no-changes`
- Selže, pokud jakékoli soubory potřebují formátování

**Použití:**
```bash
# Installed as .git/hooks/pre-commit
# Runs automatically on git commit
```

**Příklad:**
```bash
# Commit attempt with unformatted code
Checking code formatting...
Format check failed. Run 'dotnet format' to fix formatting issues.
```

**Návratové kódy:**
- `0` — Všechny soubory správně naformátovány
- `1` — Nalezeny problémy s formátováním, commit zablokován

---

### `pre-commit-antipattern.sh`
**Spouštěč:** Git pre-commit hook
**Účel:** Detekce běžných .NET anti-vzorů ve staged C# souborech

**Detekuje:**
- `DateTime.Now` / `DateTime.UtcNow` → Použijte `TimeProvider`
- `new HttpClient()` → Použijte `IHttpClientFactory`
- `async void` (kromě event handlerů) → Použijte `async Task`
- `.Result` / `.GetAwaiter().GetResult()` → Sync-over-async

**Použití:**
```bash
# Installed as .git/hooks/pre-commit
# Runs automatically on git commit
```

**Příklad:**
```bash
Checking staged C# files for common issues...
⚠️  OrderService.cs:42: Use TimeProvider instead of DateTime.Now
🔴 PaymentHandler.cs:18: async void is dangerous — use async Task instead

Found 2 anti-pattern issue(s) in staged files.
Fix the issues above or use 'git commit --no-verify' to skip this check.
```

**Návratové kódy:**
- `0` — Žádné anti-vzory nenalezeny
- `1` — Nalezeny problémy, commit zablokován

---

## Post-Tool hooky

### `post-edit-format.sh`
**Spouštěč:** Po použití Edit/Write nástroje na `.cs` soubory
**Účel:** Automatické formátování změněných C# souborů pro udržení konzistentního stylu

**Chování:**
1. Detekuje editovaný `.cs` soubor ze vstupu nástroje
2. Najde nejbližší `.csproj` nebo `.sln`
3. Spustí `dotnet format` omezený na editovaný soubor
4. Tiše uspěje (žádný výstup, pokud nenastane chyba)

**Detekce cesty k souboru:**
- První argument (`$1`)
- Proměnná prostředí `CLAUDE_EDITED_FILE`
- PostToolUse stdin JSON (`{"tool_input":{"file_path":"..."}}`)

**Příklad:**
```bash
# Claude edits src/Features/Orders/CreateOrder.cs
# Hook automatically runs:
dotnet format src/Features/Orders/Orders.csproj --include CreateOrder.cs --no-restore
```

**Konfigurace:**
Respektuje `.editorconfig` v projektu pro pravidla formátování.

---

### `post-scaffold-restore.sh`
**Spouštěč:** Po použití Edit/Write nástroje na `.csproj` soubory
**Účel:** Obnova NuGet balíčků po změnách projektového souboru

**Chování:**
1. Detekuje editovaný `.csproj` soubor
2. Spustí `dotnet restore --verbosity quiet`
3. Nahlásí úspěch nebo selhání

**Proč je to důležité:**
- Build ihned po změnách `.csproj` selže, pokud nejsou balíčky obnoveny
- Hook zajistí, že závislosti jsou připraveny před dalším buildem

**Příklad:**
```bash
# Claude adds a PackageReference to MyApp.Api.csproj
# Hook automatically runs:
Project file changed. Running dotnet restore...
Restore completed.
```

**Návratové kódy:**
- `0` — Vždy (při selhání varuje, ale neblokuje)

---

### `post-test-analyze.sh`
**Spouštěč:** Manuální (přesměrování výstupu `dotnet test`)
**Účel:** Analýza výsledků testů a poskytnutí akčního shrnutí

**Použití:**
```bash
dotnet test 2>&1 | bash hooks/post-test-analyze.sh
# or
dotnet test > test-output.log 2>&1
bash hooks/post-test-analyze.sh test-output.log
```

**Výstup:**
```
═══════════════════════════════════
  Test Results Summary
═══════════════════════════════════

  🔴 FAILED: 2
  ✅ Passed: 45
  ⏭️  Skipped: 1

  Failed Tests:
  ─────────────
  OrderServiceTests.CreateOrder_WithInvalidSku_Returns400
  PaymentHandlerTests.ProcessPayment_WithExpiredCard_ThrowsException

  Next Steps:
  1. Fix the failing tests above
  2. Run 'dotnet test' to verify fixes
  3. Check test output for root cause details

═══════════════════════════════════
```

**Parsuje:**
- Počet úspěšných
- Počet neúspěšných
- Počet přeskočených
- Detaily selhání (prvních 50 řádků)

---

## Konfigurace hooků

Hooky jsou konfigurovány v `hooks/hooks.json`:

```json
{
  "hooks": {
    "PreToolUse": [
      {
        "matcher": "Bash",
        "hooks": [
          {
            "type": "command",
            "command": "bash ${CLAUDE_PLUGIN_ROOT}/hooks/pre-bash-guard.sh"
          }
        ]
      }
    ],
    "PostToolUse": [
      {
        "matcher": "Edit|Write",
        "hooks": [
          {
            "type": "command",
            "command": "bash ${CLAUDE_PLUGIN_ROOT}/hooks/post-edit-format.sh"
          },
          {
            "type": "command",
            "command": "bash ${CLAUDE_PLUGIN_ROOT}/hooks/post-scaffold-restore.sh"
          }
        ]
      }
    ]
  }
}
```

### Vzory matcherů
- `Bash` — Odpovídá použití Bash nástroje
- `Edit|Write` — Odpovídá operacím editace nebo zápisu souborů
- `Edit` — Odpovídá pouze operacím editace
- `Write` — Odpovídá pouze operacím zápisu

### Proměnné prostředí
- `${CLAUDE_PLUGIN_ROOT}` — Kořenový adresář pluginu dotnet-claude-kit
- `${CLAUDE_EDITED_FILE}` — Cesta k editovanému souboru (pouze PostToolUse)
- `${CLAUDE_TOOL_INPUT}` — Kompletní vstupní JSON nástroje (pouze PreToolUse)

---

## Instalace Git hooků

Pro použití pre-commit hooků ve vašem projektu:

### Možnost 1: Symbolický odkaz (doporučeno)
```bash
ln -s ../../hooks/pre-commit-format.sh .git/hooks/pre-commit
chmod +x .git/hooks/pre-commit
```

### Možnost 2: Kopírování
```bash
cp hooks/pre-commit-format.sh .git/hooks/pre-commit
chmod +x .git/hooks/pre-commit
```

### Možnost 3: Kombinace více hooků
Vytvořte `.git/hooks/pre-commit`:
```bash
#!/usr/bin/env bash
set -e

# Run format check
bash hooks/pre-commit-format.sh

# Run anti-pattern check
bash hooks/pre-commit-antipattern.sh

echo "✓ All pre-commit checks passed"
```

Nastavte jako spustitelný:
```bash
chmod +x .git/hooks/pre-commit
```

---

## Pravidla hooků

### CO DĚLAT
- **Přijměte změny formátování po editaci** — Automaticky vynucují konzistentní styl
- **Prozkoumejte selhání pre-commit hooků** — Opravte příčinu, neobcházejte pomocí `--no-verify`
- **Projděte si výstup post-test-analyze** — Obsahuje užitečné poznatky
- **Počkejte na post-scaffold-restore** — Nechte obnovu NuGet dokončit před buildem

### CO NEDĚLAT
- **Nevracejte změny formátování** — Výstup hooku je kanonický styl
- **Nepřeskakujte pre-commit hooky** — Zachycují reálné problémy dříve, než se dostanou do CI
- **Neobcházejte pomocí `--no-verify`** — Používejte pouze v nouzových situacích, nikdy jako standardní postup
- **Nezasahujte do konfigurace hooků** — Hooky jsou nakonfigurovány záměrně

---

## Vývoj hooků

### Vytvoření nového hooku

1. **Vytvořte skript** v `hooks/`
2. **Přidejte shebang a ošetření chyb:**
   ```bash
   #!/usr/bin/env bash
   set -euo pipefail
   ```
3. **Zdokumentujte použití** v komentářích na začátku
4. **Ošetřete vstup** z argumentů, proměnných prostředí nebo stdin
5. **Ukončete s odpovídajícím kódem:**
   - `0` — Úspěch (povolení operace)
   - `1` — Selhání (zablokování operace, zobrazení chyby)
   - `2` — Blokování (pro PreToolUse hooky)
6. **Zaregistrujte v `hooks.json`** pokud se jedná o hook nástroje
7. **Otestujte ručně** před commitem

### Šablona skriptu hooku

```bash
#!/usr/bin/env bash
# Hook name: brief description
# Trigger: when this runs
# Purpose: what it does
#
# Usage: how to invoke manually (if applicable)

set -euo pipefail

# Input handling
INPUT="${1:-${ENV_VAR:-}}"

# Validation logic
if [[ condition ]]; then
    echo "Error message"
    exit 1
fi

# Success
echo "Success message"
exit 0
```

---

## Řešení problémů

### Hook se nespouští
**Zkontrolujte:**
1. Hook je zaregistrován v `hooks.json`
2. Skript má oprávnění ke spuštění (`chmod +x`)
3. Vzor matcheru odpovídá používanému nástroji
4. `${CLAUDE_PLUGIN_ROOT}` se správně rozpoznává

### Hook neočekávaně selhává
**Ladění:**
1. Spusťte hook ručně s testovacím vstupem
2. Zkontrolujte návratový kód: `echo $?`
3. Projděte si chybový výstup
4. Ověřte, že jsou dostupné závislosti (dotnet, git)

### Formátovací hook neaplikuje změny
**Možné příčiny:**
1. V projektu chybí `.editorconfig`
2. `dotnet format` není nainstalován
3. Soubor není součástí `.csproj` nebo `.sln`

**Oprava:**
```bash
# Verify dotnet format is available
dotnet format --version

# Run manually to see errors
dotnet format --verbosity diagnostic
```

### Pre-commit hook blokuje validní kód
**Dočasné obejití** (používejte střídmě):
```bash
git commit --no-verify -m "message"
```

**Lepší přístup:**
1. Opravte problém, který hook detekoval
2. Pokud se jedná o falešně pozitivní nález, aktualizujte skript hooku
3. Commitněte opravu

---

## Výkonnostní aspekty

### Doba spuštění hooků
- `post-edit-format.sh` — ~1-2 sekundy na soubor
- `post-scaffold-restore.sh` — ~3-10 sekund (závisí na počtu balíčků)
- `pre-commit-format.sh` — ~2-5 sekund (celé řešení)
- `pre-commit-antipattern.sh` — ~1 sekunda (pouze staged soubory)

### Tipy pro optimalizaci
1. **Omezení formátování** pouze na změněné soubory (již implementováno v post-edit-format)
2. **Použijte `--no-restore`** v příkazech pro formátování k přeskočení nadbytečných obnov
3. **Spouštějte pre-commit kontroly** pouze na staged souborech, ne na celém řešení
4. **Cachujte výsledky** kde je to možné (zatím neimplementováno)

---

## Integrace s příkazy

Hooky doplňují příkazy:

| Příkaz | Související hooky |
|--------|------------------|
| `/scaffold` | `post-scaffold-restore.sh` — Automatická obnova po generování .csproj |
| `/verify` | `pre-commit-format.sh`, `pre-commit-antipattern.sh` — Stejné kontroly |
| `/de-sloppify` | `post-edit-format.sh` — Automatické formátování během čištění |
| `/build-fix` | `pre-build-validate.sh` — Včasné zachycení strukturálních problémů |
| `/checkpoint` | `pre-commit-format.sh`, `pre-commit-antipattern.sh` — Spouští se při commitu |

---

## Osvědčené postupy

### Pro uživatele
1. **Důvěřujte hookům** — Jsou navrženy k zachycení reálných problémů
2. **Čtěte výstup hooků** — Chybové zprávy obsahují konkrétní kroky k nápravě
3. **Nebojujte s formátováním** — Nechte hook vynucovat konzistenci
4. **Hlaste falešně pozitivní nálezy** — Pomozte vylepšit hooky

### Pro vývojáře hooků
1. **Udržujte hooky rychlé** — Vývojáři budou pomalé hooky obcházet
2. **Poskytujte srozumitelné chybové zprávy** — Popište co je špatně a jak to opravit
3. **Na návratových kódech záleží** — 0 = úspěch, 1 = chyba, 2 = blokování
4. **Testujte okrajové případy** — Prázdný vstup, chybějící soubory, žádné řešení
5. **Důkladně dokumentujte** — Použití, účel, návratové kódy

---

## Viz také

- **Příkazy** — Viz `COMMANDS-README.md` pro orchestraci pracovních postupů
- **Dovednosti** — Viz `skills/` pro vzory kódování a anti-vzory
- **Pravidla** — Viz `.claude/rules/hooks.md` pro pravidla interakce s hooky
- **MCP nástroje** — Použijte Roslyn MCP pro hlubší analýzu, než jakou poskytují hooky
