# Pets: Season 6 information with the modern GFx skin

Open **Pets** with **Y**, from the Community menu above the bottom HUD, or by
clicking/tapping a pet's life indicator. The window lists the equipped companion
(S6 slot 8) and Dark Raven (slot 1). All equipped helpers/mounts are supported,
including Angel, Imp, Uniria, Dinorant, Dark Horse, Fenrir, Demon, Spirit of
Guardian, Rudolf, Panda, Unicorn, Skeleton and compatible custom helper items.
An empty panel explains that a pet must be equipped through Inventory.

This is an information window over existing Season 6 equipment. It does not
create the pet inventory, additional slots, summoning systems or pet item-move
protocols introduced by later seasons.

## Presentation

- The complete right-hand frame from `CharacterInfoFrame_I1.ozd` uses the same
  verified source (656,0,354,638) and preserved 96px header/39px footer as Pet Trainer.
- `pet_bg.ozd` supplies the pet selector cards and detail inset.
- `pethp_guege.ozd` supplies the modern red life strip.
- `btn_m01/02/03.ozd` and `btn_close01/02/03.ozd` supply the existing GFx controls.
- Pet item previews reuse `BmdPreviewRenderer`. Hovering the preview uses the
  existing item tooltip builder/renderer, including variant-specific Fenrir options.
- The static panel is cached in an owned render target. Shared textures/previews
  remain owned by their loaders. Graphics resources can be recreated after scene
  disposal; no new proprietary texture files are committed.

The modern `PetInventoryFrame` artwork was inspected, but its newer pet inventory
grid is not imported into this Season 6 interface. Non-trainable pets show their
life/durability and existing item options, without invented level/experience fields.
Long option lists support mouse wheel and touch arrows.

## Server data and controls

Dark Horse and Dark Raven display level, experience and life from the existing
A9 response cache. The window requests updates on opening/equipment changes and
at most once every five seconds per equipped trainable pet while visible. The
experience bar uses OpenMU's S6 formula `level^3 * 100 * (level + 10)` and max level 50.
The equipped-item life field is used until the server's A9 data arrives.

Horse defense/absorption and Raven **base** damage/speed/attack-rate displays mirror
`OpenMU-server`'s `Pets.cs` and `CharacterClasses/ClassDarkLord.cs`. Total Agility
and Leadership come from the existing character state. Base Raven values exclude
master skill/combat multipliers. Custom server formulas require synchronizing
these display rules; the client does not apply any additional combat bonuses.

Raven buttons offer Standby, Automatic attack, With owner and Attack target.
The first three send the existing C1 A7 command and wait for a new server PetMode
acknowledgement before treating it as confirmed. The fourth selects the original
Raven target command (123) in the skill HUD and closes the panel so a target can
be chosen normally. Desktop right-click remains unchanged; touch activation
against monsters sends the same command instead of an ordinary skill packet.
Commands are disabled for absent/dead ravens. Closing Pets never closes an NPC
conversation or changes the raven's current behaviour.

## Validation

The final implementation was not compiled or run in game, per the user's standing
request. Focused pure-rule checks are supplied:

```sh
dotnet run --project tests/PetInfo/PetInfo.csproj
```

Check empty equipment, a companion alone, Raven alone, both slots, all Fenrir
variants and non-trainable helpers. Check equip/unequip, durability/experience
updates, max level, command acknowledgement/timeouts and target selection.
Also check Y/chat input, HUD/menu touch access, Escape, long options, overlapping
windows, scene changes and both graphics backends before considering visual
and runtime parity validated.
