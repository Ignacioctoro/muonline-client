# Character switching and live combat diagnosis

The client clears learned skills and remembered area casts at logout and before
sending a character-selection request. Skill session revisions discard already
queued F3:11 updates belonging to the previous character. The active skill,
PC/mobile shortcuts and open palettes reset when that session changes. Normal
skill-list updates within the same character keep valid shortcuts.

Local acceptance: log in as Elf, assign skills in both hotkey banks and mobile
slots, return to selection and choose DL. Open both skill palettes: only the DL
skills should appear. Repeat DL -> Elf and reselect the same character. Repeat
while a full F3:11 update is pending. Replacing a Summoner book within one session
must continue to clear only removed skills.

## Open issues: Force damage and arrow consumption

Fire Slash and Summoner book removal were confirmed fixed by the user. Force
animation/effect is visible, but damage is still unconfirmed. Arrow count is
visible but does not decrease without Infinity Arrow. The user's weapon is an
excellent Dark Stinger with all options. The S6 Stinger Bow initializer (4:23)
adds AmmunitionConsumptionRate=1; excellent options do not exempt that bow.
This does not prove the running database has the same power-ups.

The server diagnostic is prepared on broyal-server. After updating/restarting
that server, use /combatinfo as a GM on the affected character, or use
/combatinfo char=CharacterName from another online GM. Output is also written
to server logs under COMBATINFO. It only reads values.

Compare /combatinfo before and after attacking with the Elf:

- Rate 1, amount and inventory decrease: consumption works server-side; inspect
  delivery/handling of 0x2A on the client.
- Rate 0 with InfinityArrow=False: inspect the weapon base power-ups and other
  active modifiers in the database. Do not add local client decrements.
- Attribute amount decreases but inventory stays fixed/missing: inspect ammo
  recognition (IsAmmunition) and the durability-change notification.

For DL, /combatinfo shows the configured/available Force 60, item Force Wave 66
and Strengthener 509, including type, damage type, target restrictions and hits.
The S6 server intentionally hides 66 in the palette and may resolve a request
for 60 through 66. The diagnostic build additionally logs FORCE warnings for
ammunition-blocked attacks and rejected target hits. Successful hit damage is
logged at Debug level. Neither damage formulas nor consumption rules were changed.

No build or in-game execution was performed here. Added regression coverage for
resetting the learned palette and invalidating its session; tests were not run.
