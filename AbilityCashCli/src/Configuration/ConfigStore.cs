using System.Text.Json;

namespace AbilityCashCli.Configuration;

public sealed class ConfigStore : IConfigStore
{
    public string ConfigPath { get; }

    public ConfigStore()
        : this(Path.Combine(AppContext.BaseDirectory, "config.json")) { }

    public ConfigStore(string configPath)
    {
        ConfigPath = configPath;
    }

    public AppConfig Load()
    {
        if (!File.Exists(ConfigPath))
        {
            var defaults = AppConfig.CreateDefault();
            var json = JsonSerializer.Serialize(defaults, AppConfigJsonContext.Default.AppConfig);
            File.WriteAllText(ConfigPath, json);
            return defaults;
        }

        var content = File.ReadAllText(ConfigPath);
        return JsonSerializer.Deserialize(content, AppConfigJsonContext.Default.AppConfig)
            ?? throw new InvalidOperationException($"Не удалось разобрать {ConfigPath}");
    }
}
