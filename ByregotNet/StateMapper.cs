using System.Text.Json;

namespace ByregotNet;

public static class StateMapper
{
    public static readonly string[] ActionNamesV3 =
    [
        "BasicSynthesis",    // 0
        "CarefulSynthesis",  // 1
        "Groundwork",        // 2
        "RapidSynthesis",    // 3
        "IntensiveSynthesis",// 4
        "MuscleMemory",      // 5
        "PrudentSynthesis",  // 6
        "DelicateSynthesis", // 7
        "BasicTouch",        // 8
        "StandardTouch",     // 9
        "AdvancedTouch",     // 10
        "ByregotsBlessing",  // 11
        "HastyTouch",        // 12
        "PreciseTouch",      // 13
        "PrudentTouch",      // 14
        "PreparatoryTouch",  // 15
        "TrainedFinesse",    // 16
        "Reflect",           // 17
        "TrainedEye",        // 18
        "RefinedTouch",      // 19
        "DaringTouch",       // 20
        "Innovation",        // 21
        "GreatStrides",      // 22
        "Veneration",        // 23
        "WasteNot",          // 24
        "WasteNotII",        // 25
        "Manipulation",      // 26
        "Observe",           // 27
        "TrainedPerfection", // 28
        "MastersMend",       // 29
        "ImmaculateMend",    // 30
    ];

    private static readonly int[] BaseCpCostV3 =
    [
         0,  7, 18,  0,  6,  6, 18, 32,       // 0–7
        18, 32, 46, 24,  0, 18, 25, 40, 32,   // 8–16
         6, 250, 24,  0,                        // 17–20
        18, 32, 18, 56, 98, 96,  7,  0, 88, 112, // 21–30
    ];

    private static readonly int[] BaseDurCostV3 =
    [
        10, 10, 20, 10, 10, 10,  5, 10,  // 0–7
        10, 10, 10, 10, 10, 10,  5, 20,  // 8–15
         0, 10,  0, 10, 10,              // 16–20
         0,  0,  0,  0,  0,  0,  0,  0,  0,  0, // 21–30
    ];

    public static (float[] obs, bool[] mask, bool isExpert) BuildObs(string artisanStateJson, bool splendorCosmic)
    {
        using var doc = JsonDocument.Parse(artisanStateJson);
        var craft     = doc.RootElement.GetProperty("craft");
        var step      = doc.RootElement.GetProperty("step");
        var isExpert  = craft.TryGetProperty("CraftExpert", out var ex) && ex.GetBoolean();
        var modelStep = step.GetProperty("Index").GetInt32() - 1;
        var mask      = BuildActionMaskV3(step, modelStep, craft);
        var obs       = isExpert ? BuildObsExpert(craft, step, modelStep, splendorCosmic) : BuildObsNormal(craft, step, modelStep, splendorCosmic);
        return (obs, mask, isExpert);
    }

    private static float[] BuildObsNormal(JsonElement craft, JsonElement step, int modelStep, bool splendorCosmic)
    {
        var condition    = step.GetProperty("Condition").GetString() ?? "Normal";
        var dur          = step.GetProperty("Durability").GetInt32();
        var cp           = step.GetProperty("RemainingCP").GetInt32();
        var totalDur     = craft.GetProperty("CraftDurability").GetInt32();
        var totalProg    = craft.GetProperty("CraftProgress").GetInt32();
        var totalQual    = craft.GetProperty("CraftQualityMax").GetInt32();
        var expedience   = step.TryGetProperty("ExpedienceLeft",          out var exp) ? exp.GetInt32()  : 0;
        var tpActive     = step.TryGetProperty("TrainedPerfectionActive", out var tp)  && tp.GetBoolean();
        var jobLevel     = craft.TryGetProperty("StatLevel",              out var jl)  ? jl.GetInt32()  : 100;
        var rlvl         = craft.TryGetProperty("RecipeLevel",            out var rl)  ? rl.GetInt32()  : 0;
        var baseProgress = craft.TryGetProperty("BaseProgress",           out var bp)  ? bp.GetInt32()  : 0;
        var baseQuality  = craft.TryGetProperty("BaseQuality",            out var bq)  ? bq.GetInt32()  : 0;

        var progRatio = totalProg > 0 ? Math.Min((float)baseProgress / totalProg, 1f) : 0f;
        var qualRatio = totalQual > 0 ? Math.Min((float)baseQuality  / totalQual, 1f) : 0f;

        var obs = new float[28];
        obs[0]  = Math.Min((float)step.GetProperty("Progress").GetInt32() / totalProg, 1f);
        obs[1]  = Math.Min((float)step.GetProperty("Quality").GetInt32()  / totalQual, 1f);
        obs[2]  = Math.Clamp((float)dur / 80f, 0f, 1f);
        obs[3]  = Math.Clamp((float)cp  / 800f, 0f, 1f);
        obs[4]  = step.GetProperty("IQStacks").GetInt32() / 10f;
        obs[5]  = modelStep / 40f;
        obs[6]  = modelStep == 0 ? 1f : 0f;
        obs[7]  = condition == "Poor"      ? 1f : 0f;
        obs[8]  = condition == "Normal"    ? 1f : 0f;
        obs[9]  = condition == "Good"      ? 1f : 0f;
        obs[10] = condition == "Excellent" ? 1f : 0f;
        obs[11] = step.GetProperty("InnovationLeft").GetInt32()   / 4f;
        obs[12] = step.GetProperty("GreatStridesLeft").GetInt32() / 3f;
        obs[13] = step.GetProperty("VenerationLeft").GetInt32()   / 4f;
        obs[14] = step.GetProperty("WasteNotLeft").GetInt32()     / 8f;
        obs[15] = step.GetProperty("ManipulationLeft").GetInt32() / 8f;
        obs[16] = step.GetProperty("MuscleMemoryLeft").GetInt32() / 5f;
        obs[17] = totalDur  / 80f;
        obs[18] = totalProg / 12000f;
        obs[19] = totalQual / 25000f;
        obs[20] = progRatio;
        obs[21] = qualRatio;
        obs[22] = craft.GetProperty("StatCP").GetInt32() / 800f;
        obs[23] = jobLevel / 100f;
        obs[24] = rlvl     / 800f;
        obs[25] = expedience > 0 ? 1f : 0f;
        obs[26] = tpActive ? 1f : 0f;
        obs[27] = splendorCosmic ? 1.0f : (0.5f / 0.75f); // GoodBonus / 0.75
        return obs;
    }

