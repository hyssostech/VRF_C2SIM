using System.Globalization;

namespace VrfC2SimApp;

/// <summary>
/// HOW MUCH OF A NAME VR-FORCES KEEPS (C1c, 2026-09-28; follow-up of run G1, RL-20260927-03).
///
/// An object the interface creates comes back in its ObjectCreated callback - and in every later completion, console
/// row and text report - under its DIS/RPR MARKING, not under the name we sent. The marking is a fixed field:
///   VR-Link 5.10 vlpi/netStructs.h:39-45  DtNetEntityMarking    { DtCharacterSet; DtText[DtMaxEntityMarkingLength = 11] }
///   VR-Link 5.10 vlpi/netStructs.h:47-53  DtNetAggregateMarking { DtCharacterSet; DtText[DtMaxAggregateMarkingLength = 31] }
///   VR-Link 5.10 vl/aggregateStateRepository.h:33-34 "Aggregate state PDU marking is 31 bytes long"
///                                          (AGGREGATE_SR_MARKING_TEXT_LENGTH=31)
///   VR-Link 5.10 vl/entityStatePdu.h:203   char markingBuff[DtMaxEntityMarkingLength+1] - read back with room for
///                                          the terminator, so a marking that FILLS its field reads back whole.
/// MEASURED, requested against returned length for every created object in the 132 app logs in runs/ (2026-09-28):
///   AGGREGATE (the field is 31)  - a name that FITS comes back WHOLE: 31 characters, 2 of 2 (4ID__FRIENDLY_INFANTRY_
///                                  DIVISION in both G1 runs); a name that OVERFLOWS comes back as its first 30:
///                                  32 to 63 characters, 107 of 107 cut names (entity-level shells and aggregate-profile
///                                  containers alike; in EACH G1 run, 20260928T101531Z and 20260928T102541Z, the 36 init
///                                  containers - 28ID__FRIENDLY_INFANTRY_DIVISION, 32 -> 30 - and the 22 lost members);
///   PLATFORM  (the field is 11)  - 11 characters come back WHOLE, 26 of 26; 14 to 63 come back as their first 10,
///                                  235 of 235 (NameRegistry.MarkingTruncationWidth).
/// So an overflowing name is cut to the field width LESS ONE; why (a terminator reserved only when cutting) is not
/// visible in the headers [A] - the widths are the vendor's, the cut is the measurement. A ROUTE, waypoint or control
/// area is not a DIS entity or aggregate and carries no marking: 237 route names of 31 to 206 characters and control
/// areas of up to 46 ("DOUBLEDAY__FRIENDLY_ASSAULT_POSITION_DOUBLEDAY") came back WHOLE over the same 132 runs.
///
/// WHAT THE LIMIT DOES TO ATTRIBUTION. NameRegistry maps a returned name back to the requested one by UNIQUE PREFIX,
/// so two requested aggregate names that agree in their first 30 characters cannot be told apart once cut - G1 lost 22
/// of its 23 container members that way (the populate planner built "container.suffix" within 34, the old
/// VrfC2SimService.MaxVrfMarkingChars, and "48_IBCT/28ID__FRIENDLY_IN.INF1" came back for four of them). The rule here
/// is therefore: EVERY NAME THE INTERFACE ASKS VR-FORCES FOR IS UNIQUE WITHIN ITS FIRST 30 CHARACTERS (a name of exactly
/// 31, which fits and comes back whole, is treated like a cut one - conservative, it can cost an unneeded ~k tag and
/// never an attribution), and every name the interface MAKES (a container's member, a synthesized sub-unit) is at most
/// 30 characters, so VR-Forces returns it EXACTLY and no prefix scan ever runs for it. NameRegistry.KeyConflict is the
/// check; the ~k tags below are the deterministic way out when two names would coincide.
/// M3b (2026-09-28, the G1-3 Result): a CONTAINER MEMBER's name is shorter still - at most
/// <see cref="PlannedMemberNameChars"/> (16) - because the vendor's navigate-to-location names the route it plans after its
/// taskee and hands that route to a move-along BY a reference that is cut at 35 characters (the route-name budget below).
/// </summary>
public static class VrfNames
{
    /// <summary>The AGGREGATE marking field: DtMaxAggregateMarkingLength (vlpi/netStructs.h:51). A name of at most this
    /// many characters came back whole (2 of 2 at 31).</summary>
    public const int AggregateMarkingField = 31;

