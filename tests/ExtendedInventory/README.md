# Extended inventory regression checks

Run from the repository root:

```powershell
dotnet run --project tests/ExtendedInventory/ExtendedInventory.csproj -p:MonoGameFramework=MonoGame.Framework.DesktopGL -- C:\Data_Broyal\Data
```

Uses the production layout, occupancy checks, state and 0x24 packet handler. No game window or server is required. The optional argument points to the local MU Data directory.

OpenMU sends `Character.InventoryExtensions` in character information. Each unlocked extension adds 32 slots: 76�107, 108�139, 140�171 and 172�203. Both sides of a move use storage `Inventory`; personal store starts at 204. Multi-cell items cannot cross the 8x4 boundary of an extension. No open-window packet or server code modification is needed. A character with zero extensions sees locked blocks. Change the character's server-side `InventoryExtensions` configuration (0�4) and reconnect to use the new allocation.

Manual game checks: open via E, move a multi-cell item in both directions by clicking and dragging, move between pages with an item picked, cancel pickup with right click, verify locked blocks reject drops, inspect a tooltip and rotating hover preview, consume a potion with right click, close/reopen during a pending move, and reconnect to verify saved positions.
