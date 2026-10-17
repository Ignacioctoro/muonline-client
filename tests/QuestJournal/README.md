# Season 6 quest journal

Run `dotnet run --project tests/QuestJournal/QuestJournal.csproj -p:MonoGameFramework=MonoGame.Framework.DesktopGL`.

The fixtures are written with OpenMU's generated packet classes. They cover NPC
offers and starting steps, confirmed acceptance, requirement counters, reward
values, padding bytes, failed/successful completion, cancellation by group,
reconnection snapshots, late details, malformed counts/lengths, and outgoing
acceptance packets. No server or game assets are required.

## In-game verification

- Press T outside chat: My Quests opens/closes. Escape and X close it and consume input.
- Talk to Elf Soldier and choose Quests; Receive Buff still uses the existing buff flow.
- On an eligible character (e.g. level 1 for Spider Hunt), select an offered quest,
  then Accept Quest. No active state is created until the server confirms it.
- Kill monsters, select the active quest, and verify server counters and green
  completed requirements. The visible selected quest refreshes every 3 seconds.
- Return to Elf Soldier and complete the quest when requirements are met.
- Confirm Give Up Quest, reopen the menu, and reconnect: cancelled/completed
  missions must not return as active. Switching characters clears cached state.
- Check Class Upgrade Quest and the existing Sebina/Marlon/Devin NPC dialogs.
- Hover item requirement/reward text to use the inventory tooltip.
- Check list and requirement/reward scrolling and clicks over the world/HUD.
- Open Chaos Goblin: Regular Combination and Item 380 Option must be vertical.

## Compatibility

Uses S6 F6/0A,0B,0C,0D,0F,1A,1B responses and F6/30 for NPC offers.
Legacy class quests keep their A1/A2/A3/A4 interaction path. NewUI skins come
from the installed GFx MyQuestInfo_I1 and NpcJobChangeQuest_I3 atlases.
No proprietary textures are included in the repository.

QuestCatalog contains presentation-only names/level hints derived from OpenMU's
MIT-licensed SeasonSix/Quests.cs. Unknown custom IDs have a fallback name.
It does not grant rewards, invent progress, or populate offers independently of
the server. Categories cover general/class/repeatable S6 quests; modern guide
quests and automatic teleportation are not added. NPC acceptance/completion
requires the current NPC interaction. Class quests are accepted at their NPC.

Automated checks do not replace a live client/server smoke test.
