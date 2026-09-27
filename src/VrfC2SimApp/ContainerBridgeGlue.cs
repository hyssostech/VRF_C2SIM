using VrfC2Sim;

namespace VrfC2SimApp;

/// <summary>
/// C1's two touches of the native bridge (RL-20260927-03), kept out of the pure population logic so that logic loads
/// without VrfBridge.dll.
///
/// THE PUBLICATION READER IS BOUND BY REFLECTION, ON PURPOSE. VrfBridge.PublishedSubordinateCount is a NEW native
/// member (VrfFacade.cpp; design sec 8 item 4) and this machine has no C++ toolset to rebuild the bridge (VS 18
/// Community carries no VC\Tools\MSVC and no VC targets - MSBuild MSB4019 on Microsoft.Cpp.Default.props,
/// 2026-09-27), so the managed code must compile against the PINNED bridge (90272bc9..., which lacks it). Binding by
/// name at run time - the tools/PauseSim precedent for BackendControlState (RUNBOOK sec 9) - lets one managed build
/// run against either bridge: with the rebuilt bridge the gate reads the count; with the pinned one the member is
/// ABSENT, and on the aggregate model set the interface then REFUSES TO START (a population gate that cannot see the
/// members is not a gate). EntityLevel never looks the member up.
/// </summary>
public static class PublishedCountReader
{
    public const string MemberName = "PublishedSubordinateCount";

    /// <summary>A delegate over the bridge's PublishedSubordinateCount(string) -> int, or null when the loaded
    /// VrfBridge.dll predates it.</summary>
    public static Func<string, int> Bind(object bridge)
    {
        if (bridge == null) return null;
        var m = bridge.GetType().GetMethod(MemberName, new[] { typeof(string) });
        if (m == null || m.ReturnType != typeof(int)) return null;
        return (Func<string, int>)m.CreateDelegate(typeof(Func<string, int>), bridge);
    }

    /// <summary>Does this bridge TYPE carry the member (the self-test reads the linked assembly's type)?</summary>
    public static bool Present(Type bridgeType)
        => bridgeType?.GetMethod(MemberName, new[] { typeof(string) }) is { } m && m.ReturnType == typeof(int);
}

/// <summary>The bridge-free scripted-task variables, as the bridge's ScriptVar (the vendor's DtRw* bindings:
/// Object -> "simulationobject", Flag -> "checkbox", Location -> "location"; VrfFacade.cpp addScriptVar).</summary>
public static class ContainerScriptVars
{
    public static List<ScriptVar> ToBridge(IReadOnlyList<ContainerTaskVar> vars)
    {
        var outp = new List<ScriptVar>(vars?.Count ?? 0);
        if (vars == null) return outp;
        foreach (var v in vars)
            outp.Add(v.Kind switch
            {
                ContainerTaskVarKind.Object => ScriptVar.Object(v.Name, v.Text ?? ""),
                ContainerTaskVarKind.Flag => ScriptVar.Flag(v.Name, v.Flag),
                _ => ScriptVar.Place(v.Name, new Geodetic { LatDeg = v.Lat, LonDeg = v.Lon, AltMeters = v.Alt }),
            });
        return outp;
    }
}

/// <summary>The population state machine's view of VR-Forces, over the real bridge. TICK THREAD ONLY - every call
/// here is a bridge call (the facade is single-threaded; VrfC2SimService's class comment).</summary>
public sealed class ContainerBridgeAdapter : IContainerBridge
{
    private readonly VrfBridge _bridge;
    private readonly Func<string, int> _count;

    public ContainerBridgeAdapter(VrfBridge bridge, Func<string, int> publishedCount)
    {
        _bridge = bridge;
        _count = publishedCount;
    }

    public bool CanReadPublication => _count != null;

    public void AddToOrganization(string childUuid, string superiorUuid) => _bridge.AddToOrganization(childUuid, superiorUuid);

    public int PublishedSubordinateCount(string aggregateUuid)
    {
        if (_count == null || string.IsNullOrEmpty(aggregateUuid)) return -1;
        try { return _count(aggregateUuid); }
        catch { return -1; }   // a read that throws is "no reading", never "published"
    }

    public void RunScriptedTask(string uuid, string scriptId, IReadOnlyList<ContainerTaskVar> vars)
        => _bridge.RunScriptedTask(uuid, scriptId, ContainerScriptVars.ToBridge(vars));

    /// <summary>Populating in place never deletes (RL-20260927-03). Present for the interface only; a call is a
    /// defect and says so loudly instead of deleting a unit with its members.</summary>
    public void DeleteObject(string uuid)
        => throw new InvalidOperationException($"C1 never deletes an object (RL-20260927-03) - refused for {uuid}");
}
