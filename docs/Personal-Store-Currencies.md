# Personal Store currencies and account balances

The shared client supports Zen, server-advertised item currencies and WCoin C/P/Goblin Points.
Only currencies advertised by OpenMU appear in the price selector. The deployment preset enables
Bless, Soul, Chaos, Creation, Life and WCoin C, leaving WCoin P and Goblin Points unavailable for store prices.
Zen stays available for compatibility. Each listing has one currency, never a combined price.

The inventory's WC field displays WCoin C only. All three balances are retained separately in session state.
Unknown balances display `--`, not a fabricated zero. Balances refresh when opening the inventory,
every ten seconds while visible, and after purchases; OpenMU also pushes balances after coin payments.
Scene disposal clears the account data and allowed currency list. No account is credited locally.

Windows: right-click an item in your closed store, tap the currency button to cycle the allowed types,
enter the amount and confirm. Android: long-press the item, choose the currency, then tap the amount
to open the native keyboard. Tap Confirm after entering it. A zero amount removes the price.
The buyer's tooltip and confirmation show the exact amount and currency. List refreshes dismiss
the confirmation to avoid confirming a replaced listing. Server acknowledgements remain authoritative.

Protocol: `3F 0A` allowed currencies; encrypted `3F 09` extended set-price; `3F 05/13` listing;
`3F 06` result including currency-insufficient result 10; `D2 01` account-balance request/response.
The Season 6 listing layout is selected by protocol version, not ambiguous entry byte lengths.
Existing Zen-only servers still offer Zen without advertising currencies.

Update both repositories and apply the server configuration update documented in
`docs-website/docs/gameplay/personal-store-currencies.md` in OpenMU. Reconnect after applying it.
No proprietary assets or platform-specific drawing code were added.

Validation: syntax/protocol review only; no compilation or in-game execution was performed.
Test with two different accounts: Zen, each jewel, WCoin C; exact packs vs. insufficient change;
insufficient coins; full seller inventory; disabled WP/GP; cancel; reconnect/change account;
Android keyboard and repeated currency selection. Check the seller's balance after reconnecting too.
