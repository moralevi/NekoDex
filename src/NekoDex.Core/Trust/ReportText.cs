using System.Text;
using NekoDex.Core.Collections;
using NekoDex.Core.Unlock;

namespace NekoDex.Core.Trust;

/// <summary>Builds the text a user pastes to the data maintainers when an unlock source is wrong.</summary>
public static class ReportText
{
    public static string Build(string moduleId, CollectionEntry entry, UnlockSource source, string? dataVersion, string pluginVersion)
    {
        var sb = new StringBuilder();
        sb.Append("NekoDex source report: ").Append(entry.Name).Append(" (").Append(moduleId).Append(" id ").Append(entry.Id).AppendLine(")");
        if (source.IsUnknown)
        {
            sb.AppendLine("Current data: unknown / no source");
        }
        else
        {
            sb.Append("Current data (").Append(source.Note ?? "no attribution").AppendLine("):");
            foreach (var line in source.DetailLines)
            {
                sb.Append("  - ").AppendLine(line);
            }
        }

        if (source.Patch is not null)
        {
            sb.Append("Patch: ").AppendLine(source.Patch);
        }

        sb.AppendLine("Correction: ");
        sb.Append("Dataset ").Append(dataVersion ?? "none").Append(", plugin ").Append(pluginVersion);
        return sb.ToString();
    }
}