    /// <summary>What an AGGREGATE name that overflows its field comes back as: its first 30 characters (107 of 107). The
    /// attribution key, and the longest name the interface makes.</summary>
    public const int AggregateMarkingChars = 30;

    /// <summary>The PLATFORM marking field: DtMaxEntityMarkingLength (vlpi/netStructs.h:43); 11 came back whole, 26 of 26.</summary>
    public const int EntityMarkingField = 11;

    /// <summary>What a PLATFORM name that overflows its field comes back as: its first 10 characters (235 of 235) - the
    /// same number as NameRegistry.MarkingTruncationWidth.</summary>
    public const int EntityMarkingChars = 10;

    /// <summary>The highest ~k disambiguator tried before a name is refused (k = 2 .. this).</summary>
    public const int MaxDisambiguator = 99;

    // ------------------------------------------------------------------------ M3b: the route-name budget ----
    // SEEN LIVE 2026-09-28 (G1-3, run 20260928T190047Z, vrfc2simapp.log L9807-L9875): every container member's
    // navigate-to-location planned a path, created its route and started a move-along on it, and the member's aggregated
    // move-along controller answered "DtAggregatedMoveAlongController::setupRoute -- %1 route does not exist. |
    // 1-112_IN/28ID__FRIENDLY_I.RIF2 Pathr" on 5 of 5 members - so no member moved, and navigate-to-location still ended
    // SUCCESS (navigate-to-location.lua :252-259 ends true once its subtask stops, whatever that subtask's result).
    // THE MECHANISM, from the vendor's own files:
    //   - the script names each route after its taskee: object_name = this:getName() .. " Path part " .. n
    //     (SMS\base\scripts\navigate-to-location.lua :222-225; n is the part, :31 MAX_POINTS_PER_ROUTE = 100);
    //   - a script-created object's uuid is a STRING uuid "<requested object_name>_<counter>" - the vendor's own saves
    //     carry "JAM-137 Path part 1_8" (RoadToKaunasPhaseTwo.oob :103563-103565) and "SS-26 38 Point_73"; the counter's
    //     range is DOCUMENTED NOWHERE (the headers and UG52 are silent), 1..73 across the vendor's saves;
    //   - the move-along task carries its route as a DtUUID (vrftasks/moveAlongTasks.h :79-82 route()/setRoute(const
    //     DtUUID&), "Was: routeName") and a DtUUID is a fixed blob, "the first char is the type, and the rest is the data"
    //     (vrfutil/uuid.h :247-249, char myData[36]) - 35 data bytes; the vendor's own subtask references its route by that
    //     string uuid: (route "VRF_UUID:JAM-137 Path part 1_8") (RoadToKaunasPhaseTwo.oob :72127);
    //   - the aggregated move-along controller looks the route up BY that reference and completes the task when it is not
    //     found (vrfmodel/aggregatedMoveAlongController.h :70-77, :79-81).
    // A 30-character member name makes "<member> Path part 1" 42 characters (+ "_<counter>"): the reference is cut to its
    // first 35 ("1-112_IN/28ID__FRIENDLY_I.RIF2 Path" - the trailing "r" is whatever follows the blob, "incidental to what
    // follows the buffer in memory", PREREG_ROUTE_NAME_LENGTH_2026-09-02 sec 6) and never matches the route. The same cut,
    // on the interface's OWN route names, was MANIPULATED and proven on 2026-09-02 (that prereg: every route name <= 34
    // characters marched, every one >= 36 froze, the task's copy cut at 35; the object itself keeps its full name, UG52
    // 13.2.2 p363 / "A graphical object's name can be up to 255 characters long"). Identity is the uuid (RL-20260928-02), so
    // a member's NAME is free: it is budgeted so that the script's route reference fits.

    /// <summary>The longest route reference a move-along task has carried INTACT: 34 characters (PREREG_ROUTE_NAME_LENGTH
    /// _2026-09-02: <= 34 marched, >= 36 froze; 35 was never bisected). The blob holds 35 data bytes (vrfutil/uuid.h
    /// :247-249); 34 is the measured-safe value.</summary>
    public const int UuidReferenceChars = 34;

