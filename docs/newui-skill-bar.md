# NewUI skill bar and Castle Siege availability

The skill selector now uses the existing `Interface/GFx/main_IE.ozd` HUD socket, rather than `newui_skillbox`/`newui_skillbox2`. `main.ozg` SkillHotKey sprite 154 scales its 20x28 icon placeholder by 1.3 / 1.2857: 26x36 pixels. Its selection subimage 151 occupies (48,174,36,45), offset (-5,-5) from the icon. The normal socket is sampled from the existing HUD panel at (531,32,30,42), fitted to that 36x45 outer size. This selector adaptation preserves the established list arrangement; it is not a full port of the Scaleform skill UI.

The active skill applies `VisualScale` to its children, so its existing 52px HUD placement also scales the frame and icon together. Assigned HUD hotkeys use 26x36 icons. The shared icon lookup now uses Data_Broyal `newui_skill00.OZJ` and `newui_skill00_Non_Icon.OZJ`: 25 columns, 20x28 source cells, direct skill ID including reserved cell zero. S6 Lightning Shock 230 keeps its 216 icon alias. Raven commands and Fenrir retain the separate command sheet; IDs >=300 retain the existing unsupported-icon behavior. No game assets are added.

`CastleSiegeSkillRules` shares availability between the selector, active slot, assigned HUD icons, mobile icons, skill controller and outgoing targeted/area requests. Restricted IDs: class attacks 44,45,46,57,73,74,269 and siege role skills 67..72. They require an in-game character, protocol map 30 (not terrain folder World31), and a confirmed active battle. Selection and assignment remain possible while grey; casting and starting a new walk-to-cast are blocked. A pending cast is discarded if the battle ends or the map changes.

State comes from validated S6 `C1 05 B2 17 00/01` notifications, routed through the registered B2 subcode. Unknown state is inactive. Changing maps or leaving the game clears the confirmation. The grey icon atlases update on availability changes, including invalidating the cached mobile circular icon. This is a client restriction, not server-side security or a complete implementation of guild-rank/bounty permissions, cooldowns or siege effects.

Server integration boundary: receiving B2/17 is supported, but this change does not modify the server to emit it. The OpenMU event implementation reviewed does not currently call the generated BattleStartEnd sender. Until that event-state bridge is added, these skills deliberately remain disabled, including in Loren. A map name, guild membership or join-side buff alone does not enable them. Entering mid-battle also requires fresh server confirmation; there is no inference from the schedule or clock.

Validation performed without compilation: compared all edited originals against branch head, checked changed-file whitespace, decoded the NewUI source shape/placement/subimage tags, inspected both icon atlases and reconstructed the seven attack icons against the new socket. This is asset/static verification, not a game screenshot or a runtime test.

Manual checks that do not require starting Castle Siege:

1. On any normal map, inspect 44/45/46/57/73/74/269 as applicable to the character. They should be grey in the list, active slot and assigned hotkeys. Hover retains the tooltip and adds the active-siege requirement.
2. Select/assign one and attempt a mouse or touch cast. It must not start a new path, animation or skill request. Normal non-siege skills still cast as before.
3. Enter Loren without an active battle: the skills remain grey and blocked. Relogging or changing maps cannot enable them.
4. Check icon alignment, selection border and hotkey banks at supported resolutions. Verify Summoner Lightning Shock, Raven commands and Fenrir icon identity.

Later integration checks: a valid B2/17 start while in map 30 enables the class skills; end disables them and clears pending casts. A start outside map 30 never enables them there. Reject truncated frames and values other than 0/1. Verify event confirmation on map entry/reconnect once server broadcasting is implemented, and separately implement guild-role eligibility before exposing role skills in actual siege combat.
