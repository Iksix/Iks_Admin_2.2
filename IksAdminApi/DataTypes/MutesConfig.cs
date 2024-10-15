using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace IksAdminApi;


public class MutesConfig : PluginCFG<MutesConfig>, IPluginCFG
{
    public static MutesConfig Config = new MutesConfig();
    public bool TitleToTextInReasons {get; set;} = true;
    public string[] BlockedIdentifiers {get; set;} = ["@all", "@ct", "@t", "@players", "@spec", "@bot"];
    public string[] UnblockBlockedIdentifiers {get; set;} = ["@all", "@ct", "@t", "@players", "@spec", "@bot"];
    public List<MuteReason> Reasons { get; set; } = new ()
    {
        new MuteReason("Example reason title 1", "Another text for reason", 0, 30, null, false),
        new MuteReason("Example reason title 2", banOnAllServers: true, duration: 0),
    };

    public void Set()
    {
        Config = ReadOrCreate<MutesConfig>("mutes_cfg", Config);
        AdminUtils.Debug("Mutes config loaded ✔");
        AdminUtils.Debug("Reasons count " + Config.Reasons.Count);
    }
}