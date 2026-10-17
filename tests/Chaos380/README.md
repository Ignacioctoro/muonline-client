# Chaos Goblin mode selection and NewUI dialogs

OpenMU Season 6 registers crafting 36 on the Chaos Goblin. It requires compatible equipment with a Guardian option definition, no existing Guardian option, level +4 through +15 and an additional Option, plus one Jewel of Guardian and one Jewel of Harmony at +0. Base cost is 10,000,000 Zen; base success is 50% for +4..+6, 60% for +7..+9 and 70% for +10..+15. Failure preserves equipment and consumes jewels. Live server configuration and taxes remain authoritative.

Opening the NPC now presents Regular Combination / Item 380 Option. Escape cancels and closes the NPC session; selecting either choice retains the same session and uses the existing storage and crafting packets. Regular evaluation excludes the 380 recipe group. The client uses its Data recipe compatibility rules and additionally rejects already-upgraded Guardian items and invalid ingredient levels.

RequestDialog confirmations and information notices share the NewUI GFx inset and button skins. Explicit newlines are retained and Chaos confirmation no longer sends literal backslash-n text.

```powershell
dotnet run --project tests/Chaos380/Chaos380.csproj -p:MonoGameFramework=MonoGame.Framework.DesktopGL -p:UseSharedCompilation=false -- C:\Data_Broyal\Data
```

Offline checks cover levels, success rates, costs, missing/extra/upgraded jewels, existing Guardian options, regular-mode isolation and dialog wrapping. In game, verify selector cancellation, both modes, valid crafting against OpenMU, rejected ingredients, NewUI button states and warning/dialog click blocking. No database or server configuration changes are included.
