using Sshocked.Models;

namespace Sshocked.Interfaces;

public interface IConfigRepository
{
    AppConfig Load();
    void Save(AppConfig config);
}
