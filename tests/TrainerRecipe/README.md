# Pet Trainer recipe feedback

Run from repository root:

```powershell
dotnet run --project tests/TrainerRecipe/TrainerRecipe.csproj -p:MonoGameFramework=MonoGame.Framework.DesktopGL -p:UseSharedCompilation=false -- C:\Data_Broyal\Data
```

Checks incomplete spirit selection, material quantities, complete/partial/excess requirement colors, removal and empty-box reset through the production recipe matcher and UI information builder. No server connection or game window is required.

In-game verification: open Pet Trainer > Resurrect Spirit, hover the spirit and jewels to inspect the shared inventory tooltip, place each required material and check green completion, remove a material, and switch between Horse and Raven spirits. Quantities update from confirmed mix-box contents. Extra items and server-specific recipe changes remain subject to server validation. This change does not alter mix packets, base success rates, costs or server recipes.
