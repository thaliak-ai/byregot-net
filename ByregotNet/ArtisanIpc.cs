using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using System.Collections.Generic;

namespace ByregotNet;

/// <summary>
/// Wraps Artisan IPC endpoints used by the plugin.
/// </summary>
public class ArtisanIpc : IDisposable
{
    private readonly ICallGateSubscriber<string, Func<string, string>, List<string>, int, object?> _registerSolver;
    private readonly ICallGateSubscriber<bool> _getSplendorCosmic;

    private static readonly List<string> RequestedFields =
    [
        "BaseProgress",
        "BaseQuality",
        "CraftRequiredQuality",
    ];

    public ArtisanIpc(IDalamudPluginInterface pi)
    {
        _registerSolver    = pi.GetIpcSubscriber<string, Func<string, string>, List<string>, int, object?>("Artisan.RegisterExternalSolver");
        _getSplendorCosmic = pi.GetIpcSubscriber<bool>("Artisan.GetSplendorCosmic");
    }

    public void Register(string name, Func<string, string> callback)
    {
        try
        {
            // CraftSupport.Normal | Expert | Cosmic = 7 (All)
            _registerSolver.InvokeAction(name, callback, RequestedFields, 7);
        }
        catch (Dalamud.Plugin.Ipc.Exceptions.IpcNotReadyError)
        {
            throw new InvalidOperationException(
                "Artisan IPC endpoint 'Artisan.RegisterExternalSolver' is not available. " +
                "Artisan is either not loaded or does not yet support external solvers with field declarations. " +
                "See: https://github.com/PunishXIV/Artisan (PR pending).");
        }
    }

    /// <summary>
    /// Returns true if the player has a Splendorous or Cosmic tool equipped
    /// (Good condition bonus 1.75× instead of 1.5×). Returns false if Artisan
    /// is not loaded or the IPC is unavailable.
    /// </summary>
    public bool GetSplendorCosmic()
    {
        try   { return _getSplendorCosmic.InvokeFunc(); }
        catch { return false; }
    }

    public void Dispose()
    {
        // Artisan removes all external solvers when it unloads; nothing to unregister here.
    }
}
