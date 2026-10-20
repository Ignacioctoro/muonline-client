# Party Matching NewUI

Open with **P** or the always-visible **Party** button at the top right (also on Android).
The compact member panel remains below this button. Four tabs provide My Party, Search,
Recruit and Requests, using the existing `Interface/GFx/MatchingParty_I1.ozd` atlas.

Update the matching server branch too. This is an extended protocol (ED, version 1) over the
normal game connection, not the ChatServer. Without its server plugins, the window reports a
timeout; it does not fabricate listings or change party membership locally.

Search accepts a title/leader substring. Recruitment supports base-level range, S6 class families,
optional password and automatic acceptance. Listings advertise the leader's current map, within
the same GameServer. Pending applications can be withdrawn; leaders can accept/reject them.
The Party button shows the pending request count. Refresh occurs every five seconds while needed.

My Party reuses existing authoritative membership data and leave/kick packets. The server controls
capacity, requirements and admission. No cross-subserver search, warp or helper control is added.
Disconnects and cancelled recruitment invalidate their applications. Applications expire after
two minutes. Recruitments and pending requests are not persisted across server restarts.

No compilation or in-game test was performed. Check two-account flows, wrong password/requirements,
full party, disconnects, accepting concurrent applications, touch keyboard, and window occlusion.