    private static float[] BuildObsExpert(JsonElement craft, JsonElement step, int modelStep, bool splendorCosmic)
    {
        var condition    = step.GetProperty("Condition").GetString() ?? "Normal";
        var dur          = step.GetProperty("Durability").GetInt32();
        var cp           = step.GetProperty("RemainingCP").GetInt32();
        var totalDur     = craft.GetProperty("CraftDurability").GetInt32();
        var totalProg    = craft.GetProperty("CraftProgress").GetInt32();
        var totalQual    = craft.GetProperty("CraftQualityMax").GetInt32();
        var expedience   = step.TryGetProperty("ExpedienceLeft",          out var exp) ? exp.GetInt32()  : 0;
        var tpActive     = step.TryGetProperty("TrainedPerfectionActive", out var tp)  && tp.GetBoolean();
        var jobLevel     = craft.TryGetProperty("StatLevel",              out var jl)  ? jl.GetInt32()  : 100;
        var rlvl         = craft.TryGetProperty("RecipeLevel",            out var rl)  ? rl.GetInt32()  : 0;
        var condMask     = craft.TryGetProperty("ConditionMask",          out var cm)  ? cm.GetInt32()  : 0x07FE;
        var baseProgress = craft.TryGetProperty("BaseProgress",           out var bp)  ? bp.GetInt32()  : 0;
        var baseQuality  = craft.TryGetProperty("BaseQuality",            out var bq)  ? bq.GetInt32()  : 0;
        var reqQual      = craft.TryGetProperty("CraftRequiredQuality",   out var rq)  ? rq.GetInt32()  : 0;

        var progRatio = totalProg > 0 ? Math.Min((float)baseProgress / totalProg, 1f) : 0f;
        var qualRatio = totalQual > 0 ? Math.Min((float)baseQuality  / totalQual, 1f) : 0f;

        var obs = new float[42];
        obs[0]  = Math.Min((float)step.GetProperty("Progress").GetInt32() / totalProg, 1f);
        obs[1]  = Math.Min((float)step.GetProperty("Quality").GetInt32()  / totalQual, 1f);
        obs[2]  = Math.Clamp((float)dur / 80f, 0f, 1f);
        obs[3]  = Math.Clamp((float)cp  / 800f, 0f, 1f);
        obs[4]  = step.GetProperty("IQStacks").GetInt32() / 10f;
        obs[5]  = modelStep / 40f;
        obs[6]  = modelStep == 0 ? 1f : 0f;
        obs[7]  = condition == "Normal"    ? 1f : 0f;
        obs[8]  = condition == "Good"      ? 1f : 0f;
        obs[9]  = condition == "Centred"   ? 1f : 0f;
        obs[10] = condition == "Sturdy"    ? 1f : 0f;
        obs[11] = condition == "Pliant"    ? 1f : 0f;
        obs[12] = condition == "Malleable" ? 1f : 0f;
        obs[13] = condition == "Primed"    ? 1f : 0f;
        obs[14] = condition == "GoodOmen"  ? 1f : 0f;
        obs[15] = condition == "Robust"    ? 1f : 0f;
        obs[16] = step.GetProperty("InnovationLeft").GetInt32()   / 4f;
        obs[17] = step.GetProperty("GreatStridesLeft").GetInt32() / 3f;
        obs[18] = step.GetProperty("VenerationLeft").GetInt32()   / 4f;
        obs[19] = step.GetProperty("WasteNotLeft").GetInt32()     / 8f;
        obs[20] = step.GetProperty("ManipulationLeft").GetInt32() / 8f;
        obs[21] = step.GetProperty("MuscleMemoryLeft").GetInt32() / 5f;
        obs[22] = totalDur  / 80f;
        obs[23] = totalProg / 12000f;
        obs[24] = totalQual / 25000f;
        obs[25] = progRatio;
        obs[26] = qualRatio;
        obs[27] = craft.GetProperty("StatCP").GetInt32() / 800f;
        obs[28] = jobLevel / 100f;
        obs[29] = rlvl     / 800f;
        obs[30] = expedience > 0 ? 1f : 0f;
        obs[31] = tpActive ? 1f : 0f;
        obs[32] = (condMask & 0x0002) != 0 ? 1f : 0f;  // Good
        obs[33] = (condMask & 0x0010) != 0 ? 1f : 0f;  // Centred
        obs[34] = (condMask & 0x0020) != 0 ? 1f : 0f;  // Sturdy
        obs[35] = (condMask & 0x0040) != 0 ? 1f : 0f;  // Pliant
        obs[36] = (condMask & 0x0080) != 0 ? 1f : 0f;  // Malleable
        obs[37] = (condMask & 0x0100) != 0 ? 1f : 0f;  // Primed
        obs[38] = (condMask & 0x0200) != 0 ? 1f : 0f;  // GoodOmen
        obs[39] = (condMask & 0x0400) != 0 ? 1f : 0f;  // Robust
        obs[40] = splendorCosmic ? 1.0f : (0.5f / 0.75f);                           // good_bonus / 0.75
        obs[41] = totalQual > 0 ? Math.Min((float)reqQual / totalQual, 1f) : 0f;   // required_quality / total_quality
        return obs;
    }

