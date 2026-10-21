using System;
using System.Collections.Generic;
using System.Globalization;
using Client.Main.Core.Utilities;

namespace Client.Main.Core.Client;

/// <summary>Account balances and server-advertised shop currencies. Owned by the network session.</summary>
public sealed class StoreCurrencyState
{
    public const ushort WCoinCType = 0xFFF0, WCoinPType = 0xFFF1, GoblinPointsType = 0xFFF2;
    private readonly List<ushort> _allowed = new() { 0 };
    private readonly Dictionary<ushort, string> _names = new();
    public IReadOnlyList<ushort> Allowed => _allowed;
    public long WCoinC { get; private set; }
    public long WCoinP { get; private set; }
    public long GoblinPoints { get; private set; }
    public bool HasBalance { get; private set; }
    public bool IsAllowed(ushort type) => _allowed.Contains(type);
    public static bool IsAccountCoin(ushort type) => type is >= WCoinCType and <= GoblinPointsType;
    public static bool IsItem(ushort type) => type != 0 && !IsAccountCoin(type);
    public void SetAllowed(ushort[] types)
    {
        _allowed.Clear(); _allowed.Add(0);
        foreach (ushort type in types)
            if (type != 0 && (type < WCoinCType || IsAccountCoin(type)) && !_allowed.Contains(type))
            { _allowed.Add(type); Name(type); }
    }
    public string Name(ushort type)
    {
        if (_names.TryGetValue(type, out var name)) return name;
        name = type switch
        {
            0 => "Zen", WCoinCType => "WCoin C", WCoinPType => "WCoin P", GoblinPointsType => "Goblin Points",
            _ => ItemDatabase.GetItemDefinition((byte)(type >> 12), (short)(type & 0xFFF))?.Name ?? $"Item {type >> 12}/{type & 0xFFF}"
        };
        _names[type] = name; return name;
    }
    public string Format(uint amount, ushort type) => amount.ToString("N0", CultureInfo.InvariantCulture) + " " + Name(type);
    public void SetBalances(long wc, long wp, long gp)
    { WCoinC = wc; WCoinP = wp; GoblinPoints = gp; HasBalance = true; }
    public void Reset()
    { WCoinC = WCoinP = GoblinPoints = 0; HasBalance = false; _allowed.Clear(); _allowed.Add(0); }
}
