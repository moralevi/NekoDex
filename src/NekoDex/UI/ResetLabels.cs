using NekoDex.Core.Resets;

namespace NekoDex.UI;

internal static class ResetLabels
{
    public static string For(ResetDefinition reset) => reset.Id switch
    {
        "daily" => Strings.Get("Reset_Daily", "Daily reset"),
        "weekly" => Strings.Get("Reset_Weekly", "Weekly reset"),
        "gc" => Strings.Get("Reset_GrandCompany", "Grand Company missions"),
        "cactpot" => Strings.Get("Reset_JumboCactpot", "Jumbo Cactpot drawing"),
        "fashion" => Strings.Get("Reset_FashionReport", "Fashion Report judging opens"),
        _ => reset.Name,
    };

    public static string For(ResetCadence cadence) => cadence switch
    {
        ResetCadence.Daily => Strings.Get("Cadence_Daily", "Daily"),
        ResetCadence.Weekly => Strings.Get("Cadence_Weekly", "Weekly"),
        _ => Strings.Get("Cadence_Never", "Never"),
    };
}
