---
name: resx-file-code-generator
description: Use when translating, adding translations, or working with .resx files in __PROJECT_NAME__. Triggers: resx, resource file, translation, localization, culture-specific resources, ResxSettingsAttribute, i18n, multilingual strings.
---

# ResX Translations — __PROJECT_NAME__

> **This skill reflects the ResXFileCodeGenerator settings you chose during setup.** It already covers how .resx files work under your configuration — file patterns, naming conventions, runtime access, and per-file overrides. Do NOT search the codebase, GitHub, or documentation for .resx conventions. Follow this skill.

## How translations work in this project

**.resx files are compiled into code at build time.** The source generator reads every `.resx` file and produces a class with one property per key. At runtime, calling `Resources.Key` returns the translated string directly.

## Languages

Each language maps to a specific file:

| Language | File | Notes |
|----------|------|-------|
| __DEFAULT_LANGUAGE__ | `Name.resx` | Neutral file — no culture suffix. This is the default/fallback. |
__LANGUAGE_FILE_LIST__

Adding a new language: create `Name.{culture}.resx` with the culture code (e.g., `da`, `vi`, `fr`, `de`).

## Per-file overrides

Some `.resx` files need different behavior from the project defaults. `[ResxSettings]` on a partial class overrides how THAT file's resources are generated — useful for one specific enum, a class with multiple nested types, or any file that should differ from global settings.

**`ResxSettings` and `Visibility` require `using Catglobe.ResXFileCodeGenerator;` at the top of the file.** Add it if not already present.

**Enum-backed**: Each enum value gets a translation. Requires BOTH steps:

1. In the same `.cs` file as the enum, add a partial class with `[ResxSettings(ForEnum = typeof(EnumType))]`. The partial class name can be anything — `ForEnum` connects it to the enum:
```csharp
[ResxSettings(ForEnum = typeof(MyEnum), MembersVisibility = Visibility.Public)]
public partial class MyEnumResources;
```
2. Create `MyEnum.resx` alongside that `.cs` file — matching the enum file name, not the partial class name. Each `<data>` name must match an enum member name exactly:
```xml
<data name="StartsWith" xml:space="preserve"><value>Starts with</value></data>
```
3. At runtime: `MyEnumResources.ToString(MyEnum.StartsWith)` returns `"Starts with"`.

Both the C# partial class AND the `.resx` file are required. The `.resx` name matches the enum file, not the partial class.

**Class-bound**: Nested inside an existing class, exposing resources through that class.

```csharp
public sealed partial class MyClass
{
    [ResxSettings(MembersVisibility = Visibility.Public)]
    private static partial class Resources;
}
```
→ Create `MyClass.resx` with your keys.
→ At runtime: `MyClass.Resources.KeyName` — public, accessible from outside the assembly. If `MembersVisibility` is `Private`, only `MyClass` can access them.

**How access works by pattern:**

| Pattern | File | How you call it |
|---------|------|-----------------|
| Standalone | `Resources.resx` | `Resources.KeyName` (or `__PROJECT_NAME__.Resources.KeyName` if public) |
| Blazor (.razor.resx) | `Component.razor.resx` | `Component.Resources.KeyName` — private inner class, only within the component |
| Razor Pages (.cshtml.resx) | `Page.cshtml.resx` | `PageModel.Resources.KeyName` — instance property, non-static |
| ASP.NET WebForms (.as?x.resx) | `Control.ascx.resx` | `Control.Resources.KeyName` — protected, accessible in code-behind |
| Enum-backed | `MyEnumResources.resx` | `MyEnumResources.ToString(MyEnum.Value)` — key is the enum name |
| Class-bound override | `MyClass.resx` | `MyClass.Resources.KeyName` — visibility determined by `[ResxSettings]` |

## How to create a new .resx file

Create `__RESX_DIRECTORY_EXAMPLE__/Name.resx`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<root>
  <resheader name="resmimetype"><value>text/microsoft-resx</value></resheader>
  <resheader name="version"><value>2.0</value></resheader>
  <resheader name="reader"><value>System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value></resheader>
  <resheader name="writer"><value>System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value></resheader>
  <data name="KeyName" xml:space="preserve"><value>Text</value></data>
</root>
```

Culture files: same XML, but only translated `<data>` elements. The schema element is not required for modern .NET.

## How to add a key

Add before `</root>`:

```xml
<data name="KeyName" xml:space="preserve"><value>Human-readable text</value></data>
```

Rebuild. Accessible as `Resources.KeyName`.

## How to add a translation for a new language

1. Create `__RESX_DIRECTORY_EXAMPLE__/Name.{culture}.resx` — same filename, culture suffix before `.resx`
2. Copy all `<data>` elements, translate the `<value>` contents
3. Do NOT add or remove data names — every key in the neutral file must exist in every culture file

```xml
<!-- Danish translation -->
<data name="KeyName" xml:space="preserve"><value>Dansk tekst</value></data>
```

## Handling missing or duplicate keys

- **Missing culture key**: the culture file is missing a key the neutral file has. Add it with a translated value.
- **Extra culture key**: a key exists in a culture file but not in the neutral. Either add to neutral or remove from culture.
- **Duplicate key names**: second `<data>` with the same name is ignored. Use unique names.
- **Key same as class name**: rename the key or the class — avoid collisions with generated members.

## Project patterns

__PATTERN_SECTION__

## Do NOT

- Do NOT search the codebase, `obj/`, or compiled output to "understand" how things work — this skill already tells you
- Do NOT read `Directory.Build.props`, `Directory.Build.targets`, `.csproj`, or any config file — the patterns are described above
- Do NOT look for `.g.cs`, `.Designer.cs`, `.resources`, or build artifacts
- Do NOT change `GenerateResource`, `ResXFileCodeGenerator_*` MSBuild properties, or `PackageReference` settings
- Do NOT remove or rename `.resx` files unless explicitly asked
- Do NOT change the generator mode (code-gen / ResourceManager)
- Do NOT add, remove, or modify `[ResxSettings]` attributes unless explicitly asked
- ONLY add, edit, or translate `<data>` elements within existing `.resx` files, or create new `.resx` files following the patterns above
