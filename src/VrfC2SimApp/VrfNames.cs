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
    /// <see cref="AggregateMarkingChars"/>, the parent cut from the RIGHT so the suffix (what the object IS) always
    /// survives. The shape VrfC2SimService.MakeChildName has always had, so a child reads as its parent's in every
    /// log; unchanged whenever parent + tail already fit. NULL when the suffix and its tag leave no room for even one
    /// character of the parent.
    /// </summary>
    public static string ChildName(string parent, string suffix, int disambiguator = 0)
    {
        string tail = Tag(disambiguator) + "." + (suffix ?? "");
        int room = AggregateMarkingChars - tail.Length;
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
                                         out int tag, out string why)
    {
        why = null;
        for (int k = 0; k <= MaxDisambiguator; k = k == 0 ? 2 : k + 1)
        {
            string candidate = ChildName(parent, suffix, k);
            if (candidate == null)
            {
                why ??= $"the suffix '{suffix}' leaves no room for the parent within {AggregateMarkingChars} characters";
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
