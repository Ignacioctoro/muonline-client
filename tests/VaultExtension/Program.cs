using Client.Main;
using Client.Main.Configuration;
using Client.Main.Controls.UI.Game;
using Client.Main.Core.Client;
using Client.Main.Networking;
using Client.Main.Networking.PacketHandling.Handlers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Xna.Framework;
using MUnique.OpenMU.Network.Packets;

if (args.Length > 0) Constants.DataPath = args[0];
void Check(bool value, string message) { if (!value) throw new Exception(message); }
for (int page = 0; page < 2; page++)
for (int y = 0; y < 15; y++)
for (int x = 0; x < 8; x++)
{
    var position = new Point(x, y);
    int slot = VaultPageLayout.Slot(page, position);
    Check(slot == page * 120 + y * 8 + x, "Absolute vault address");
    Check(VaultPageLayout.Contains(page, slot) && !VaultPageLayout.Contains(1-page, slot), "Pages cannot alias");
    Check(VaultPageLayout.Fits(page, position, 1, 1, true), "Unlocked slot");
    Check(VaultPageLayout.Fits(page, position, 1, 1, false) == (page == 0), "Account lock");
}
Check(!VaultPageLayout.Fits(1, new Point(7, 0), 2, 1, true), "Right edge");
Check(!VaultPageLayout.Fits(0, new Point(0, 14), 1, 2, true), "Page boundary");
Check(!VaultPageLayout.Fits(2, Point.Zero, 1, 1, true), "Only one extension");
var state = new CharacterState(NullLoggerFactory.Instance);
var scope = new ScopeManager(NullLoggerFactory.Instance, state);
var network = new NetworkManager(NullLoggerFactory.Instance, new MuOnlineSettings(), state, scope);
var misc = new MiscGamePacketHandler(NullLoggerFactory.Instance, network, network.GetCharacterService(), state, scope, TargetProtocolVersion.Season6);
await misc.HandleCharacterListAsync(new byte[] {0xC1,8,0xF3,0,0,0,0,1});
Check(state.IsVaultExtended, "Read account extension from Season 6 character list");
await misc.HandleCharacterListAsync(new byte[] {0xC1,8,0xF3,0,0,0,0,0});
Check(!state.IsVaultExtended, "Next account must not inherit extension");
var handler = new InventoryHandler(NullLoggerFactory.Instance, state, null!, default);
byte[] item = new byte[12]; item[0]=13; item[9]=0xE0;
byte[] Reply(ItemStorageKind storage, byte slot) { var p=new byte[17]; p[0]=0xC3;p[1]=17;p[2]=0x24;p[3]=(byte)storage;p[4]=slot;item.CopyTo(p,5);return p; }
state.AddOrUpdateInventoryItem(12,item);
state.StashPendingInventoryMove(12,12);
await handler.HandleItemMovedOrFailedAsync(Reply(ItemStorageKind.Vault,120));
Check(!state.TryGetInventoryItem(12,out _) && state.GetVaultItems().ContainsKey(120), "Deposit in extended page");
foreach(var (from,to) in new (byte,byte)[]{(120,239),(239,0),(0,119),(119,128)})
{
    state.StashPendingVaultMove(from,to);
    Check(state.HasPendingVaultMove && state.GetVaultItems().ContainsKey(from), "Retain source until server reply");
    await handler.HandleItemMovedOrFailedAsync(Reply(ItemStorageKind.Vault,to));
    Check(!state.GetVaultItems().ContainsKey(from) && state.GetVaultItems()[to].SequenceEqual(item), "Move across page boundaries without duplicate");
    Check(!state.HasPendingVaultMove, "Unlock after reply");
}
state.StashPendingVaultMove(128,130);
await handler.HandleItemMovedOrFailedAsync(new byte[]{0xC3,5,0x24,0xFF,0});
Check(state.GetVaultItems().ContainsKey(128) && !state.GetVaultItems().ContainsKey(130) && !state.HasPendingVaultMove, "Rejected move retains source");
state.StashPendingVaultMove(128,0xFF);
await handler.HandleItemMovedOrFailedAsync(Reply(ItemStorageKind.Inventory,20));
Check(state.GetVaultItems().Count==0 && state.TryGetInventoryItem(20,out var result) && result.SequenceEqual(item), "Withdraw extended item into backpack");
Check(!state.TryGetInventoryItem(7,out _), "Equipment is untouched");
Console.WriteLine("PASS: all 240 slots, page locks/bounds, account packet flag, deposits, cross-page moves, rejection and withdrawal.");

// Page refresh must preserve the picked object's identity and absolute source.
var window = VaultControl.Instance;
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
void Field(string name, object value) => window.GetType().GetField(name, flags)!.SetValue(window,value);
void Refresh() => window.GetType().GetMethod("RefreshVaultContent",flags)!.Invoke(window,null);
List<InventoryItem> Items() => (List<InventoryItem>)window.GetType().GetField("_items",flags)!.GetValue(window)!;
state.IsVaultExtended = true;
state.AddOrUpdateVaultItem(0,item);
state.AddOrUpdateVaultItem(120,item);
Field("_characterState",state);
Field("_vaultPage",0);
Refresh();
Check(Items().Count==1 && Items()[0].GridPosition==Point.Zero,"Main page excludes extended items");
var picked=Items()[0];
Field("_draggedItem",picked);
Field("_draggedSourceSlot",(byte)0);
Field("_vaultPage",1);
Refresh();
Check(Items().Count==1 && !ReferenceEquals(Items()[0],picked) && Items()[0].GridPosition==Point.Zero,"Extended slot 120 maps to local origin without source alias");
Check(!window.CanPlaceAt(Point.Zero,picked),"Other page's occupied slot blocks drop");
Field("_vaultPage",0);
Refresh();
Check(Items().Count==1 && ReferenceEquals(Items()[0],picked),"Returning to source page preserves picked identity");
Check(window.CanPlaceAt(Point.Zero,picked),"Picked source does not collide with itself");
Console.WriteLine("PASS: page refresh, occupancy, and picked-object identity across page switches.");

