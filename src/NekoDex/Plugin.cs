using System;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using NekoDex.Core.Unlock;
using NekoDex.Data;
using NekoDex.UI;

namespace NekoDex;

public sealed class Plugin : IDalamudPlugin
{
    private const string CommandName = "/nekodex";

    private readonly IDalamudPluginInterface pluginInterface;
    private readonly ICommandManager commandManager;
    private readonly IClientState clientState;
    private readonly IPluginLog log;

    private readonly ConfigurationManager config;
    private readonly WindowSystem windowSystem = new("NekoDex");
    private readonly MainWindow mainWindow;
    private readonly ConfigWindow configWindow;
    private readonly MiniWindow miniWindow;
    private readonly AutoChecklistController autoItems;
    private readonly ResetNotifier resetNotifier;
    private readonly DutyClearTracker dutyClears;

    // Services arrive through constructor injection; nothing in the plugin reaches for them statically.
    public Plugin(
        IDalamudPluginInterface pluginInterface,
        ICommandManager commandManager,
        IClientState clientState,
        IPlayerState playerState,
        IDataManager dataManager,
        ITextureProvider textureProvider,
        IFramework framework,
        IToastGui toastGui,
        IGameGui gameGui,
        IDutyState dutyState,
        IPluginLog log)
    {
        this.pluginInterface = pluginInterface;
        this.commandManager = commandManager;
        this.clientState = clientState;
        this.log = log;

        config = new ConfigurationManager(pluginInterface, log);
        Strings.Apply(pluginInterface, config.Current, log);

        // Unlock info comes from the bundled dataset; anything it cannot answer becomes an explicit
        // Unknown with a web link for the configured site. Entries newer than the data also fall back to the link.
        var dataset = BundledUnlockDataset.Load(log);
        IUnlockSourceProvider[] providers = dataset is null ? [] : [dataset];
        var unlockResolver = new UnlockResolver(providers, () => config.Current.LinkSite);

        // One tab per collection module. Adding a module means one line here plus its class in Data/.
        var pins = new PinController(config);
        var triadNpcs = new TriadNpcIndex(dataManager, log);
        var mountsTab = new CollectionTab(new MountModule(dataManager, clientState, log), textureProvider, unlockResolver, dataset, pins);
        CollectionTab[] tabs =
        [
            mountsTab,
            new(new MinionModule(dataManager, clientState, log), textureProvider, unlockResolver, dataset, pins),
            new(new OrchestrionModule(dataManager, log), textureProvider, unlockResolver, dataset, pins),
            new(new EmoteModule(dataManager, log), textureProvider, unlockResolver, dataset, pins),
            new(new BardingModule(dataManager, log), textureProvider, unlockResolver, dataset, pins),
            new(new FashionAccessoryModule(dataManager, clientState, log), textureProvider, unlockResolver, dataset, pins),
            new(new FacewearModule(dataManager, log), textureProvider, unlockResolver, dataset, pins),
            new(new TriadCardModule(dataManager, triadNpcs, log), textureProvider, unlockResolver, dataset, pins, new TriadDetailExtension(triadNpcs, gameGui)),
        ];
        bool CharacterReady() => clientState.IsLoggedIn && playerState.IsLoaded;

        // Resets are fixed in UTC, so the local clock is enough; the checklist controller stamps and clears with it.
        var checklist = new ChecklistController(config, () => DateTimeOffset.UtcNow);
        autoItems = new AutoChecklistController(config, new AutoChecklistSources(dataManager, log), CharacterReady, () => DateTimeOffset.UtcNow);
        var navigation = new TabNavigation();
        var dashboard = new DashboardTab(checklist, autoItems, tabs, CharacterReady, navigation);

        // Mount farming: duties from the unlock data, clear counts from duty completion events.
        var duties = new DutyIndex(dataManager);
        dutyClears = new DutyClearTracker(dutyState, playerState, config, log);
        var farm = new FarmTab(mountsTab, duties, dutyClears, pins, textureProvider);

        miniWindow = new MiniWindow(config, checklist, autoItems, tabs, pins, CharacterReady, duties, dutyClears);
        resetNotifier = new ResetNotifier(framework, toastGui, config, CharacterReady);
        var version = pluginInterface.Manifest.AssemblyVersion.ToString();
        configWindow = new ConfigWindow(config, miniWindow.SetShown, dataset, version, autoItems, new UserDataService(config, log, version), pluginInterface, SetLanguage);
        mainWindow = new MainWindow(config, clientState, playerState, dashboard, tabs, navigation, farm, configWindow.Toggle);
        windowSystem.AddWindow(mainWindow);
        windowSystem.AddWindow(configWindow);
        windowSystem.AddWindow(miniWindow);

        commandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Toggle the NekoDex window. \"/nekodex config\" opens settings, \"/nekodex mini\" toggles the mini window, \"/nekodex mini lock\" locks it in place.",
        });

        pluginInterface.UiBuilder.Draw += windowSystem.Draw;
        pluginInterface.UiBuilder.OpenMainUi += mainWindow.Toggle;
        pluginInterface.UiBuilder.OpenConfigUi += configWindow.Toggle;
        clientState.Login += OnLogin;
        clientState.Logout += OnLogout;
        pluginInterface.LanguageChanged += OnLanguageChanged;

        log.Information(
            "NekoDex {Version} loaded (Dalamud API {Api})",
            pluginInterface.Manifest.AssemblyVersion,
            pluginInterface.Manifest.DalamudApiLevel);
    }

    public void Dispose()
    {
        // Mirror every registration in the constructor so reloads leave nothing behind.
        pluginInterface.LanguageChanged -= OnLanguageChanged;
        clientState.Logout -= OnLogout;
        clientState.Login -= OnLogin;
        pluginInterface.UiBuilder.OpenConfigUi -= configWindow.Toggle;
        pluginInterface.UiBuilder.OpenMainUi -= mainWindow.Toggle;
        pluginInterface.UiBuilder.Draw -= windowSystem.Draw;

        commandManager.RemoveHandler(CommandName);
        windowSystem.RemoveAllWindows();
        resetNotifier.Dispose();
        dutyClears.Dispose();

        log.Information("NekoDex unloaded");
    }

    private void OnCommand(string command, string args)
    {
        switch (args.Trim().ToLowerInvariant())
        {
            case "config":
                configWindow.Toggle();
                break;
            case "mini":
                miniWindow.SetShown(!miniWindow.IsOpen);
                break;
            case "mini lock":
                miniWindow.SetLocked(!config.Current.MiniLocked);
                break;
            default:
                mainWindow.Toggle();
                break;
        }
    }

    private void OnLogin()
    {
        resetNotifier.Reset();
        autoItems.Reset();
        mainWindow.ResetCollections();
        if (config.Current.OpenMainWindowOnLogin)
        {
            mainWindow.IsOpen = true;
        }
    }

    private void OnLanguageChanged(string language) => Strings.Apply(pluginInterface, config.Current, log);

    /// <summary>Null follows Dalamud; a code picks that loc file. Applied immediately.</summary>
    private void SetLanguage(string? code)
    {
        config.Current.LanguageOverride = code;
        config.Save();
        Strings.Apply(pluginInterface, config.Current, log);
    }

    private void OnLogout(int type, int code)
    {
        resetNotifier.Reset();
        autoItems.Reset();
        mainWindow.ResetCollections();
    }
}
