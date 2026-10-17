# Chaos presentation and legacy jewel packs

Run from repository root with local game data:

```powershell
dotnet run --project tests/ChaosPresentation/ChaosPresentation.csproj -p:MonoGameFramework=MonoGame.Framework.DesktopGL -p:UseSharedCompilation=false -- C:\Data_Broyal\Data
```

Checks all ten OpenMU Season 6 packed-jewel IDs, their three levels (10/20/30 jewels), extended packet identity, model availability, cache isolation between single and packed jewels, and the separate Chaos/Trainer layouts.

Chaos uses the existing GFx PersonalShop frame, NpcShop cells and GFx close/action buttons. The layout places recipe summary above the 8x4 grid and materials, warning and combine button below it. No tax rate is invented when the client has not received one; the existing server remains authoritative for mixing.

The supplied modern item.bmd omits group 12 IDs 30, 31 and 136-143. Missing definitions now use explicit OpenMU packed-jewel identities and three-model cluster previews of their respective single jewel. Existing definitions from other Data sets are preserved. These are compatibility previews, not imported Season 6 assets. Inventory/world item identity and footprint remain the server's original 1x1 packed item.

Manual checks: open Chaos next to inventory, inspect item tooltips and combination placement; combine a known valid recipe; reopen Pet Trainer to check its unchanged layout. Inspect packed Bless, Soul and other jewels in inventory, hover and drag them, then compare with loose jewels. Offline checks do not replace live rendering or server verification.
