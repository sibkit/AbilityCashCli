using System.Text.Json.Serialization;

namespace AbilityCashCli.Configuration;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(AppConfig))]
internal sealed partial class AppConfigJsonContext : JsonSerializerContext;
