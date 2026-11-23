using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Client.Main.Core.Utilities;

namespace Client.Main.Core.Client;

public partial class CharacterState
{
    // byte[] keys use reference equality. Identical items in different slots can have different dates.
    // Replaced/removed records and complete inventory snapshots explicitly discard their old entries.
    private readonly ConcurrentDictionary<byte[], DateTimeOffset> _itemExpirations = new();

    public DateTimeOffset? GetItemExpiration(byte[] rawItem) =>
        rawItem != null && _itemExpirations.TryGetValue(rawItem, out var expiresAt) ? expiresAt : null;

    public void ClearItemExpirationDates()
    {
        _itemExpirations.Clear();
        RaiseInventoryChanged();
    }

    public void SetItemExpiration(ushort slot, uint unixSeconds)
    {
        // D2 12 follows the item record, including after a move or an expiration resend.
        if (slot > byte.MaxValue || !_inventoryItems.TryGetValue((byte)slot, out var raw)) return;
        var details = ItemDatabase.ParseItemDetails(raw);
        if (!details.IsTimeLimited && !details.IsExpired) return;
        _itemExpirations[raw] = DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
        RaiseInventoryChanged();
    }

    /// <summary>Equipment which the server still presents as active, for the local character's appearance.</summary>
    public IReadOnlyDictionary<byte, byte[]> GetEffectiveEquipmentItems()
    {
        var equipment = new Dictionary<byte, byte[]>();
        foreach (var pair in _inventoryItems)
            if (pair.Key < 12 && !ItemDatabase.ParseItemDetails(pair.Value).IsExpired)
                equipment[pair.Key] = pair.Value;
        return equipment;
    }
}
