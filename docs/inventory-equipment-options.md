# Inventory jewel packs and equipment options

Packed jewels display the single-jewel model with a quantity overlay. S6 Lahap packs
(group 12, IDs 30/31/136–143) encode 10/20/30 pieces as levels 0/1/2. Those levels
are not enhancement badges. Ordinary durability-based stack quantities are preserved.
The shared rule also applies to vault, trade, personal store, shop and dragged items.

The inventory's **Set items** and **Socket items** labels show equipment summaries
on hover. Clicking/tapping pins a summary; tapping elsewhere dismisses it. Both use
the existing `ClassicItemTooltipRenderer`, with a translucent black summary style,
magenta heading and blue active options. Summaries are cached until equipment,
durability or socket-data availability changes. Only equipment slots 0–11 count.

## Ancient configuration

`Client.Main/Data/openmu-season6-ancient-sets.json` mirrors the 36 sets in
`Ignacioctoro/OpenMU-server`, commit `ba45b19245acf5c9e02d17700f0efeef1d37db06`,
`src/Persistence/Initialization/VersionSeasonSix/Items/AncientSets.cs` (MIT).
It is embedded configuration, not proprietary game assets. Entries identify pieces
by group, number and Ancient discriminator and list options in server order.
Percentage text converts the server's fractional chances to percentages.

This compatibility catalog is necessary because modern Data_Broyal Ancient IDs,
memberships and values differ from OpenMU S6. It supplies both the aggregate summary
and the individual Ancient item's name/options for these known sets. Unrecognized
sets retain the existing individual BMD tooltip fallback and are not advertised as
active S6 bonuses in the summary.

As in `ItemPowerUpFactory.GetSetPowerUps`, broken pieces do not count, identical
pieces count once, fewer than two pieces grant no bonus, partial sets grant the
first `n-1` options and complete sets grant all options. If the running server has
custom Ancient configuration, synchronize this catalog with that configuration
before relying on the summary; it is not downloaded from the live server.

## Socket configuration

The equipment summary reads table **2** of `socketitem.bmd`; table 0 describes
seed elements and table 1 describes an individual item's bonus. Empty sockets,
absent sockets and broken items do not count. Requirements are minimum seed counts,
so both S6 package bonuses can apply simultaneously. Names use the existing English
table and values/requirements use the modern table. Data_Broyal matches the S6
server initializer: double damage 3% and ignore defense 1%.

## Validation

The final change was not compiled or run, at the user's request. No visual parity
or DX/GL runtime validation is claimed.

Focused checks, requiring no running server or graphics device:

```sh
dotnet run --project tests/InventoryBonuses/InventoryBonuses.csproj
```

Manually check pack quantities, regular stacks, partial/complete/mixed Ancient sets,
duplicate rings, broken equipment, missing socket elements, both socket packages,
label hover/tap behavior, UI scaling and tooltip placement near screen edges.
