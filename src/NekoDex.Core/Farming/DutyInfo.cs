namespace NekoDex.Core.Farming;

/// <summary>Difficulty band of a duty, as players talk about it. Order is roughly easiest first.</summary>
public enum DutyKind
{
    Other = 0,
    Dungeon = 1,
    Trial = 2,
    Raid = 3,
    Extreme = 4,
    Unreal = 5,
    Chaotic = 6,
    Savage = 7,
    Ultimate = 8,
}

/// <summary>One ContentFinderCondition row as the farm view needs it.</summary>
public sealed record DutyInfo(uint Id, string Name, DutyKind Kind, int Level, int ItemLevel);

/// <summary>
/// Classifies a duty from its name and content type. The sheet's HighEndDuty flag is not reliable (some Extremes are false),
/// so the name suffix decides. Content type ids: 2 dungeons, 4 trials, 5 raids, 28 ultimate, 30 variant/criterion, 37 chaotic.
/// </summary>
public static class DutyKindClassifier
{
    public static DutyKind Classify(string name, uint contentTypeId)
    {
        if (contentTypeId == 28)
        {
            return DutyKind.Ultimate;
        }

        if (contentTypeId == 37)
        {
            return DutyKind.Chaotic;
        }

        if (name.Contains("(Savage)", StringComparison.OrdinalIgnoreCase))
        {
            return DutyKind.Savage;
        }

        if (name.Contains("(Extreme)", StringComparison.OrdinalIgnoreCase) || name.StartsWith("The Minstrel's Ballad", StringComparison.OrdinalIgnoreCase))
        {
            return DutyKind.Extreme;
        }

        if (name.Contains("(Unreal)", StringComparison.OrdinalIgnoreCase))
        {
            return DutyKind.Unreal;
        }

        return contentTypeId switch
        {
            2 or 30 => DutyKind.Dungeon,
            4 => DutyKind.Trial,
            5 => DutyKind.Raid,
            _ => DutyKind.Other,
        };
    }

    public static bool IsHighEnd(DutyKind kind) => kind is DutyKind.Savage or DutyKind.Ultimate;
}
