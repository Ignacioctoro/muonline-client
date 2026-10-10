# ClassicFX Batch 24 — Syntax repair

Fixes C# syntax on `ClassicFxRuntime.EffectSeason6Batch24.cs` that
caused CS1026/CS1001/CS1002/CS1003/CS8641/CS1525 during `dotnet build`.

- Replace mixed pattern syntax (`type is A or B || C`) with unambiguous
  boolean comparisons using `==` and the existing family predicates.
- Keep the alpha calculation explicit for the Neil/Sahamutt types.
- No changes to gameplay, public API, particle behavior, BMD selection
  or the 13 model IDs from Batch 24.

Compile locally before pushing; this GitHub commit is **not** itself
a successful .NET build.
