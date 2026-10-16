# Extended vault

OpenMU provides one vault extension per account: 120 base slots (8x15) and 120 extended slots. Enable `Account.IsVaultExtended` in the server account editor and log out completely, then log in again. The permission arrives in the Season 6 character list. No new server packet or server code modification is required.

The NewUI button above the vault grid switches pages within the same NPC session; both pages share Zen. Disabled accounts see a locked button. The extended page uses absolute Vault storage slots 120-239. Pick an item, change page and click its destination to move it between pages. Moves are confirmed by the existing server response, with one transfer in flight at a time. Large items cannot be newly placed across page boundaries.

Run from repository root:

```powershell
dotnet run --project tests/VaultExtension/VaultExtension.csproj -p:MonoGameFramework=MonoGame.Framework.DesktopGL -p:UseSharedCompilation=false -- C:\Data_Broyal\Data
```

Checks all 240 addresses, locks and boundaries, the real account permission packet, deposit, cross-page moves, rejection, withdrawal and page refresh while holding an item. Uses local game data for item definitions; does not connect to a server.

Manual game checks: enable/disable the account flag and reconnect; open at the vault NPC; inspect the GFx button and page title; deposit and withdraw on both pages; move a multi-cell item between pages; inspect tooltips; close/reopen and reconnect to verify persistence. Test a second account without an extension. UI rendering and live server behavior require in-game verification.