    /// <summary>What a DtUUID-carried reference longer than the blob comes back as: its first 35 characters (G1-3: "&lt;30&gt;
    /// Path"; 2026-09-02: "T_R5_CO1_NAMELEN_PROBE_PADDING_TO_3").</summary>
    public const int UuidReferenceCutChars = 35;

    /// <summary>navigate-to-location.lua :223 - the infix between the taskee's name and the route part number.</summary>
    public const string NavigateRouteInfix = " Path part ";

    /// <summary>Digits budgeted for the route part number: 2 (a path of up to 99 routes of MAX_POINTS_PER_ROUTE = 100
    /// points, navigate-to-location.lua :31, :213-235).</summary>
    public const int NavigateRoutePartDigits = 2;

    /// <summary>Characters budgeted for VR-Forces' "_&lt;counter&gt;" on a script-created object's string uuid: "_" + 4 digits
    /// (the vendor's saves show 1..73; the range is undocumented, so 4 digits is the margin).</summary>
    public const int StringUuidSuffixChars = 5;

    /// <summary>THE LONGEST NAME A CONTAINER MEMBER GETS (M3b): 34 - 11 (" Path part ") - 2 - 5 = 16, so the route
    /// reference navigate-to-location's move-along carries for it - "&lt;member&gt; Path part &lt;n&gt;_&lt;counter&gt;" - fits the
    /// 34 characters a move-along has carried intact.</summary>
    public const int PlannedMemberNameChars =
        UuidReferenceChars - 11 - NavigateRoutePartDigits - StringUuidSuffixChars;

    /// <summary>The route name navigate-to-location creates for its taskee (navigate-to-location.lua :223).</summary>
    public static string NavigateRouteName(string member, int part)
        => (member ?? "") + NavigateRouteInfix + part.ToString(CultureInfo.InvariantCulture);

    /// <summary>The reference its move-along carries: the route's string uuid, "&lt;route name&gt;_&lt;counter&gt;".</summary>
    public static string NavigateRouteReference(string member, int part, int counter)
        => NavigateRouteName(member, part) + "_" + counter.ToString(CultureInfo.InvariantCulture);

    /// <summary>What a move-along task carries of <paramref name="reference"/>: whole up to 35 characters, else its first 35.</summary>
    public static string CarriedReference(string reference)
        => string.IsNullOrEmpty(reference) ? "" : reference.Length <= UuidReferenceCutChars ? reference
           : reference.Substring(0, UuidReferenceCutChars);

    /// <summary>Does the worst-case route reference for <paramref name="member"/> (part 99, a 4-digit counter) fit the
    /// intact-carried <see cref="UuidReferenceChars"/>?</summary>
    public static bool RouteReferenceFits(string member)
        => NavigateRouteReference(member, 99, 9999).Length <= UuidReferenceChars;

    /// <summary>
    /// A container's DESIGNATOR, the short tag a member's name starts with (M3b): C2SIM unit names read
    /// "&lt;designator&gt;/&lt;superior&gt;__&lt;description&gt;" or "&lt;designator&gt;__&lt;description&gt;" (Iron Storm: "1-112_IN/28ID__
    /// FRIENDLY_INFANTRY_BATTALION_TASK_FORCE", "4/278_ACR/28ID__...", "28ID__FRIENDLY_INFANTRY_DIVISION"), so it is the
    /// name before its first "__", then before that part's LAST '/' ("1-112_IN", "4/278_ACR", "28ID"); the whole name when
    /// neither separator is there (or would leave nothing).
    /// </summary>
    public static string Designator(string name)
    {
        if (string.IsNullOrEmpty(name)) return "";
        string head = name;
        int dunder = head.IndexOf("__", StringComparison.Ordinal);
        if (dunder > 0) head = head.Substring(0, dunder);
        int slash = head.LastIndexOf('/');
        if (slash > 0) head = head.Substring(0, slash);
        return head.Length > 0 ? head : name;
    }

