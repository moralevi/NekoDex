namespace NekoDex.UI;

/// <summary>One-shot request to bring a module tab to the front, set by the Dashboard and consumed by the main window on its next draw.</summary>
public sealed class TabNavigation
{
    public string? RequestedModuleId { get; set; }

    public bool Consume(string moduleId)
    {
        if (RequestedModuleId != moduleId)
        {
            return false;
        }

        RequestedModuleId = null;
        return true;
    }
}
