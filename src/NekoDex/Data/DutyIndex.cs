using System.Collections.Generic;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;
using NekoDex.Core.Farming;

namespace NekoDex.Data;

/// <summary>ContentFinderCondition rows as <see cref="DutyInfo"/>, loaded once.</summary>
public sealed class DutyIndex
{
    private readonly IDataManager dataManager;
    private Dictionary<uint, DutyInfo>? duties;

    public DutyIndex(IDataManager dataManager)
    {
        this.dataManager = dataManager;
    }

    public DutyInfo? Find(uint contentFinderConditionId)
    {
        if (duties is null)
        {
            var map = new Dictionary<uint, DutyInfo>();
            foreach (var row in dataManager.GetExcelSheet<ContentFinderCondition>())
            {
                if (row.Name.IsEmpty)
                {
                    continue;
                }

                var name = Core.Text.DisplayName.TitleCase(row.Name.ExtractText());
                map[row.RowId] = new DutyInfo(row.RowId, name, DutyKindClassifier.Classify(name, row.ContentType.RowId), row.ClassJobLevelRequired, row.ItemLevelRequired);
            }

            duties = map;
        }

        return duties.TryGetValue(contentFinderConditionId, out var duty) ? duty : null;
    }
}
