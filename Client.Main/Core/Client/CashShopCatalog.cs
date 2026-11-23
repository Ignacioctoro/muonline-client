using System;

namespace Client.Main.Core.Client;

public sealed class CashShopProductInfo
{
    private string _name = string.Empty;
    public int Sequence { get; set; }
    public int Option { get; set; }
    public string Name { get => string.IsNullOrWhiteSpace(_name) ? $"Product {Sequence}" : _name; set => _name = value; }
    public int Group { get; set; } = -1;
    public int Number { get; set; } = -1;
    public int Level { get; set; }
    public int Quantity { get; set; }
    public int Price { get; set; }
    public double Duration { get; set; }
    public bool Available { get; set; }
}

public sealed class CashShopPackageInfo
{
    public int Sequence { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Coin { get; set; }
    public int Price { get; set; }
    public bool Bundle { get; set; }
    public CashShopProductInfo[] Products { get; set; } = Array.Empty<CashShopProductInfo>();
}

public sealed class CashShopCurrencyInfo
{
    public int Coin { get; set; }
    public bool Visible { get; set; }
    public bool Enabled { get; set; }
}

public sealed class CashShopCatalogPage
{
    public string CharacterName { get; set; } = string.Empty;
    public int Request { get; set; }
    public int Page { get; set; }
    public int Pages { get; set; }
    public int Filter { get; set; }
    public int Category { get; set; }
    public CashShopCurrencyInfo[] Currencies { get; set; } = Array.Empty<CashShopCurrencyInfo>();
    public int ErrorCode { get; set; }
    public CashShopPackageInfo[] Packages { get; set; } = Array.Empty<CashShopPackageInfo>();
}

public sealed class CashShopProductReply
{
    public string CharacterName { get; set; } = string.Empty;
    public int Request { get; set; }
    public int Sequence { get; set; }
    public int Option { get; set; }
    public CashShopProductInfo Product { get; set; }
}

public sealed class CashShopStorageEntry
{
    public uint Index { get; set; }
    public int Sequence { get; set; }
    public int Option { get; set; }
    public uint ItemSequence { get; set; }
    public string Sender { get; set; } = string.Empty;
}
