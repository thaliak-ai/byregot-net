using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using System.Numerics;

namespace ByregotNet;

/// <summary>
/// First-run configuration wizard. Shown automatically until the user completes it.
/// Stays open across reloads (Config.FirstRun flag) so it cannot be skipped by accident.
/// </summary>
public class SetupWindow : Window
{
    private string _solverName = string.Empty;
    private bool   _shareData  = false;

    public SetupWindow() : base(
        "ByregotNet — First-time setup",
        ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.AlwaysAutoResize)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(480, 0),
            MaximumSize = new Vector2(480, float.MaxValue),
        };
        RespectCloseHotkey = false;
    }

    public void SyncFromConfig()
    {
        _solverName = Plugin.Config.SolverName;
        _shareData  = Plugin.Config.ShareData;
    }

    public override void PreDraw()
    {
        // Keep centred every frame regardless of resolution.
        var viewport = ImGui.GetMainViewport();
        var centre   = viewport.GetCenter();
        ImGui.SetNextWindowPos(centre, ImGuiCond.Always, new Vector2(0.5f, 0.5f));
    }

    public override void Draw()
    {
        ImGui.TextUnformatted("Welcome! Before you start, a couple of quick settings.");
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // --- Solver name ---
        ImGui.TextUnformatted("Solver name");
        ImGui.SetNextItemWidth(300);
        ImGui.InputText("##name", ref _solverName, 64);
        ImGui.SameLine();
        ImGui.TextDisabled("(shown in Artisan's solver dropdown)");
        ImGui.Spacing();

        // --- Data sharing ---
        ImGui.Separator();
        ImGui.Spacing();
        ImGui.TextUnformatted("Data sharing (optional)");
        ImGui.Spacing();
        ImGui.PushTextWrapPos(ImGui.GetContentRegionAvail().X);
        ImGui.TextDisabled(
            "To improve the RL model, the plugin can log anonymised craft outcomes " +
            "(recipe level, stats, action sequence, final quality). " +
            "No account info, character name, or personal data is collected. " +
            "You can change this at any time in the plugin settings.");
        ImGui.PopTextWrapPos();
        ImGui.Spacing();
        ImGui.Checkbox("Share anonymised craft data", ref _shareData);
        ImGui.Spacing();

        // --- Confirm ---
        ImGui.Separator();
        ImGui.Spacing();
        if (ImGui.Button("Save & Get Started", new Vector2(180, 0)))
        {
            Plugin.Config.SolverName = _solverName.Trim().Length > 0 ? _solverName.Trim() : "Byregot-Net";
            Plugin.Config.ShareData  = _shareData;
            Plugin.Config.FirstRun   = false;
            Plugin.Config.Save();
            Plugin.Log.Information($"[ByregotNet] Setup complete — solver='{Plugin.Config.SolverName}' shareData={_shareData}");
            IsOpen = false;
        }
    }
}
