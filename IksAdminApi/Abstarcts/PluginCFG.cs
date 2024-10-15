using System.Text.Json;

namespace IksAdminApi;

public abstract class PluginCFG<IPluginCFG>
{
    public IPluginCFG ReadOrCreate<IPluginCFG>(string fileName, IPluginCFG defaultConfig)
    {
        var modulePath = AdminUtils.AdminApi.Plugin.ModuleDirectory;
        var filePath = modulePath + $"/{fileName}.json";
        if (!File.Exists(filePath))
        {
            AdminUtils.Debug("Creating config file for " + filePath);
            File.WriteAllText(filePath, JsonSerializer.Serialize(defaultConfig, options: new JsonSerializerOptions() { WriteIndented = true, AllowTrailingCommas = true }));
        }
        using var streamReader = new StreamReader(filePath);
        var json = streamReader.ReadToEnd();
        AdminUtils.Debug("Deserialize config file for " + filePath);
        var config = JsonSerializer.Deserialize<IPluginCFG>(json);
        AdminUtils.Debug("Deserialized ✔");
        return config!;
    }
}