using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;

namespace NekoDex.Data;

public sealed record StainInfo(uint Id, string Name, Vector4 Color, byte Shade);

/// <summary>Dyes from the Stain sheet with their swatch colours, loaded once.</summary>
public sealed class StainCatalog
{
    private readonly IDataManager dataManager;
    private IReadOnlyList<StainInfo>? all;
    private Dictionary<uint, StainInfo>? byId;

    public StainCatalog(IDataManager dataManager)
    {
        this.dataManager = dataManager;
    }

    public IReadOnlyList<StainInfo> All
    {
        get
        {
            EnsureLoaded();
            return all!;
        }
    }

    public StainInfo? Find(uint id)
    {
        EnsureLoaded();
        return byId!.TryGetValue(id, out var stain) ? stain : null;
    }

    public IEnumerable<StainInfo> Search(string? search)
    {
        var needle = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        return needle is null ? All : All.Where(s => s.Name.Contains(needle, StringComparison.OrdinalIgnoreCase));
    }

    private void EnsureLoaded()
    {
        if (all is not null)
        {
            return;
        }

        var list = dataManager.GetExcelSheet<Stain>()
            .Where(s => s.RowId > 0 && !s.Name.IsEmpty)
            .OrderBy(s => s.Shade).ThenBy(s => s.SubOrder).ThenBy(s => s.RowId)
            .Select(s => new StainInfo(s.RowId, s.Name.ExtractText(), ToColor(s.Color), s.Shade))
            .ToArray();

        all = list;
        byId = list.ToDictionary(s => s.Id);
    }

    /// <summary>The sheet stores 0xRRGGBB.</summary>
    private static Vector4 ToColor(uint rgb)
        => new(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);
}
