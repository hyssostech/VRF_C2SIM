namespace VrfC2SimApp;

/// <summary>
/// LBL (HANDOFF_SEAT_2026-09-28 sec 3 item 5, asked by the owner 2026-09-28) - THE FULL C2SIM DESIGNATION IN THE VENDOR LABEL.
///
/// Every object the interface creates gets a NAME, which VR-Forces keeps only in part: an aggregate's comes back cut to its
/// first 30 characters (the 31-character marking field, VrfNames), a container member's is made at most 16 (M3b, the
/// route-name budget). Until LBL every create also sent label = nullString (VrfFacade.cpp), so the map showed only that cut
/// name. The vendor's LABEL is the identifier for exactly this: "A label is a text string that you can use to identify
/// simulation objects without the restrictions of uniqueness or character length that affect the other identifiers"
/// (UG52 13.2.5 p364; Table 21 p363: "Does not have to be unique ... No limit on character length"). It is an argument of
/// the create itself (vrfRemoteController.h 5.2 :1289 createEntity, :1302 createAggregate; the create message's object
/// label, vrfmsgs/ifCreateVrfObject.h :39-41 "an additional means to identify an object without the strict uniqueness
/// requirement of object name", :134-136) - no separate setLabel (:1354-1359) is needed. It is shown on the map by the Label
/// symbol decoration (UG52 21.2 p470, 21.2.3 p473-474), a GLOBAL GUI setting (UG52 3.7.2 p126), not a scenario one.
///
/// The Label is DISPLAY ONLY and never a key: identity is the uuid (RL-20260928-02, C1d); names are untouched (C1c, M3b).
/// </summary>
public static class DesignationLabel
{
    /// <summary>An init unit's Label: its C2SIM name exactly as STP sent it - never cut, never tagged (~PXY, ~k).</summary>
    public static string ForUnit(string c2simName) => c2simName ?? "";

    /// <summary>A container member's or a synthesized sub-unit's Label: "&lt;parent designation&gt;.&lt;suffix&gt;" - the full
    /// logical name the POPULATE line prints beside the short one (PopulatePlanner.FullMemberName).</summary>
    public static string ForMember(string parentDesignation, string suffix)
        => PopulatePlanner.FullMemberName(parentDesignation, suffix);

    /// <summary>The designation a plan stands for: its Label, or - for a plan that has none - its name.</summary>
    public static string Of(CreationPlan plan) => string.IsNullOrEmpty(plan.Label) ? plan.Name ?? "" : plan.Label;
}

/// <summary>
/// LBL: does the loaded VrfBridge carry the two LABEL overloads the service calls - CreateEntity(..., String uuid, String
/// label) and CreateAggregate(..., AggregateState, Boolean, String uuid, String label)? A managed refresh beside an older
/// VrfBridge.dll would otherwise fail every create with MissingMethodException (RUNBOOK sec 9), so the service refuses to
/// start without them, as C1d does for its uuid overloads (IdentityBridge).
/// </summary>
public static class LabelBridge
{
    public static bool Present(Type bridgeType)
    {
        if (bridgeType == null) return false;
        var entity = bridgeType.GetMethod("CreateEntity", new[]
        {
            typeof(VrfC2Sim.EntityTypeSpec), typeof(VrfC2Sim.Geodetic), typeof(VrfC2Sim.Force), typeof(double),
            typeof(string), typeof(string), typeof(string),
        });
        var aggregate = bridgeType.GetMethod("CreateAggregate", new[]
        {
            typeof(VrfC2Sim.EntityTypeSpec), typeof(VrfC2Sim.Geodetic), typeof(VrfC2Sim.Force), typeof(double),
            typeof(string), typeof(VrfC2Sim.AggregateState), typeof(bool), typeof(string), typeof(string),
        });
        return entity != null && aggregate != null && entity.ReturnType == typeof(void) && aggregate.ReturnType == typeof(void);
    }
}
