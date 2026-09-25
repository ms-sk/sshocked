using System.Text.Json.Serialization;

namespace Sshocked.Models;

[JsonSerializable(typeof(AppConfig))]
[JsonSerializable(typeof(ServerGroup))]
[JsonSerializable(typeof(ServerHost))]
[JsonSerializable(typeof(AuthType))]
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
public partial class AppConfigContext : JsonSerializerContext
{
}
