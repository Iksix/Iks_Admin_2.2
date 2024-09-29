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
    public void Load(bool hotReload)
    {
        AdminApi.EOnModuleLoad(this);
    }

    public void Unload(bool hotReload)
    {
        AdminApi.EOnModuleUnload(this);
    }
}