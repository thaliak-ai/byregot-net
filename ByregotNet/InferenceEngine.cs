using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace ByregotNet;

/// <summary>
/// In-process ONNX inference for the normal (obs_dim=28) and expert (obs_dim=42) models.
/// Selects model at predict time based on isExpert. InferenceSession is thread-safe.
/// </summary>
public sealed class InferenceEngine : IDisposable
{
    private readonly InferenceSession _normal;
    private readonly InferenceSession _expert;

    public InferenceEngine()
    {
        _normal = LoadEmbedded("ByregotNet.craft_agent_normal.onnx");
        _expert = LoadEmbedded("ByregotNet.craft_agent_expert.onnx");
    }

    private static InferenceSession LoadEmbedded(string resourceName)
    {
        var assembly = typeof(InferenceEngine).Assembly;
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new FileNotFoundException($"Embedded model not found: {resourceName}");
        var bytes = new byte[stream.Length];
        stream.ReadExactly(bytes);
        return new InferenceSession(bytes);
    }

    /// <summary>
    /// Run a forward pass and return the index of the best valid action.
    /// Falls back to BasicSynthesis (0) if no valid action is unmasked.
    /// </summary>
    public int Predict(float[] obs, bool[] actionMask, bool isExpert)
    {
        var session = isExpert ? _expert : _normal;
        var tensor  = new DenseTensor<float>(obs, [1, obs.Length]);
        var input         = NamedOnnxValue.CreateFromTensor("obs", tensor);
        using var results = session.Run([input]);
        var logits = results[0].AsEnumerable<float>().ToArray();

        var best      = -1;
        var bestScore = float.NegativeInfinity;
        for (var i = 0; i < logits.Length; i++)
        {
            if (actionMask[i] && logits[i] > bestScore)
            {
                bestScore = logits[i];
                best      = i;
            }
        }
        return best >= 0 ? best : 0;
    }

    public void Dispose()
    {
        _normal.Dispose();
        _expert.Dispose();
    }
}
