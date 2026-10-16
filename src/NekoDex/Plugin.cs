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
    private readonly GlamourDresserReader dresser;
    private readonly AutoChecklistController autoItems;

    // Services arrive through constructor injection; nothing in the plugin reaches for them statically.
    public Plugin(
        IDalamudPluginInterface pluginInterface,
        ICommandManager commandManager,
        IClientState clientState,
        IPlayerState playerState,
        IDataManager dataManager,
        ITextureProvider textureProvider,
        IPluginLog log)
    {
        this.pluginInterface = pluginInterface;
        this.commandManager = commandManager;
        this.clientState = clientState;
        this.log = log;

        Strings.Setup(pluginInterface, log);
        config = new ConfigurationManager(pluginInterface, log);

        // Unlock info comes from the bundled dataset; anything it cannot answer becomes an explicit
        // Unknown with a web link for the configured site. Entries newer than the data also fall back to the link.
        var dataset = BundledUnlockDataset.Load(log);
        var itemSources = new ItemSourceProvider(dataManager, log);
        IUnlockSourceProvider[] providers = dataset is null ? [itemSources] : [dataset, itemSources];
        var unlockResolver = new UnlockResolver(providers, () => config.Current.LinkSite);

        // One tab per collection module. Adding a module means one line here plus its class in Data/.
        var pins = new PinController(config);
        var armoire = new ArmoireModule(dataManager, log);
        CollectionTab[] tabs =
        [
            new(new MountModule(dataManager, clientState, log), textureProvider, unlockResolver, dataset, pins),
            new(new MinionModule(dataManager, clientState, log), textureProvider, unlockResolver, dataset, pins),
            new(new OrchestrionModule(dataManager, log), textureProvider, unlockResolver, dataset, pins),
            new(new EmoteModule(dataManager, log), textureProvider, unlockResolver, dataset, pins),
            new(new BardingModule(dataManager, log), textureProvider, unlockResolver, dataset, pins),
            new(new FashionAccessoryModule(dataManager, clientState, log), textureProvider, unlockResolver, dataset, pins),
            new(new FacewearModule(dataManager, log), textureProvider, unlockResolver, dataset, pins),
            new(new TriadCardModule(dataManager, log), textureProvider, unlockResolver, dataset, pins),
            new(armoire, textureProvider, unlockResolver, dataset, pins),
        ];
        bool CharacterReady() => clientState.IsLoggedIn && playerState.IsLoaded;

        // Outfit planner: item catalogue from the Item sheet, ownership from dresser snapshot, armoire and bags.
        dresser = new GlamourDresserReader(config, playerState, log);
        var gearCatalog = new GearCatalog(dataManager, log);
        var itemOwnership = new ItemOwnershipSources(dresser, armoire);
        var outfits = new OutfitsTab(config, gearCatalog, new StainCatalog(dataManager), itemOwnership, dresser, textureProvider);

        // Gear advisor (Phase 7, simple scope): next upgrade by item level per slot for the current job and level.
        var advisor = new AdvisorTab(playerState, gearCatalog, itemOwnership, unlockResolver, textureProvider, new TomestoneShopIndex(dataManager, log), CurrencyReader.Tomestones);

        // Resets are fixed in UTC, so the local clock is enough; the checklist controller stamps and clears with it.
        var checklist = new ChecklistController(config, () => DateTimeOffset.UtcNow);
        autoItems = new AutoChecklistController(config, new AutoChecklistSources(dataManager, log), CharacterReady, () => DateTimeOffset.UtcNow);
        var navigation = new TabNavigation();
        var dashboard = new DashboardTab(checklist, autoItems, tabs, CharacterReady, navigation);

        miniWindow = new MiniWindow(config, checklist, autoItems, tabs, pins, CharacterReady);
        var version = pluginInterface.Manifest.AssemblyVersion.ToString();
        configWindow = new ConfigWindow(config, miniWindow.SetShown, dataset, version, autoItems, new UserDataService(config, log, version));
        mainWindow = new MainWindow(config, clientState, playerState, dashboard, tabs, outfits, advisor, navigation, configWindow.Toggle);
        windowSystem.AddWindow(mainWindow);
        windowSystem.AddWindow(configWindow);
        windowSystem.AddWindow(miniWindow);

        commandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Toggle the NekoDex window. \"/nekodex config\" opens settings, \"/nekodex mini\" toggles the mini window.",
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
            default:
                mainWindow.Toggle();
                break;
        }
    }

    private void OnLogin()
    {
        dresser.Reset();
        autoItems.Reset();
        mainWindow.ResetCollections();
        if (config.Current.OpenMainWindowOnLogin)
        {
            mainWindow.IsOpen = true;
        }
    }

    private void OnLanguageChanged(string language) => Strings.Apply(language, pluginInterface, log);

    private void OnLogout(int type, int code)
    {
        dresser.Reset();
        autoItems.Reset();
        mainWindow.ResetCollections();
    }
}