    private static bool[] BuildActionMaskV3(JsonElement step, int modelStep, JsonElement craft)
    {
        var condition    = step.GetProperty("Condition").GetString() ?? "Normal";
        var cp           = step.GetProperty("RemainingCP").GetInt32();
        var durability   = step.GetProperty("Durability").GetInt32();
        var iqStacks     = step.GetProperty("IQStacks").GetInt32();
        var wasteNotLeft = step.GetProperty("WasteNotLeft").GetInt32();
        var prevCombo    = step.GetProperty("PrevComboAction").GetString() ?? "None";
        var expedience   = step.TryGetProperty("ExpedienceLeft",             out var exp) ? exp.GetInt32()  : 0;
        var tpAvailable  = step.TryGetProperty("TrainedPerfectionAvailable", out var tp)  ? tp.GetBoolean() : true;
        var jobLevel     = craft.TryGetProperty("StatLevel",                 out var jl)  ? jl.GetInt32()  : 100;
        var craftLevel   = craft.TryGetProperty("CraftLevel",                out var cl)  ? cl.GetInt32()  : jobLevel;

        var isGoodOrExcellent  = condition is "Good" or "Excellent";
        var isPliant           = condition == "Pliant";
        var isSturdy           = condition == "Sturdy";
        var wasteNotActive     = wasteNotLeft > 0;
        var trainedEyeEligible = jobLevel >= craftLevel + 10;

        var mask = new bool[ActionNamesV3.Length];
        for (var i = 0; i < mask.Length; i++)
            mask[i] = IsValidV3(i, modelStep, cp, durability, iqStacks,
                                wasteNotActive, isGoodOrExcellent, isPliant, isSturdy, prevCombo,
                                expedience, tpAvailable, jobLevel, trainedEyeEligible);
        return mask;
    }

    private static bool IsValidV3(
        int idx, int modelStep, int cp, int durability, int iqStacks,
        bool wasteNotActive, bool isGoodOrExcellent, bool isPliant, bool isSturdy,
        string prevCombo, int expedience, bool tpAvailable, int jobLevel, bool trainedEyeEligible)
    {
        int cpCost = BaseCpCostV3[idx];
        if (idx == 9  && prevCombo == "BasicTouch")    cpCost = 18; // StandardTouch combo
        if (idx == 10 && prevCombo == "StandardTouch") cpCost = 18; // AdvancedTouch combo
        if (isPliant) cpCost /= 2;
        if (cp < cpCost) return false;

        int durCost = BaseDurCostV3[idx];
        if (wasteNotActive) durCost /= 2;
        if (isSturdy)       durCost /= 2;
        if (durCost > 0 && durability <= 0) return false;

        switch (idx)
        {
            case 5:  case 17: if (modelStep != 0) return false; break;                        // MuscleMemory, Reflect — first step only
            case 18: if (modelStep != 0 || !trainedEyeEligible) return false; break;          // TrainedEye
            case 11: if (iqStacks == 0) return false; break;                                   // ByregotsBlessing
            case 6:  case 14: if (wasteNotActive) return false; break;                        // PrudentSynthesis, PrudentTouch
            case 4:  case 13: if (!isGoodOrExcellent) return false; break;                    // IntensiveSynthesis, PreciseTouch
            case 16: if (iqStacks < 10) return false; break;                                   // TrainedFinesse
            case 12: if (jobLevel >= 96 && expedience > 0) return false; break;               // HastyTouch
            case 20: if (expedience == 0) return false; break;                                 // DaringTouch
            case 28: if (!tpAvailable) return false; break;                                    // TrainedPerfection
        }
        return true;
    }
}
