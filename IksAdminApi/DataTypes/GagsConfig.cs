using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace IksAdminApi;


public class GagsConfig : PluginCFG<GagsConfig>, IPluginCFG
{
    public static GagsConfig Config = new GagsConfig();
    public bool TitleToTextInReasons {get; set;} = true; // При прописывании команды например: 'css_gag iks 0 читы', конечная причина будет заменена на Text причины с соотвествующим Title
    public string[] BlockedIdentifiers {get; set;} = ["@all", "@ct", "@t", "@players", "@spec", "@bot"];
    public string[] UnblockBlockedIdentifiers {get; set;} = ["@all", "@ct", "@t", "@players", "@spec", "@bot"];
    public List<GagReason> Reasons { get; set; } = new ()
    {
        new GagReason("Example reason title 1", "Another text for reason", 0, 30, null, false),
        new GagReason("Example reason title 2", banOnAllServers: true, duration: 0),
    };

    public Dictionary<int, string> Times {get; set;} = new()
    {
        {60, "1 мин"},
        {3600, "1 час"},
        {3600 * 24, "1 день"},
        {3600 * 24 * 7, "1 неделя"},
        {3600 * 24 * 30, "1 месяц"},        
        {0, "Навсегда"}        
    };

    public void Set()
    {
        Config = ReadOrCreate<GagsConfig>("gags_cfg", Config);
        AdminUtils.Debug("Gags config loaded ✔");
        AdminUtils.Debug("Reasons count " + Config.Reasons.Count);
    }
}