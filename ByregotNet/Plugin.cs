using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace ByregotNet;

public sealed class Plugin : IDalamudPlugin
{
    internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    internal static IPluginLog              Log             { get; private set; } = null!;
    internal static Configuration           Config          { get; private set; } = null!;
    internal static InferenceEngine         Inference       { get; private set; } = null!;

    private readonly ArtisanIpc   _artisanIpc;
    private readonly WindowSystem _windowSystem = new("ByregotNet");
    private readonly MainWindow   _mainWindow   = new();
    private readonly SetupWindow  _setupWindow  = new();

    public Plugin(IDalamudPluginInterface pi, IPluginLog log)
    {
        PluginInterface = pi;
        Log             = log;
        Config          = pi.GetPluginConfig() as Configuration ?? new Configuration();
        Inference       = new InferenceEngine();

        _artisanIpc = new ArtisanIpc(pi);
        _artisanIpc.Register(Config.SolverName, SolverCallback);

        _windowSystem.AddWindow(_mainWindow);
        _windowSystem.AddWindow(_setupWindow);
        pi.UiBuilder.Draw         += DrawUI;
        pi.UiBuilder.OpenMainUi   += OpenMainUI;
        pi.UiBuilder.OpenConfigUi += OpenConfigUI;

        if (Config.FirstRun)
        {
            _setupWindow.SyncFromConfig();
            _setupWindow.IsOpen = true;
        }

        Log.Information($"[ByregotNet] Solver '{Config.SolverName}' registered — embedded ONNX models loaded.");
    }

    private void DrawUI()      => _windowSystem.Draw();
    private void OpenMainUI()  { _mainWindow.SyncFromConfig(); _mainWindow.IsOpen = true; }
    private void OpenConfigUI() => OpenMainUI();

    private int  _stepCount     = 0;
    private bool _splendorCosmic = false;

    private string SolverCallback(string artisanStateJson)
    {
        try
        {
            using var peek = System.Text.Json.JsonDocument.Parse(artisanStateJson);
            var stepIndex = peek.RootElement.GetProperty("step").GetProperty("Index").GetInt32();
            if (stepIndex == 1)
            {
                _stepCount     = 0;
                _splendorCosmic = _artisanIpc.GetSplendorCosmic();
            }

            if (_stepCount < 3)
            {
                Log.Debug($"[ByregotNet] step {stepIndex} artisan_state={artisanStateJson}");
                _stepCount++;
            }

            var (obs, mask, isExpert) = StateMapper.BuildObs(artisanStateJson, _splendorCosmic);
            var actionIdx = Inference.Predict(obs, mask, isExpert);
            return $"{{\"action\":\"{StateMapper.ActionNamesV3[actionIdx]}\"}}";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[ByregotNet] Solver callback failed — returning None (craft will pause)");
            return """{"action":"None"}""";
        }
    }

    public void Dispose()
    {
        PluginInterface.UiBuilder.Draw         -= DrawUI;
        PluginInterface.UiBuilder.OpenMainUi   -= OpenMainUI;
        PluginInterface.UiBuilder.OpenConfigUi -= OpenConfigUI;
        _artisanIpc.Dispose();
        Inference.Dispose();
    }
}
