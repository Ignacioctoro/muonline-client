# Command Window

The upper-right Comandos touch button follows the existing Mobile Controls Enabled and Opacity settings on all platforms, including desktop testing. It hides while Inventory or the pause menu is open. Pointer barriers and the inventory guard prevent clicks through closing windows.

Choose a command, then tap/click a nearby player. Target selection consumes the pointer through release so it cannot also move/attack. Close/Escape cancels selection. Existing PlayerContextMenu handlers and guild/shop eligibility are reused for Trade, Buy, Party, Whisper, Guild, Alliance, Hostility, Add Friend and Duel. Suspend hostilities uses the existing guild-master request with confirmation; the server remains authoritative.

Follow uses normal client pathfinding and walking packets, at most twice per second, stopping within two tiles. It cancels on manual ground movement/joystick, cancellation keys, target loss/death, local death/teleport/map change and excessive distance. It pauses while inventory/pause is open. No new server packet or teleport is used.

Visuals use CommandWindow_I1.ozd; rectangles were verified against CommandWindow.ozg subimage definitions: body (0,0,327,386), header (652,0,327,73), buttons (652,75), (779,75), (652,112), (779,112), each 125x35. Existing data assets are required; no proprietary files are committed.

Validation: static C# syntax parsing and an atlas/layout preview only, without building as requested. In-game checks pending: every action with eligible/ineligible players, Buy with closed/open stores, Follow through obstacles and cancellation, mobile setting/opacity, inventory-close click-through, and Windows/Android touch target selection.
