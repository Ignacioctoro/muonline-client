# Friends / Messenger

Open **My Friend** with **F** or the people button beside Quest on the NewUI HUD.
The same button is available on Android; text entry uses the platform text field.

- Friends List: add/accept/delete friends, online server, start a chat, write a letter.
- Letter Box: unread markers, sender/date/subject, read/reply/delete, six-line composer.
- Window List: show/hide/close conversations and hide all windows. Hiding retains the connection;
  closing a chat disconnects it. Up to ten conversations and 200 recent messages per room.
- Invite to Party reuses the existing nearby-player invitation. It does not invite across servers.
- Refuse Chat changes messenger visibility using OpenMU's existing online-state packet.

The server confirms friend/letter mutations. A sent letter draft is cleared only after a successful
acknowledgement for its request, and only if its body has not been edited. Sending letters may cost
Zen according to the server configuration. No prices are guessed by the client.

## OpenMU setup

No server code changes are needed. FriendServer and ChatServer already exist in OpenMU's Startup
host. Chat needs an active ChatServerDefinition and a reachable endpoint. The game server supplies
the chat host, room id and authentication token. The S6 connection-info packet does not include a
port; the client defaults to OpenMU's **45980**. Older OpenMU installations may still use 55980. Override the port in local settings if necessary:

```json
{
  "MuOnlineSettings": {
    "ChatServerPort": 45980
  }
}
```

Game packets use the existing encrypted connection (letters use C4). Conversations use independent
TCP streams with S6 XOR3 token/message payloads wrapped in the client-to-server XOR32 layer
required by OpenMU's ChatServer listener. Server replies have no XOR32 layer. Keep-alives use the
same outgoing transport. All UI state updates are dispatched
onto the game thread. Leaving the game closes room connections. No chat token is logged.

Conversations show participants and support inviting another online friend by name. Invitations
use the existing `CB` request/result with big-endian room/request identifiers. The server confirms
each invitation. The Retry button requests fresh authentication through the game server after a
disconnect; an expired token is never reused. Chat creation and invitation requests time out with
visible feedback, and authentication is limited to ten seconds. Hidden conversations stay connected.
Repeated Chat clicks reopen an existing live conversation instead of creating duplicates.

If chat still cannot connect, check the endpoint in the conversation status and client log. The
advertised ChatServer address must be reachable from the player device and TCP 45980 (or the
configured port) must be available. No server changes or firewall changes are made by this patch.

Letter rendering supports the legacy 1.x client setting and the current 2.04d extended OpenMU
setting. The latter reads OpenLetterExtended's 42-byte appearance field. The letter's appearance
and pose are not previewed in this first UI implementation; the text and mailbox operations are.

## Assets and verification

Uses existing `Interface/GFx/FriendMenu_I1.ozd`, `FriendWindow_I1.ozd`, `btn_l01.ozd` and
`btn_l02.ozd`; no proprietary assets are added to the repository.

Source syntax and protocol offsets reviewed without compiling or running the game, as requested.
In-game checks: friend invitation between two accounts, online/offline state, mail delivery/read/
delete, chat reconnect, hiding/showing windows, Android keyboard and closing the session.
