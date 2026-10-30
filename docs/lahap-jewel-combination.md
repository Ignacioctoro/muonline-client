# Lahap: jewel combination and dismantling

Lahap (NPC 256) now sends the ordinary talk request and opens this dialog only
after OpenMU acknowledges NPC window 9. This includes the existing Elbeland spawn
without hardcoding the map or coordinates in the client.

The popup follows the supplied original-client screenshots: entry menu, ten jewel
choices in two columns, 10/20/30 combination quantities, confirmation, result and
a selectable bundle list with Disband. Larger lists have page arrows. Cancel
returns to the previous step. Close/Escape closes the NPC session; movement,
damage and leaving the game scene also clean up the dialog and subscriptions.

## Verified Data_Broyal artwork

The original texture data was inspected visually, including alpha and sprite bounds.
No texture files are copied into this repository:

- `Interface/GFx/popupbg01.ozd` through `popupbg09.ozd`: the 16x16 popup border
  corners/rails and translucent center. Frame corners retain their original size.
- `Interface/GFx/Popups_I1.ozd`: normal button at (78,0,74,36), highlighted button
  at (0,0,74,36), pressed button at (156,0,74,36). Button corners are nine-sliced.
- `Interface/GFx/popup_line_m.ozd`: title divider.

The older `newui_msgbox_*` and `newui_btn_empty*` textures were inspected and
rejected for this dialog: their silver ornamented style does not match the reference.
The final popup uses the brown/gold GFx style and a see-through world background.
The static frame is cached in a disposable render target for each current height;
loaded texture objects remain owned by the shared loader.

## Server contract

The client sends the typed C1 BC `LahapJewelMixRequest` with mix IDs 0–9,
quantity enum 0–2 and the absolute source slot for dismantling. It uses the
same single/packed item definitions as OpenMU `CreateJewelMixes`.

Costs follow OpenMU `ItemStackAction`: 500,000 / 1,000,000 / 1,500,000 Zen to
combine 10 / 20 / 30 jewels, and 1,000,000 Zen to dismantle a bundle.
The preflight checks item instances (matching the server), money and occupied
inventory cells, including unlocked extensions. OpenMU requires enough empty
cells *before* removing the bundle. The selected bundle is checked again after
confirmation; a vanished selection is not replaced by another bundle silently.

OpenMU sends inventory changes and a money update on success, rather than a
dedicated Lahap result packet. The dialog waits for both the expected jewel/bundle
count changes and the Zen deduction before showing success. Localized blue server
messages are shown as operation failures while a request is pending. Sending a
request alone never produces the success message. Duplicate submission is blocked;
an unconfirmed request times out to a message requiring the session to be reopened.
The client does not remove items or subtract Zen locally.

The entry menu contains only **Jewel combination** and **Dismantle jewel**,
as requested. Custom server fee or jewel-mix changes require updating this
compatibility configuration.

## Validation

No compilation or live game validation was performed for this change, as requested.
Focused checks are supplied for mapping, fees, inventory footprints and packet bytes:

```sh
dotnet run --project tests/Lahap/Lahap.csproj
```

In-game checks: talk to Elbeland Lahap; combine all supported quantities; cancel
confirmation; select and dismantle bundles in main/extended inventory; exercise
insufficient money, jewels and free cells; verify success follows server updates;
check movement/damage closure, Escape, page arrows, touch input and UI scaling.