    /// <summary>A CONTAINER MEMBER's name (M3b): "&lt;designator, cut&gt;[~k].&lt;suffix&gt;" within
    /// <see cref="PlannedMemberNameChars"/> - <see cref="ChildName"/> on the container's <see cref="Designator"/>.</summary>
    public static string MemberName(string container, string suffix, int disambiguator = 0)
        => ChildName(Designator(container), suffix, disambiguator, PlannedMemberNameChars);

    /// <summary>The ATTRIBUTION KEY: the first 30 characters of <paramref name="name"/> - what it comes back as whenever it
    /// overflows the aggregate field. Two names longer than 30 that share it may be indistinguishable once cut.</summary>
    public static string Key(string name)
        => string.IsNullOrEmpty(name) ? "" : name.Length <= AggregateMarkingChars ? name : name.Substring(0, AggregateMarkingChars);

    /// <summary>The MEASURED rule for an AGGREGATE (VR-Forces 5.2d, see the class comment): a name that fits the 31-character
    /// field comes back whole, a longer one as its first 30. The model the offline replays feed the NameRegistry with;
    /// the interface's own checks use the more conservative <see cref="Key"/>.</summary>
    public static string ReturnedAggregateName(string name)
        => string.IsNullOrEmpty(name) ? "" : name.Length <= AggregateMarkingField ? name : name.Substring(0, AggregateMarkingChars);

    /// <summary>The disambiguator tag: "" for k &lt;= 1, else "~k".</summary>
    public static string Tag(int k) => k <= 1 ? "" : "~" + k.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// A MADE name - a container's member or a synthesized sub-unit: "&lt;parent, cut&gt;[~k].&lt;suffix&gt;" within
    /// <paramref name="maxChars"/> (default <see cref="AggregateMarkingChars"/>; a container MEMBER passes
    /// <see cref="PlannedMemberNameChars"/>, M3b), the parent cut from the RIGHT so the suffix (what the object IS) always
    /// survives. The shape VrfC2SimService.MakeChildName has always had, so a child reads as its parent's in every
    /// log; unchanged whenever parent + tail already fit. NULL when the suffix and its tag leave no room for even one
    /// character of the parent.
    /// </summary>
    public static string ChildName(string parent, string suffix, int disambiguator = 0, int maxChars = AggregateMarkingChars)
    {
        string tail = Tag(disambiguator) + "." + (suffix ?? "");
        int room = Math.Min(maxChars, AggregateMarkingChars) - tail.Length;
        if (room < 1) return null;
        string p = parent ?? "";
        if (p.Length > room) p = p.Substring(0, room);
        return p + tail;
    }

    /// <summary>
    /// A C2SIM UNIT name that coincides with another requested name within 30 characters, made unique: its first
    /// (30 - tag) characters + "~k" - at most 30, so VR-Forces returns it exactly.
    /// </summary>
    public static string Disambiguated(string name, int k)
    {
        string tag = Tag(Math.Max(2, k));
        string n = name ?? "";
        return n.Substring(0, Math.Min(n.Length, AggregateMarkingChars - tag.Length)) + tag;
    }

    /// <summary>
    /// The first of ChildName(parent, suffix, k) for k = 0, 2, 3 .. <see cref="MaxDisambiguator"/> that
    /// <paramref name="conflictOf"/> clears (null = no conflict). <paramref name="why"/> is the PLAIN name's conflict
    /// (null when the plain name was free); the result is null when no candidate clears. <paramref name="tag"/> is the
    /// k used (0 = none, -1 = none found).
    /// </summary>
    public static string UniqueChildName(string parent, string suffix, Func<string, string> conflictOf,
                                         out int tag, out string why, int maxChars = AggregateMarkingChars)
    {
        why = null;
        int max = Math.Min(maxChars, AggregateMarkingChars);
        for (int k = 0; k <= MaxDisambiguator; k = k == 0 ? 2 : k + 1)
        {
            string candidate = ChildName(parent, suffix, k, max);
            if (candidate == null)
            {
                why ??= $"the suffix '{suffix}' leaves no room for the parent within {max} characters";
                break;
            }
            string c = conflictOf?.Invoke(candidate);
            if (c == null) { tag = k; return candidate; }
            why ??= $"'{candidate}' {c}";
        }
        tag = -1;
        return null;
    }
}
