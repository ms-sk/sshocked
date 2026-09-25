using System.Text.Json;
using Sshocked.Interfaces;
using Sshocked.Models;

namespace Sshocked.Services;

public class JsonConfigRepository : IConfigRepository
{
    private readonly string _filePath;

    public JsonConfigRepository()
    {
        var appDir = AppContext.BaseDirectory;
        _filePath = Path.Combine(appDir, "appconfig.json");
    }

    public AppConfig Load()
    {
        if (!File.Exists(_filePath))
        {
            var config = new AppConfig { IsFirstStart = true };
            Save(config);
            return config;
        }

        var json = File.ReadAllText(_filePath);
        return JsonSerializer.Deserialize(json, AppConfigContext.Default.AppConfig)
               ?? new AppConfig();
    }

    public void Save(AppConfig config)
    {
        var json = JsonSerializer.Serialize(config, AppConfigContext.Default.AppConfig);
        File.WriteAllText(_filePath, json);
    }
}
