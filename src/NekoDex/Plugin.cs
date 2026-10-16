using System;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
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

    // Services arrive through constructor injection; nothing in the plugin reaches for them statically.
    public Plugin(
        IDalamudPluginInterface pluginInterface,
        ICommandManager commandManager,
        IClientState clientState,
        IPlayerState playerState,
        IPluginLog log)
    {
        this.pluginInterface = pluginInterface;
        this.commandManager = commandManager;
        this.clientState = clientState;
        this.log = log;

        config = new ConfigurationManager(pluginInterface, log);

        configWindow = new ConfigWindow(config);
        mainWindow = new MainWindow(clientState, playerState, configWindow.Toggle);
        windowSystem.AddWindow(mainWindow);
        windowSystem.AddWindow(configWindow);

        commandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Toggle the NekoDex window. \"/nekodex config\" opens settings.",
        });

        pluginInterface.UiBuilder.Draw += windowSystem.Draw;
        pluginInterface.UiBuilder.OpenMainUi += mainWindow.Toggle;
        pluginInterface.UiBuilder.OpenConfigUi += configWindow.Toggle;
        clientState.Login += OnLogin;

        log.Information(
            "NekoDex {Version} loaded (Dalamud API {Api})",
            pluginInterface.Manifest.AssemblyVersion,
            pluginInterface.Manifest.DalamudApiLevel);
    }

    public void Dispose()
    {
        // Mirror every registration in the constructor so reloads leave nothing behind.
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
        if (args.Trim().Equals("config", StringComparison.OrdinalIgnoreCase))
        {
            configWindow.Toggle();
            return;
        }

        mainWindow.Toggle();
    }

    private void OnLogin()
    {
        if (config.Current.OpenMainWindowOnLogin)
        {
            mainWindow.IsOpen = true;
        }
    }
}
