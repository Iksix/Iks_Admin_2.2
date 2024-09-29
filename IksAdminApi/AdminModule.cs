using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Commands;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace IksAdminApi;

public abstract class AdminModule : BasePlugin
{
    public static IIksAdminApi AdminApi { get; set; } = null!;

    public override void OnAllPluginsLoaded(bool hotReload)
    {
        AdminApi.EOnModuleLoaded(this);
        AdminApi.SetCommandInititalizer(ModuleName);
        InitializeCommands();
        AdminApi.ClearCommandInitializer();
        AdminApi.OnReady += OnCoreReady;
    }

    public virtual void OnCoreReady()
    {
    }

    public virtual void InitializeCommands()
    {
        // use AdminApi.AddNewCommand(...) here
    }

    public override void Unload(bool hotReload)
    {
        AdminApi.EOnModuleUnload(this);
    }
}