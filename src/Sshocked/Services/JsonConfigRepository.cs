using System.Text.Json;
using Sshocked.Interfaces;
using Sshocked.Models;

namespace Sshocked.Services;

public class JsonConfigRepository : IConfigRepository
{
    private readonly string _filePath;

    public JsonConfigRepository()
    {
        var baseDir = Environment.OSVersion.Platform == PlatformID.Win32NT
            ? Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");

        var appDir = Path.Combine(baseDir, "sshocked");
        Directory.CreateDirectory(appDir);
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
