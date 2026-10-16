using System.Collections.Generic;
using System.Linq;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;
using NekoDex.Core.Advisor;

namespace NekoDex.Data;

/// <summary>
/// Gear sold for Allagan tomestones, from the SpecialShop sheet. Tomestone shops use currency type 4 and a small cost id
/// that equals the TomestonesItem sheet's tomestone number (verified 2026-10-16: 1 Poetics, 2 Mathematics, 3 Mnemonics, 4 Heliometry).
/// The cheapest listing wins when an item is sold in several shops.
/// </summary>
public sealed class TomestoneShopIndex
{
    private const byte TomestoneCurrencyType = 4;

    private readonly IDataManager dataManager;
    private readonly IPluginLog log;
    private Dictionary<uint, GearPrice>? prices;
    private IReadOnlyList<(uint ItemId, string Name)>? currencies;

    public TomestoneShopIndex(IDataManager dataManager, IPluginLog log)
    {
        this.dataManager = dataManager;
        this.log = log;
    }

    /// <summary>Tomestone currencies that exist in this game version, as item id and name.</summary>
    public IReadOnlyList<(uint ItemId, string Name)> Currencies
    {
        get
        {
            EnsureLoaded();
            return currencies!;
        }
    }

    public GearPrice? Find(uint itemId)
    {
        EnsureLoaded();
        return prices!.TryGetValue(itemId, out var price) ? price : null;
    }

    private void EnsureLoaded()
    {
        if (prices is not null)
        {
            return;
        }

        // Tomestone number -> currency item.
        var byNumber = new Dictionary<uint, (uint ItemId, string Name)>();
        foreach (var row in dataManager.GetExcelSheet<TomestonesItem>())
        {
            if (row.Tomestones.RowId == 0 || !row.Item.IsValid || row.Item.RowId == 0)
            {
                continue;
            }

            byNumber[row.Tomestones.RowId] = (row.Item.RowId, row.Item.Value.Name.ExtractText());
        }

        var result = new Dictionary<uint, GearPrice>();
        foreach (var shop in dataManager.GetExcelSheet<SpecialShop>())
        {
            if (shop.UseCurrencyType != TomestoneCurrencyType)
            {
                continue;
            }

            foreach (var listing in shop.Item)
            {
                var receive = listing.ReceiveItems[0];
                if (receive.Item.RowId == 0 || !receive.Item.IsValid || receive.Item.Value.EquipSlotCategory.RowId == 0)
                {
                    continue;
                }

                var cost = listing.ItemCosts[0];
                if (cost.CurrencyCost == 0 || !byNumber.TryGetValue(cost.ItemCost.RowId, out var currency))
                {
                    continue;
                }

                var price = new GearPrice(currency.ItemId, currency.Name, (int)cost.CurrencyCost);
                if (!result.TryGetValue(receive.Item.RowId, out var existing) || existing.Amount > price.Amount)
                {
                    result[receive.Item.RowId] = price;
                }
            }
        }

        prices = result;
        currencies = byNumber.OrderBy(kv => kv.Key).Select(kv => kv.Value).ToArray();
        log.Debug("Tomestone shop index: {Items} priced items across {Currencies} currencies", result.Count, currencies.Count);
    }
}
