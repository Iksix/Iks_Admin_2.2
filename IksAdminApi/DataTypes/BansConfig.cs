using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace IksAdminApi;


public class BansConfig : PluginCFG<BansConfig>, IPluginCFG
{
    public static BansConfig Config = new BansConfig();
    public bool TitleToTextInReasons {get; set;} = true;
    public string[] BlockedIdentifiers {get; set;} = ["@all", "@ct", "@t", "@players", "@spec", "@bot"];
    public List<BanReason> Reasons { get; set; } = new ()
    {
        new BanReason("Example reason title 1", "Another text for reason", 0, 30, null, false),
        new BanReason("Example reason title 2", banOnAllServers: true, duration: 0),
    };

    public void Set()
    {
        Config = ReadOrCreate<BansConfig>("bans_cfg", Config);
        AdminUtils.Debug("Bans config loaded ✔");
        AdminUtils.Debug("Reasons count " + Config.Reasons.Count);
    }
}