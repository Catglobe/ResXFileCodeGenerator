# ResXFileCodeGenerator — Setup Guide

Catglobe.ResXFileCodeGenerator is a Roslyn incremental source generator (NuGet: `Catglobe.ResXFileCodeGenerator`) that converts `.resx` XML resource files into strongly-typed C# accessor classes at compile time. Two backends: legacy `System.Resources.ResourceManager` with satellite DLLs, and ultra-fast code-generated lookup tables (~600% faster, zero allocations, no satellite DLLs).

**NuGet**: [Catglobe.ResXFileCodeGenerator](https://www.nuget.org/packages/Catglobe.ResXFileCodeGenerator)
**Requires**: .NET 6 SDK or later, SDK-style `.csproj`

## For Humans

ResXFileCodeGenerator watches your `.resx` files and generates C# classes so you can write `Resources.Greeting` instead of `ResourceManager.GetString("Greeting")`. Every change to a `.resx` file triggers an incremental regeneration — the generated code updates instantly with full IntelliSense.

You have two modes. **Code-gen mode** (`UseResManager=true`) bakes every translation directly into your assembly as compiled code. Lookups take ~5 nanoseconds with zero allocations, satellite DLLs disappear, and the linker can tree-shake unused strings. The trade-off: custom `CultureInfo` at runtime is not supported. **ResourceManager mode** (`UseResManager=false`, the default) keeps the standard `System.Resources.ResourceManager` with satellite DLLs and full dynamic culture support, at the cost of ~33ns lookups, per-call allocations, and cold-start penalty.

Most projects should pick code-gen mode. The only reason to stay on ResourceManager is if you set `Thread.CurrentThread.CurrentUICulture` to a custom `CultureInfo` instance at runtime. If your translations are known at build time — and nearly all are — code-gen is faster, simpler, and produces smaller assemblies.

Setup takes five minutes: install the NuGet package, drop a few MSBuild properties into `Directory.Build.props`, add per-file-type overrides and satellite-prevention targets in `Directory.Build.targets`, and build. The generator discovers `.resx` files automatically — no registration, no `.tt` templates, no manual mapping. Culture files (`Resources.da.resx`, `Resources.da-DK.resx`) are grouped with their neutral parent by basename.

## For LLM Agents

Paste this prompt into your AI agent:
```
Add latest version of ResXFileCodeGenerator using nuget, and configure using the instructions found here: https://raw.githubusercontent.com/Catglobe/ResXFileCodeGenerator/main/docs/guide/setup.md
```

If you are an LLM agent helping a user set up Catglobe.ResXFileCodeGenerator, follow these steps in order.

### Step 0: Discover the current project state

**Resolution rule**: The discovery phase resolves EVERY question it can answer from the filesystem. The user is only asked questions discovery CANNOT answer, and even then recommendations are provided. An agent that reaches Step 1 without running discovery has already failed.

**0.1 Locate the solution and SDK-style projects**

```powershell
$cwd = (Get-Location).Path
$slnFiles  = @(Get-ChildItem -Recurse -Filter *.sln  | Select-Object -First 10 FullName)
$slnxFiles = @(Get-ChildItem -Recurse -Filter *.slnx | Select-Object -First 10 FullName)
$allSolutions = $slnFiles + $slnxFiles
if ($allSolutions.Count -eq 0) { Write-Output "No .sln or .slnx found — this repo may be solution-less" }
elseif ($allSolutions.Count -gt 1) {
    Write-Output "Found $($allSolutions.Count) solution files:"
    $allSolutions | ForEach-Object { Write-Output "  $($_.FullName)" }
    Write-Output "MULTI-SOLUTION: Ask user which one to configure, or auto-pick the solution with the most .resx files."
}
$projects = Get-ChildItem -Recurse -Filter *.csproj
$sdkProjects = $projects | Where-Object { Select-String -Path $_.FullName -Pattern 'Microsoft.NET.Sdk' -Quiet }
$nonSdkProjects = $projects | Where-Object { $_ -notin $sdkProjects }
if ($nonSdkProjects) {
    Write-Output "NON-SDK-STYLE PROJECTS FOUND:"
    $nonSdkProjects | ForEach-Object { Write-Output "  $($_.FullName)" }
    Write-Output "These are not supported by Catglobe.ResXFileCodeGenerator (SDK-style .csproj required)."
}
foreach ($p in $sdkProjects) {
    $tfmMatches = Select-String -Path $p.FullName -Pattern '<TargetFramework[^s]'
    $tfm = if ($tfmMatches) { ($tfmMatches | Select-Object -First 1).Line.Trim() } else { "No TFM found" }
    $rel = $p.FullName.Replace($cwd + '\', '')
    $resxCount = @(Get-ChildItem (Split-Path $p.FullName) -Filter *.resx -Recurse |
                    Where-Object { $_.DirectoryName -notmatch '\\obj\\' -and $_.DirectoryName -notmatch '\\bin\\' }).Count
    Write-Output "  $rel  |  $tfm  |  .resx: $resxCount"
}
if ($sdkProjects.Count -eq 0) { Write-Output "No SDK-style projects found. Nothing to configure."; exit }
```

**0.2 Build a basename-grouped resource map that matches the generator exactly**

`BasenameFromPath` (`Utilities.cs:113`) = directory prefix + first-dot truncated name.

```powershell
$resxFiles = Get-ChildItem -Recurse -Filter *.resx |
    Where-Object { $_.FullName -notmatch '\\obj\\' -and $_.DirectoryName -notmatch '\\bin\\' }
$parsedFiles = $resxFiles | ForEach-Object {
    $full    = $_.FullName
    $relPath = $full.Replace($cwd + '\', '')
    $dir     = Split-Path $full -Parent
    $name    = $_.Name
    # BasenameFromPath: directory + name before first dot
    $dotIdx = $name.IndexOf('.')
    if ($dotIdx -gt 0) {
        $firstSegment = $name.Substring(0, $dotIdx)
        $fullBasename = "$dir\$firstSegment"
    } else {
        $firstSegment = [System.IO.Path]::GetFileNameWithoutExtension($name)
        $fullBasename = "$dir\$firstSegment"
    }
    # Culture detection: last segment before .resx, validated
    $noExt = $name -replace '\.resx$', '[]'
    $noExt = $noExt -replace '\[\]$', ''  # remove sentinel added by -replace
    $lastDot = $noExt.LastIndexOf('.')
    $culture = 'neutral'
    if ($lastDot -gt 0) {
        $candidate = $noExt.Substring($lastDot + 1)
        if ($candidate -imatch '^[a-zA-Z]{2,3}(-[a-zA-Z]{2,8}){0,2}$') { $culture = $candidate }
    }
    # Expected code file name
    $expectedCodeFile = if ($culture -ne 'neutral') { $noExt -replace "\.$culture$", '' } else { $noExt }
    # Framework extension hint
    $frameworkExt = ''
    if ($expectedCodeFile -match '\.(razor|cshtml|aspx|ascx|master|xaml|resw)$') { $frameworkExt = $matches[1] }
    [PSCustomObject]@{
        FullPath         = $full
        RelativePath     = $relPath
        Directory        = $dir
        Basename         = $fullBasename
        FirstSegment     = $firstSegment
        Culture          = $culture
        ExpectedCodeFile = $expectedCodeFile
        FrameworkExt     = $frameworkExt
    }
}
$byBasename = $parsedFiles | Group-Object Basename
```

**How the generator sees files** (`Utilities.cs:113`):

```csharp
// C# reference:
basename = path + Path.DirectorySeparatorChar
         + (filename.IndexOf('.') is var idx && idx < 0
             ? filename
             : filename.Substring(0, idx));
```

| File path | Basename | Culture |
|-----------|----------|---------|
| `C:\src\Containers\Index.razor.resx` | `C:\src\Containers\Index` | neutral |
| `C:\src\Containers\Index.razor.da.resx` | `C:\src\Containers\Index` | da |
| `C:\src\Containers\Index.cshtml.resx` | `C:\src\Containers\Index` | neutral |
| `C:\src\Views\Index.resx` | `C:\src\Views\Index` | neutral |

**0.3 Find matching code file for each group — `Find-CodeFile()`**

```powershell
function Find-CodeFile {
    param($Group, [switch]$Match)
    $neutral = $Group.Group | Where-Object { $_.Culture -eq 'neutral' }
    if (-not $neutral) { if ($Match) { return @() } else { return $null } }
    # For standalone .resx (no framework extension), return file infos
    if ($neutral.Count -eq 1 -and -not $neutral[0].FrameworkExt) {
        $nf = $neutral[0]
        $result = @{ File = $nf; Strategy = 'Standalone'; CodeFiles = @() }
        if ($Match) { return @($result) } else { return $result }
    }
    $results = @()
    foreach ($nf in $neutral) {
        $codeName = $nf.ExpectedCodeFile  # e.g. "Index.razor" or "Resources"
        $acceptedExtensions = @('cs', 'razor', 'cshtml', 'aspx', 'ascx', 'master', 'xaml')
        $codeFiles = @()
        foreach ($ext in $acceptedExtensions) {
            $testPath = Join-Path $nf.Directory "$codeName.$ext"
            if (Test-Path $testPath) {
                $item = Get-Item $testPath
                $matchType = if ($item.Name -ieq "$codeName.$ext") { 'exact' } else { 'case-mismatch' }
                $codeFiles += @{ Path = $item.FullName; Name = $item.Name; MatchType = $matchType }
            }
        }
        if (-not $codeFiles) {
            $dirCodeFiles = Get-ChildItem $nf.Directory -File |
                Where-Object { $_.Extension -in ($acceptedExtensions | ForEach-Object { ".$_" }) -and $_.Name -notlike '*.resx' }
            $matched = @($dirCodeFiles | Where-Object { $_.BaseName -ieq $codeName })
            foreach ($f in $matched) {
                $matchType = if ($f.Name -ceq "$codeName$($f.Extension)") { 'exact' } else { 'case-mismatch' }
                $codeFiles += @{ Path = $f.FullName; Name = $f.Name; MatchType = $matchType }
            }
        }
        $strategy = if ($nf.FrameworkExt) { "Framework: $($nf.FrameworkExt)" }
                   elseif ($codeFiles.Count -eq 0) { 'No code file found' }
                   elseif ($codeFiles[0].MatchType -eq 'case-mismatch') { 'Code-behind (case mismatch)' }
                   else { 'Code-behind' }
        $result = @{ File = $nf; Strategy = $strategy; CodeFiles = $codeFiles }
        $results += $result
    }
    if ($Match) { return $results } else { return $results[0] }
}
```

**How the generator discovers code files**: The generator watches Compilation SyntaxTrees for `[ResxSettings]` attributes and matches them against the basename map. For standalone `.resx` files (no framework extension), the generator creates a standalone resource class. For files with framework extensions (`.razor`, `.cshtml`, etc.), the generator expects a matching code file. `Find-CodeFile()` models this by checking existence of `{ExpectedCodeFile}.{ext}` for each known framework extension.

Output per group:

```
[Group: C:\src\Containers\Index]
  Neutral files:
    Containers\Index.razor.resx   → Framework=razor  → CodeFile=Index.razor [exact]  Strategy=Framework: razor
    Containers\Index.cshtml.resx   → Framework=cshtml → CodeFile=Index.cshtml [exact]  Strategy=Framework: cshtml
  ⚠ COLLISION: 2 neutral files with different framework extensions in same directory

[Group: C:\src\Views\Index]
  Neutral files:
    Views\Index.resx               → Standalone (no framework ext)
  Strategy=Standalone

[Group: C:\src\Components\Component]
  Neutral files:
    Components\Component.razor.resx → Framework=razor → CodeFile=Component.razor [exact]  Strategy=Framework: razor
```

**Classification table** (CF = Code File; NF = Neutral File):

| Rule | Condition | Category | Setting priority | Collision? |
|------|-----------|----------|------------------|------------|
| A | NFs with no matching CF (no `.razor`, `.cshtml`, `.as?x`, `.cs`) | Standalone library | `UseResManager` → ask user | No |
| B | Collision: same directory, same first-dot name, different CF types | Collision group | N/A (must be resolved) | YES |
| C | Every NF matches one CF via `.razor` | Blazor dedicated | `InnerClassVisibility=private`, `PublicClass=true` | No |
| D | Every NF matches one CF via `.cshtml` | Razor Pages dedicated | `ClassNamePostfix=Model`, `StaticMembers=false`, `PublicClass=true` | No |
| E | Every NF matches one CF via `.as?x` | WebForms dedicated | `InnerClassVisibility=protected`, `PublicClass=true` | No |
| F | Matches CF via `.cs` only | Code-behind | `PartialClass=true` | No |
| G | Ambiguous: basename could match multiple CFs in different directories | Ambiguous | Explicit `DependentUpon` per file | Possible |

**0.4 Collision + case-mismatch detection**

```powershell
Write-Output "=== Collision Detection ==="
foreach ($group in $byBasename) {
    $results = Find-CodeFile -Group $group -Match
    $strategies = ($results | ForEach-Object { $_.Strategy } | Select-Object -Unique)
    if ($strategies.Count -gt 1 -and ($strategies -match 'Framework').Count -gt 1) {
        Write-Output "⚠ COLLISION: $($group.Name)"
        foreach ($r in $results) {
            $cfList = ($r.CodeFiles | ForEach-Object { "$($_.Name) [$($_.MatchType)]" }) -join ', '
            Write-Output "    $($r.File.RelativePath) → $($r.Strategy) → $cfList"
        }
    }
}

Write-Output "=== Case-Mismatch Detection ==="
foreach ($group in $byBasename) {
    $results = Find-CodeFile -Group $group -Match
    foreach ($r in $results) {
        foreach ($cf in $r.CodeFiles) {
            if ($cf.MatchType -eq 'case-mismatch') {
                Write-Output "⚠ CASE MISMATCH: $($r.File.RelativePath) → $($cf.Name) (case differs)"
            }
        }
    }
}
```

**0.5 Aggregate summary**

```powershell
$patternCounts = @{ Blazor = 0; RazorPages = 0; WebForms = 0; Standalone = 0; Collision = 0; CodeBehind = 0 }
foreach ($group in $byBasename) {
    $results = Find-CodeFile -Group $group -Match
    $strategies = ($results | ForEach-Object { $_.Strategy } | Select-Object -Unique)
    $frameworkStrats = @($strategies | Where-Object { $_ -like 'Framework:*' })
    if ($frameworkStrats.Count -gt 1) { $patternCounts.Collision++ }
    elseif ($frameworkStrats -like '*razor*') { $patternCounts.Blazor++ }
    elseif ($frameworkStrats -like '*cshtml*') { $patternCounts.RazorPages++ }
    elseif ($frameworkStrats -like '*aspx*' -or $frameworkStrats -like '*ascx*' -or $frameworkStrats -like '*master*') { $patternCounts.WebForms++ }
    elseif ($strategies -contains 'Code-behind') { $patternCounts.CodeBehind++ }
    else { $patternCounts.Standalone++ }
}
Write-Output "=== Pattern Distribution ==="
$patternCounts.GetEnumerator() | Where-Object { $_.Value -gt 0 } | Sort-Object Name | ForEach-Object { Write-Output "  $($_.Key): $($_.Value)" }

$languages = @{}
foreach ($group in $byBasename) {
    foreach ($f in $group.Group) { if ($f.Culture -ne 'neutral') { $languages[$f.Culture] = 1 } }
}
if ($languages.Count -gt 0) { Write-Output "  Languages: $($languages.Keys -join ', ')" } else { Write-Output "  No culture files" }
```

**0.6 Recommendations**

| Finding | Action |
|---------|--------|
| ⛔ **Collision groups** | **HALT — BLOCKER.** Ask user. Generator picks first valid, drops rest with diagnostic 004. |
| ⚠ **Case mismatches** | Ask user which casing. Add explicit `DependentUpon` per file in `.csproj`. |
| ✓ **Standalone** | Global settings from Step 1. No user question needed. |
| ✓ **Framework-dedicated** | Per-file-type overrides (Step 4). No user question needed. |
| ✓ **Languages** | Confirm with user. Feed into SKILL.md placeholder. |

```
         ┌──────────────────────────────────────┐
         │     Run 0.1–0.5 discovery scripts    │
         └────────┬─────────────────────────────┘
                  │
    ┌─────────────┼─────────────────┐
    ▼             ▼                  ▼
┌─────────┐ ┌──────────────┐ ┌──────────────────┐
│Collision│ │Case mismatch │ │Clean groups       │
│BLOCKER  │ │⚠ Ask user    │ │✓ Apply rules A–G  │
└────┬────┘ └──────┬───────┘ └────────┬─────────┘
     │              │                  │
     ▼              ▼                  ▼
 ┌────────┐  ┌────────────┐  ┌───────────────────┐
 │ HALT   │  │ Explicit   │  │ Per-file-type      │
 │ Resolve│  │ Dependent  │  │ overrides (Step 4) │
 │ re-run │  │ Upon in    │  │ or global settings │
 │ disc.  │  │ .csproj    │  │ (Step 1)           │
 └────────┘  └────────────┘  └───────────────────┘
```

**0.7 Existing config check**

```powershell
$existingConfigs = @()
if (Test-Path Directory.Build.props) {
    $propsContent = Get-Content Directory.Build.props -Raw
    $matches = [regex]::Matches($propsContent, 'ResXFileCodeGenerator[^<\s]+')
    foreach ($m in $matches) { $existingConfigs += "Directory.Build.props: $($m.Value)" }
}
if (Test-Path Directory.Build.targets) {
    $targetsContent = Get-Content Directory.Build.targets -Raw
    $matches = [regex]::Matches($targetsContent, 'ResXFileCodeGenerator[^<\s]+|GenerateResource|PreventMSB3030|DependentUpon')
    foreach ($m in $matches) { $existingConfigs += "Directory.Build.targets: $($m.Value)" }
}
Get-ChildItem -Recurse -Filter *.csproj | ForEach-Object {
    if (Select-String -Path $_.FullName -Pattern 'Catglobe.ResXFileCodeGenerator' -Quiet) {
        $existingConfigs += "PackageRef: $($_.FullName.Replace($cwd + '\', ''))"
    }
}
$existingConfigs = $existingConfigs | Select-Object -Unique
if ($existingConfigs) {
    Write-Output "=== Existing Configurations ==="
    $existingConfigs | ForEach-Object { Write-Output "  $_" }
} else { Write-Output "No existing ResXFileCodeGenerator configuration found." }
```

**0.8 Orphaned config detection**

```powershell
$orphanedPatterns = @()
if (Test-Path Directory.Build.targets) {
    $targetsContent = Get-Content Directory.Build.targets -Raw
    $knownTypes = @('razor', 'cshtml', 'aspx', 'ascx', 'master')
    foreach ($type in $knownTypes) {
        if ($targetsContent -match "\.$type\.resx") {
            $hasFiles = ($byBasename | Where-Object {
                $_.Group | Where-Object { $_.RelativePath -like "*.$type.resx" }
            }).Count -gt 0
            if (-not $hasFiles) { $orphanedPatterns += "*.$type.resx" }
        }
    }
}
if ($orphanedPatterns) {
    Write-Output "=== Orphaned Config ==="
    foreach ($p in $orphanedPatterns) { Write-Output "  Rule for $p found but no matching .resx files — keep or remove?" }
}
# UNC path warning
$uncPaths = ($resxFiles | Where-Object { $_.FullName -like '\\*' }).Count
if ($uncPaths -gt 0) {
    Write-Output "UNC/network paths detected ($uncPaths files). Generator may behave unexpectedly."
    Write-Output "Verify paths resolve on the build machine."
}
```

---

### Step 1: Ask configuration questions

After presenting discovery findings, ask only the questions discovery couldn't answer. Each question frames around the user's outcome in their own language — no MSBuild terminology in the prompt text. The technical column is for the agent to map answers to flags, not to show the user.

**Discovery summary to present first:**

> "Here's what I found in your project:
> - __P_STANDALONE__ standalone `.resx` files (shared resources)
> - __P_BLAZOR__ `.razor.resx` files (component-scoped)
> - __P_CSHTML__ `.cshtml.resx` files (page-scoped)
> - __P_WEBFORMS__ `.as?x.resx` files (legacy WebForms)
> - __P_COLLISION__ potential filename collisions to resolve
> - __P_CASE__ case-mismatch files that need explicit linking
> - __P_LANGUAGES__ languages detected: __LANG_LIST__"

If any count is zero, omit that bullet. If collisions exist, present them as blocking — resolution is required before continuing. Map each pattern to its consequence in user terms:

| Pattern found | What this means for you | Settings that take effect automatically |
|---------------|------------------------|----------------------------------------|
| Blazor (`.razor.resx`) | Each component has its own private translation bundle — scoped, not shared across components | `InnerClassVisibility=private`, `PublicClass=true` |
| Razor Pages (`.cshtml.resx`) | Page-scoped translations accessible as `@Model.Resources.Key` — one bundle per page model | `ClassNamePostfix=Model`, `StaticMembers=false`, `PublicClass=true` |
| WebForms (`.as?x.resx`) | Protected translations reachable from the code-behind class — not public to external code | `InnerClassVisibility=protected`, `PublicClass=true` |
| Standalone (`.resx` only) | Shared translation library usable anywhere in the project | Default settings (customizable below) |

#### 🚀 Q0: Confirm — does this match your project?

> "That's what I found. Before I proceed: does this match what you expect? Any patterns missing, anything I got wrong?"

Always ask after presenting discovery. The user confirms or corrects the findings. This catches misclassifications, missing files, and surprises before configuration begins.

| User says | Action |
|-----------|--------|
| "yes" / "correct" / "looks good" / default | Proceed to Quick Setup (or Q1 if "review each"). Discovery is the single source of truth for all subsequent steps. |
| "no" + correction | Re-run Step 0.0-0.5 discovery scripts with corrected scope. Then re-present. |

Also, if zero `.resx` files were found across a multi-project solution (from Step 0.0 project scan), ask:
> "I didn't find any `.resx` files yet. You have __PROJECT_COUNT__ projects — which should I set up for translations?"
> "add to all" → install package everywhere. A list → only those. "none for now" → abort.

After confirmation, proceed to the Quick Setup Path and Q1–Q5, scoped to the confirmed projects.

---

#### 🚀 Quick Setup Path

> "I'll set up fast code-gen for everything by default — translations compiled into your assembly, ~600% faster, zero runtime overhead, no satellite DLLs. Project-local visibility (internal), automatic build-error prevention. Want me to configure it all now?
> 1. **Yes, configure everything** — proceed to installation
> 2. **Let me review each setting** — walk me through one by one"

| User says | Action |
|-----------|--------|
| "1" / "yes" / "configure" / "go" | Set `UseResManager=true`, `PublicClass=false`, `UseDefaults=true`, dual-mode `GenerateResource`. Skip to Step 2. |
| "2" / "no" / "review" / "walk" / "choose" | Proceed through Q1–Q5 below. |

Code-gen, project-local visibility, and automatic build-error prevention are always the defaults — they apply without asking. If the user chose path 2, ask only the questions below that discovery couldn't answer.

---

#### Q1: Any projects keeping the old ResourceManager?

> "Code-gen is the default everywhere. Do any of your projects need to keep using the standard `ResourceManager` instead?
> 
> You'd keep ResourceManager if:
> - You use `IStringLocalizer<T>` in an ASP.NET Core app (needs ResourceManager infrastructure)
> - Your app sets `Thread.CurrentThread.CurrentUICulture` to a custom culture at runtime
> - A project has existing satellite DLL deployment pipelines you can't change right now
> 
> If none of those apply, all projects use code-gen — no further questions needed."

| User says | Action |
|-----------|--------|
| "no" / "none" / "all code-gen" / default | All projects use code-gen (`UseResManager=true`). Nothing to configure. |
| A list of project names / paths | Only those projects get `UseResManager=false` override. All others get code-gen. Map each: `<EmbeddedResource>` per-file or per-project override. |

> **Naming callout**: `UseResManager=true` means "use **code-gen** mode" (bake strings into assembly). The flag is named `UseResManager` for historical compatibility — it does NOT mean "use ResourceManager." Think of it as "UseResManager-replacement."

**Skip entirely if**: Zero `.resx` files found — or the user confirms no ResourceManager needs (default: skip, code-gen assumed).

---

#### Q2: How are your resources organized?

> "How do you want your resources structured?
> 1. **Per-class** — One `.resx` file paired with each component/page. Resources are private to that class. Typical for Blazor/Razor Pages.
> 2. **Per-module** — One or a few `.resx` files per project. Shared internally by all classes in the assembly. Typical for libraries and apps.
> 3. **Global** — A dedicated shared-resource project. Other projects reference it. Typical for multi-project solutions with consistent terminology.
> 4. **Mixed** — Some per-class, some per-module, maybe a global library. Happy with discovery defaults — let the detected patterns drive it."

| User says | What it means | Maps to |
|-----------|--------------|---------|
| "1" / "per-class" / "component" | Each .resx is private to its code file. Framework files already follow this pattern. New standalone .resx become inner classes. | Per-file: `InnerClassVisibility=private`, `PublicClass=true` where framework-detected |
| "2" / "per-module" / "project" (default) | Standalone .resx stay internal. Resources shared within the assembly, not outside. | `PublicClass=false` on standalone. Framework files unchanged. |
| "3" / "global" / "shared" | A dedicated project/library with public resources. Other projects reference it. | `PublicClass=true` on the shared project. Others internal. |
| "4" / "mixed" / "defaults" | Discovery patterns are correct. Framework files get their overrides, standalone stay internal. | No change from discovery defaults. |

**Skip if**: Pre-existing `PublicClass` or `InnerClassVisibility` found in project files. Otherwise always ask — the user should choose their resource organization style explicitly, not guess at visibility flags.

---

#### Q3: What languages do you support and which is the default?

Two parts. Order matters — always ask which is the default, never guess.

**Part 1 — All languages:**

If discovery found languages on disk:
> "I found __DETECTED_COUNT__ languages already on disk: **__DETECTED_CULTURES__**.
>
> What additional languages do you plan to support?"

If no languages on disk:
> "Which languages do you plan to support? (e.g., da, vi, fr, de — I'll handle naming conventions)"

**Part 2 — Default language** (always ask, never guess):

> "Which of those is the **default**? (goes in the neutral `.resx` without a culture suffix)"

Map their answer: the default language → neutral `Name.resx`. All others → `Name.{culture}.resx`.

Example: user says "Danish, German, Greek" → ask "Which is the default?" → they answer "Danish" → Danish = `Resources.resx` (neutral), German = `Resources.de.resx`, Greek = `Resources.el.resx`.

| Part | Maps to |
|------|---------|
| Default language | Neutral `.resx` file (no culture suffix), `__DEFAULT_LANGUAGE__` in SKILL.md |
| All other languages | `Name.{culture}.resx` files, `__LANGUAGE_FILE_LIST__` in SKILL.md |

---

#### Q4: Create a SKILL.md for AI agents?

Two steps: whether to create one, and where to save it.

**Step A — Opt-in:**

> "I can save this project's ResXFileCodeGenerator configuration as a skill file so AI agents remember these settings for future .resx work — patterns, languages, file paths and all. Should I create one?"
> 1. **Yes** — proceed to Step B (choose where).
> 2. **No** — skip to Step 7.

**Step B — Location:**

> "Where should I save the skill file?
> 1. **Project** — Shared with your team. Lives in the repo.
> 2. **Local** — Just for you. Lives in your AI agent's config."

| Step A | Step B | Action |
|--------|--------|--------|
| "no" / "skip" | N/A | Skip Step 6 entirely. |
| "yes" / "create" | "project" / "repo" / "team" | Save to a project-level location your agent recognizes (e.g., project-local skill directory). |
| "yes" / "create" | "local" / "just me" / "private" | Save to your agent's user-level skill/config directory. |
| "yes" / "create" | (no preference) | Default → project if repo detected, local otherwise. |

**Skip if**: No supported AI agent detected (no known skill directories) AND no repo detected. Otherwise always ask — skill files are lightweight and useful.

---

#### Q5: Any non-standard behavior you need?

> "The defaults work for most projects. Before we wrap up, do you need any of these uncommon behaviors?
> - Resources accessed on a per-instance basis (not static)? — Rare: some older ASP.NET patterns
> - Custom class names or suffixes for generated code? — Rare: name conflicts with existing types
> - Resources nested inside another class? — Rare: already handled by framework patterns
> - Null-forgiving operators on resource returns? — Rare: only if you treat missing keys as programmer errors
> 
> Or just keep the defaults — that's what most projects use."

| User says | Action |
|-----------|--------|
| "no" / "defaults" / "done" / enter | Keep all defaults. Nothing more to configure. |
| "yes" + which one(s) | Only configure those specific options. Reference the table below per setting. |

**Reference table — only if user asked for a specific override:**

| What it does | Global MSBuild Property | Default | Consequence of changing it |
|-------------|------------------------|---------|---------------------------|
| Static class: access via `Resources.Key` without `new` | `ResXFileCodeGenerator_StaticClass` | `true` | Set to `false` if you need per-instance state or non-static inner class access |
| Static members: each string is a direct property | `ResXFileCodeGenerator_StaticMembers` | `true` | Set to `false` when translations belong to a specific object instance |
| Extendable class: add your own methods alongside generated code | `ResXFileCodeGenerator_PartialClass` | `false` | Set to `true` when you need custom logic in the resource class (e.g., fallback methods) |
| Null-safe returns: suppresses `?` on string return types | `ResXFileCodeGenerator_NullForgivingOperators` | `false` | Set to `true` if you treat missing keys as programmer errors (adds `!` operator) |
| Class name suffix: e.g., `ResourcesModel` not `Resources` | `ResXFileCodeGenerator_ClassNamePostfix` | `""` | Set to `"Model"` for Razor Pages to match the code-behind naming convention |
| Nested inner class: resources inside another class | `ResXFileCodeGenerator_InnerClassVisibility` | `NotGenerated` | Values: `Public`/`Internal`/`Private`/`Protected`/`SameAsOuter` — scopes who can access |
| Inner class name: override default `"Resources"` | `ResXFileCodeGenerator_InnerClassName` | `""` | Set when you need a specific name (e.g., `"MyResources"`) |
| Instance property: `myPage.Resources.Key` access | `ResXFileCodeGenerator_InnerClassInstanceName` | `""` | Set to create a per-instance property (e.g., `"Resources"` → `myPage.Resources.Key`) |

---

### Step 2: Install NuGet package

For each project discovered to need the package (containing `.resx` files or consuming generated resources):

```bash
dotnet add __PROJECT_PATH__ package Catglobe.ResXFileCodeGenerator
```

Add the PackageReference. Use the variant that matches the project type:

**Library project** (other projects consume this one — prevents generator types from leaking downstream):

```xml
<PackageReference Include="Catglobe.ResXFileCodeGenerator" Version="__VERSION__" />
```

**Application project** (`.exe`, web app, Blazor — nothing consumes it downstream):

```xml
<PackageReference Include="Catglobe.ResXFileCodeGenerator" Version="__VERSION__" />
```

Skip test projects unless they contain `.resx` files. If Central Package Management is detected (`Directory.Packages.props`), use `<PackageVersion>` there instead and omit the `Version` attribute on the `<PackageReference>`.

---

### Step 3: Configure Directory.Build.props (global defaults)

Create or merge into `Directory.Build.props` at the solution root. Merge with existing content — never overwrite unrelated properties. Only include properties the user explicitly chose non-default values for:

```xml
<Project>
  <PropertyGroup>
    <ResXFileCodeGenerator_UseResManager>__USE_RES_MANAGER_VALUE__</ResXFileCodeGenerator_UseResManager>
    <ResXFileCodeGenerator_PublicClass>__PUBLIC_CLASS_VALUE__</ResXFileCodeGenerator_PublicClass>
    <ResXFileCodeGenerator_UseDefaults>__USE_DEFAULTS_VALUE__</ResXFileCodeGenerator_UseDefaults>
    <!-- Add only properties that differ from defaults -->
  </PropertyGroup>
</Project>
```

Replace each `__...__` with the value from Step 1. Omit lines where the user chose the default.

---

### Step 4: Configure Directory.Build.targets (overrides and targets)

Create or merge into `Directory.Build.targets` at the solution root. Include ONLY the overrides matching patterns discovered in Step 0.

**4a. Per-file-type overrides** (only for detected patterns):

Start the file:

```xml
<Project>
  <ItemGroup>
```

If discovery found `.razor.resx` files:

```xml
    <!-- Blazor: resources are private inner classes -->
    <EmbeddedResource Update="**/*.razor.resx">
      <PublicClass>true</PublicClass>
      <InnerClassVisibility>private</InnerClassVisibility>
    </EmbeddedResource>
```

If `.cshtml.resx` files:

```xml
    <!-- Razor Pages: non-static, inner class with instance -->
    <EmbeddedResource Update="**/*.cshtml.resx">
      <ClassNamePostfix>Model</ClassNamePostfix>
      <StaticMembers>false</StaticMembers>
      <PublicClass>true</PublicClass>
      <InnerClassInstanceName>Resources</InnerClassInstanceName>
      <InnerClassName>MyResources</InnerClassName>
    </EmbeddedResource>
```

If `.as?x.resx` files:

```xml
    <!-- Legacy ASP.NET WebForms: protected inner class -->
    <EmbeddedResource Update="**/*.as?x.resx">
      <PublicClass>true</PublicClass>
      <InnerClassVisibility>protected</InnerClassVisibility>
    </EmbeddedResource>
```

If discovery found **ambiguous or colliding basenames**, add explicit `DependentUpon` entries per file instead of relying on the wildcard pattern.

**4b. GenerateResource** (always dual-mode):

```xml
    <!-- Disable built-in resgen.exe in Release: code-gen handles all resource output.
         Keep resgen.exe in Debug: Visual Studio's resource designer, IntelliSense,
         and incremental build caching need it. Removing these lines breaks Debug builds. -->
    <EmbeddedResource Update="@(EmbeddedResource)" Condition="'$(Configuration)' == 'Release'">
      <GenerateResource>false</GenerateResource>
    </EmbeddedResource>
    <EmbeddedResource Update="@(EmbeddedResource)" Condition="'$(Configuration)' != 'Release'">
      <GenerateResource>true</GenerateResource>
    </EmbeddedResource>
```

> `GenerateResource=true` in Debug keeps Visual Studio's resource designer, IntelliSense cache, and incremental builds working. Without it, a fresh clone's first Debug build can fail because VS's resource caching pipeline expects `resgen.exe` output even though the source generator also produces the resources.

**4c. Culture-specific file linking:**

```xml
    <!-- Link .{culture}.resx files to their neutral parent .resx.
         Without this, each culture file becomes its own resource class.
         Culture files like Messages.da.resx are nested under Messages.resx. -->
    <EmbeddedResource Update="**/*.??.resx;**/*.??-??.resx">
      <DependentUpon>$([System.IO.Path]::GetFileNameWithoutExtension('%(FileName)')).resx</DependentUpon>
    </EmbeddedResource>
```

**4d. PreventMSB3030 + PreventNU5026** (automatic by default):

`UseDefaults=true` is the default. The NuGet package's own `.props` file handles PreventMSB3030 and PreventNU5026 targets automatically — **skip this section entirely**.

If `UseDefaults` was explicitly set to `false` (non-default, user must have specifically requested it), add these targets manually:

```xml
  </ItemGroup>

  <!-- Prevent MSB3030: "Copying file ... because it was not found."
       Code-gen doesn't produce satellite DLLs, but the build system expects them.
       Removing this causes build failures on every Release build. -->
  <Target Name="PreventMSB3030" DependsOnTargets="ComputeIntermediateSatelliteAssemblies"
          BeforeTargets="GenerateSatelliteAssemblies" Condition="'$(Configuration)' == 'Release'">
    <ItemGroup>
      <IntermediateSatelliteAssembliesWithTargetPath Remove="@(IntermediateSatelliteAssembliesWithTargetPath)" />
    </ItemGroup>
  </Target>
  <!-- Prevent NU5026: NuGet packaging step expects satellite assemblies.
       Removing this causes NuGet package creation to fail. -->
  <Target Name="PreventNU5026" DependsOnTargets="SatelliteDllsProjectOutputGroup"
          BeforeTargets="_GetBuildOutputFilesWithTfm" Condition="'$(Configuration)' == 'Release'">
    <ItemGroup>
      <SatelliteDllsProjectOutputGroupOutput Remove="@(SatelliteDllsProjectOutputGroupOutput)" />
    </ItemGroup>
  </Target>
</Project>
```

---

### Step 5: Per-project .csproj additions

For each project needing the package, add or ensure the PackageReference exists per Step 2. For projects with case-mismatch issues (detected in Step 0), add explicit `EmbeddedResource Update` entries for the affected files:

```xml
<ItemGroup>
  <EmbeddedResource Update="__RELATIVE_PATH__/stepmenu.ascx.resx">
    <DependentUpon>StepMenu.ascx</DependentUpon>
  </EmbeddedResource>
</ItemGroup>
```

---

### Step 6: Install SKILL.md (agent-agnostic)

Continuation from Q4 in Step 1. If the user opted out, skip to Step 7.

**6a. Get the template** from `templates/resx-skill-template.md` in this repository. The template contains all sections with `__PLACEHOLDER__` variables ready to fill.

Fetch the template from:
```
https://raw.githubusercontent.com/Catglobe/ResXFileCodeGenerator/main/templates/resx-skill-template.md
```

Or read it directly from the repo if already cloned:
```
templates/resx-skill-template.md
```

**6b. Fill in every `__PLACEHOLDER__`** with actual values from previous steps. The template is a HOW-TO skill for daily .resx work — it focuses on translation workflows, not MSBuild settings. For sections where nothing applies, write "None."

**6c. Save the filled template.** Determine the destination based on the user's location choice:

- **Project**: Save to a skill/rules directory your agent recognizes at the project level. Each agent has its own convention — use yours.
- **Local**: Save to your agent's user-level skill/config directory. Again, follow your agent's conventions — you know where your skills live better than this guide does. If you are unsure, search for your agent's skill directory conventions, or ask the user which AI agent they're using.

**Quick reference — key placeholders:**

| Placeholder | Fill with | Example |
|-------------|-----------|---------|
| `__PROJECT_NAME__` | Solution or project name | `CatGlobe` |
| `__DEFAULT_LANGUAGE__` | Neutral .resx language | `English (en)` |
| `__LANGUAGE_FILE_LIST__` | Each language → its file | `- English → Resources.resx (neutral)\n- Spanish → Resources.es.resx\n- Greek → Resources.el.resx` |
| `__RESX_DIRECTORY_EXAMPLE__` | Where .resx files live | `Resources` |
| `__PATTERN_SECTION__` | Project patterns from discovery | `- 15 Blazor\n- 8 Standalone` |

---

### Step 7: Verify

```bash
dotnet restore && dotnet build
```

Verify the build completes with zero errors. Run `dotnet test` if the project has tests.

**Common post-setup issues:**

| Symptom | Likely cause | Action |
|---------|-------------|--------|
| MSB3030 error | Missing PreventMSB3030 target | Verify Step 4d or set `UseDefaults=true` |
| Duplicate generation | `GenerateResource` not set to `false` in Release | Verify Step 4b |
| Basename collision (diagnostic 004) | Two `.resx` files in same directory share first-dot name | Add explicit `DependentUpon` or rename one set |
| No generated class for case-mismatch files | Wildcard `DependentUpon` can't match different case | Add explicit per-file `DependentUpon` |

---

### Step 8: Idempotency

Re-running is safe:

- **Config files**: Merge with existing content — properties are not overwritten, only added if missing.
- **NuGet package**: `dotnet add package` is a no-op if already installed. Version upgrades are explicit.
- **SKILL.md**: Overwritten with latest config values from `templates/resx-skill-template.md`. Each run updates the filled template with the most recent discovery data.
- **Build**: A clean build (`dotnet clean && dotnet build`) regenerates everything from source.

---

## Appendices

### Configuration Reference

All available MSBuild properties and their defaults:

| Setting | Global MSBuild Property | Type | Default | Description |
|---------|------------------------|------|---------|-------------|
| UseResManager | `ResXFileCodeGenerator_UseResManager` | bool | `false` | `true` = fast code-gen, `false` = ResourceManager |
| UseDefaults | `ResXFileCodeGenerator_UseDefaults` | bool | `false` | `true` = enable PreventMSB3030 and PreventNU5026 targets automatically |
| PublicClass | `ResXFileCodeGenerator_PublicClass` | bool | `false` | Generated class is `public` (vs `internal`) |
| NullForgivingOperators | `ResXFileCodeGenerator_NullForgivingOperators` | bool | `false` | Adds `!` to suppress nullable warnings |
| StaticClass | `ResXFileCodeGenerator_StaticClass` | bool | `true` | Generated class is `static` |
| StaticMembers | `ResXFileCodeGenerator_StaticMembers` | bool | `true` | Members are `static` |
| PartialClass | `ResXFileCodeGenerator_PartialClass` | bool | `false` | Generated class is `partial` |
| ClassNamePostfix | `ResXFileCodeGenerator_ClassNamePostfix` | string | `""` | Appended to generated class name |
| InnerClassVisibility | `ResXFileCodeGenerator_InnerClassVisibility` | Visibility | `NotGenerated` | Nest class: `Public`, `Internal`, `Private`, `Protected`, `SameAsOuter`, `NotGenerated` |
| InnerClassName | `ResXFileCodeGenerator_InnerClassName` | string | `""` | Inner class name (default: `"Resources"`) |
| InnerClassInstanceName | `ResXFileCodeGenerator_InnerClassInstanceName` | string | `""` | Instance property name for inner class access |

All global properties can be overridden per-file using `EmbeddedResource` item metadata with the same name (without the `ResXFileCodeGenerator_` prefix). Example: `<PublicClass>true</PublicClass>` on an `<EmbeddedResource>` element.

The `Visibility` enum values: `NotSet` (-1), `NotGenerated` (0), `Public`, `Internal`, `Private`, `Protected`, `SameAsOuter`.

### Mode Comparison

| Aspect | ResourceManager (`UseResManager=false`) | Code-Gen (`UseResManager=true`) |
|--------|------------------------------------------|----------------------------------|
| Lookup speed | ~33ns | ~5ns (600% faster) |
| Allocations | Per lookup | Zero |
| Satellite DLLs | Required | None |
| Custom CultureInfo | Supported | Not supported |
| Cold start | High (lazy load penalty) | None |
| Linker optimization | Not possible | Full (tree-shaking) |
| Assembly size | Smaller main DLL + satellite DLLs | Larger main DLL, no satellite DLLs (up to 50% total savings) |
| Build time | Slow (~150ms per `.resources` file) | Fast |

### Troubleshooting

#### Basename Collision (Diagnostic 004)

The generator uses `BasenameFromPath` which combines the file's **directory** with the name truncated at the **first dot**:

- `Pages/Index.razor.resx` → basename `Pages\Index`
- `Pages/Index.cshtml.resx` → basename `Pages\Index`
- `Pages/Index.resx` → basename `Pages\Index`

All three are in the same directory and share the same first-dot name — they collide. Files in different directories (e.g., `Pages/Index.resx` and `Views/Index.resx`) have different basenames and do NOT collide.

**Fix**: Use explicit `DependentUpon` entries in `.csproj` for each affected file, or rename files to avoid collision (e.g., `Pages/PagesIndex.razor.resx`, `Pages/ViewsIndex.cshtml.resx`).

#### MSB3030 / MSB3030: "Copying file ... because it was not found"

The build system expects satellite assemblies that no longer exist (because code-gen mode doesn't produce them). Add `PreventMSB3030` from Step 4d, or set `ResXFileCodeGenerator_UseDefaults=true` in `Directory.Build.props` to have the NuGet package handle it automatically.

#### NU5026: "The file ... does not exist in the package"

Same root cause as MSB3030 — references to satellite assemblies in the NuGet packaging step. Add `PreventNU5026` from Step 4d, or enable `UseDefaults=true`.

#### Case Mismatch in Legacy ASP.NET

`stepmenu.ascx.resx` relates to `StepMenu.ascx` (different case). The wildcard `DependentUpon` pattern resolves filenames case-sensitively and will fail to match. Add explicit entries per file:

```xml
<EmbeddedResource Update="path/to/stepmenu.ascx.resx">
  <DependentUpon>StepMenu.ascx</DependentUpon>
</EmbeddedResource>
```

#### Diagnostic IDs

| ID | Severity | Description |
|----|----------|-------------|
| `CatglobeResXFileCodeGenerator001` | Warning | Duplicate member name ignored |
| `CatglobeResXFileCodeGenerator002` | Warning | Member has same name as class |
| `CatglobeResXFileCodeGenerator003` | Error | Cannot have static members/class with class instance |
| `CatglobeResXFileCodeGenerator004` | Warning | Invalid culture in resx set (includes basename collision) |
| `CatglobeResXFileCodeGenerator005` | Warning | Missing neutral (default) file |
| `CatglobeResXFileCodeGenerator006` | Error | Cannot read/parse the resx file |
| `CatglobeResXFileCodeGenerator007` | Warning | Spurious key in satellite file |
| `CatglobeResXFileCodeGenerator008` | Warning | `[ResxSettings]` has no matching `.resx` |
| `CatglobeResXFileCodeGenerator009` | Error | Context classes must be partial |
| `CatglobeResXFileCodeGenerator010` | Error | Problem parsing type symbol |
| `CatglobeResXFileCodeGenerator011` | Error | `ForEnum` references non-enum type |
| `CatglobeResXFileCodeGenerator012` | Info | Enum member has no translation |
