using Dalamud.Configuration;

namespace ByregotNet;

public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    /// <summary>False until the user completes the first-run setup wizard.</summary>
    public bool FirstRun { get; set; } = true;

    /// <summary>Name shown in Artisan's solver dropdown.</summary>
    public string SolverName { get; set; } = "Byregot-Net";

    /// <summary>Whether the user opted in to sharing anonymised craft outcome data.</summary>
    public bool ShareData { get; set; } = true;

    public void Save() => Plugin.PluginInterface.SavePluginConfig(this);
}
