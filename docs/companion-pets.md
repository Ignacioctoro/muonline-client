# Season 6 companion pets

Shared client visuals for Guardian Angel (13:0), Imp (13:1), Demon (13:64)
and Panda (13:80). OpenMU continues to own bonuses, equipment and durability.
This change adds their animated followers; it does not send combat or automatic
loot requests. Panda uses its pet model, not the transformation-ring model.

## Assets

Use the existing Data_Broyal files; no proprietary assets are included:

| Pet | Model | Motion |
| --- | --- | --- |
| Angel | Item/helper01.bmd | Flying companion |
| Imp | Item/helper02.bmd | Flying companion |
| Demon | Item/partCharge4/demon.bmd | Elevated follower |
| Panda | Item/PandaPet.bmd | Ground follower |

The Item copies of helper01/02 are identical to the Player copies in the
inspected data, and resolve fairy/satan textures in their correct folder.
The existing BMD reader supports the encrypted Demon/Panda models.

Local state reads equipment slot 8, including durability. Remote initial
appearance supports standard 18-byte and extended 25/27-byte packets. Runtime
equipment changes override the initial appearance, including explicit unequip.
The standard decoder keeps empty helper (3) distinct from Angel (0), and uses
byte 16's high three bits for Demon/Panda rather than the obsolete pet index.

Each pet has an independent lazy model to avoid cross-pet asynchronous reload
races. Models attach to the player's child lifecycle, use world-space movement
with parent compensation, and dispose with the player. Movement is time-based;
model completion is applied on the main thread and ignored after disposal.

## Manual checks (Windows and Android)

1. Equip each pet in slot 8; confirm the right model, animation and HUD name.
2. Observe from another client, both before entering view and after changing
   equipment. Unequip, swap directly between pets, and swap to a mount.
3. Test rapid equip/unequip while assets initially load: no previous pet should
   reappear when a delayed load finishes.
4. Walk, turn, teleport and change maps. Panda follows terrain; flying pets
   retain their height and do not inherit the player's combat animation.
5. Exhaust local pet durability: the visual disappears. Reconnect and check
   equipment again. Verify existing mounts and Dark Raven still work.
6. Check character selection and empty helper slots: no phantom Angel.

Validation performed without building: C# syntax parsing, inspection of OpenMU
appearance serialization, and helper-model/texture checks. Runtime rendering
and platform builds remain to be verified in the game.
