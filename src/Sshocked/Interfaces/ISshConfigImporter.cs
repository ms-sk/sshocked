using Sshocked.Models;

namespace Sshocked.Interfaces;

public interface ISshConfigImporter
{
    List<ServerHost> Import();
}
