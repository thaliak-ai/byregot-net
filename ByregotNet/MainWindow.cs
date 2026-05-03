using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace ByregotNet;

/// <summary>
/// Main plugin window — lets the user configure the solver name shown in Artisan's dropdown.
/// </summary>
public class MainWindow : Window
{
    private string _solverName = string.Empty;
    private bool   _shareData  = false;

    public MainWindow() : base("ByregotNet##main", ImGuiWindowFlags.AlwaysAutoResize)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new System.Numerics.Vector2(360, 80),
            MaximumSize = new System.Numerics.Vector2(600, 300),
        };
    }

    public void SyncFromConfig()
    {
        _solverName = Plugin.Config.SolverName;
        _shareData  = Plugin.Config.ShareData;
    }

    public override void Draw()
    {
        ImGui.TextUnformatted("Embedded models:");
        ImGui.TextUnformatted("  Normal : 1.16.2");
        ImGui.TextUnformatted("  Expert : 1.8-expert");
        ImGui.TextUnformatted("Model is selected automatically based on recipe type.");

        ImGui.Separator();

        ImGui.TextUnformatted("Solver name (Artisan dropdown)");
        ImGui.SetNextItemWidth(280);
        ImGui.InputText("##name", ref _solverName, 64);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.Checkbox("Share anonymised craft data", ref _shareData);

        ImGui.Spacing();

        if (ImGui.Button("Save & Apply"))
        {
            Plugin.Config.SolverName = _solverName.Trim();
            Plugin.Config.ShareData  = _shareData;
            Plugin.Config.Save();
            Plugin.Log.Information($"[ByregotNet] Config saved — solver='{Plugin.Config.SolverName}' shareData={_shareData}");
        }

        ImGui.SameLine();

        if (ImGui.Button("Close"))
            IsOpen = false;
    }
}
