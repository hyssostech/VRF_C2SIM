namespace VrfC2SimApp;

/// <summary>
/// HOW FAR APART CO-LOCATED UNITS OF ONE ECHELON MUST BE SPREAD (user ruling 2026-09-21,
/// option C of the D6 harvest: "spread composed siblings at the ECHELON's own scale").
///
/// THE RULING'S OWN ARITHMETIC, APPLIED PER ECHELON. C14 (user ruling 2026-09-07,
/// docs/experiments/PREREG_ASSEMBLY_LAYOUT_2026-09-07.md sec 1) sized its 700 m so that it is
/// "larger than the longest shipped company formation (630 m) so no two units' default formations
/// can overlap whatever their heading". That is an echelon-relative criterion with a COMPANY
/// number in it. A PLATOON laid out in a platoon formation needs a platoon-sized ring, and using
/// the company number on platoons is what put a whole platoon under the STP-837 traversal bar in
/// run D3 (spread 700 m from a destination 1,097 m away: 700 &gt; 0.5 x 1097, so its members could
/// never show the displacement the bar demanded - d6_harvest_report.md P2.5).
///
/// WHERE THE NUMBERS COME FROM - the same source the 630 m figure came from, read more carefully.
/// The shipped formation files are
/// C:\MAK\vrforces5.2d\data\simulationModelSets\EntityLevel\vrfSim\formation\*.frm. Each entry
/// carries (promotion-id N) (leader-promotion-id M) (position-offset x y z), and the offset is
/// RELATIVE TO THAT ENTRY'S LEADER - so a formation's true extent is the resolved chain, not the
/// largest single offset. SPAN below is the maximum pairwise distance between resolved slots,
/// which is the figure the ruling's "whatever their heading" clause asks for (heading rotates the
/// whole pattern, so only the diameter matters).
///
/// *** THE TABLE IS THE GROUND SUBSET, AND SAYING SO IS THE POINT (SF-3, cold-start review of
/// 35a13f2). *** It is measured over the formations that the shipped EntityLevel .entity templates
/// OF THAT ECHELON actually reference, RESTRICTED TO GROUND templates (scratchpad frm_spans.py /
/// frm_by_template.py, 2026-09-21). Re-run WITHOUT that restriction the same script returns
/// SECTION = 3,000 m (Air Section) and SQUAD = 12,649 m (Fighter Squadron - the name classifier
/// matches "Squadron"): the air rows are an order of magnitude bigger and are NOT in this table.
/// <see cref="KeyOf"/> therefore refuses to hand a unit whose SIDC battle dimension is AIR, SPACE,
/// SEA SURFACE or SUBSURFACE a ground row - it returns "", the documented fallback
/// (Vrf:DeStackEchelonFallbackMeters, default 0 = "do not spread") - and every call reports WHICH
/// ROW IT USED AND WHY through <see cref="KeyOf(string,string,out string)"/>, which the de-stack
/// logs per init. Before SF-3 an air section took the 300 m ground SECTION row in silence. No
/// shipped fixture contains an air or naval aggregate, so this changes nothing measured; it closes
/// the surface the next STP export can walk into.
///
/// The rows:
///
///   echelon    span    longest shipped formation for it        via template
///   TEAM        24.6 m Formation-Wedge-US-Army-LtInf-FT.frm    Infantry Fire Team (USA)
///   SQUAD      110.0 m Formation-Column-US-Army-LtInf-SQD.frm  Rifle Squad (USA Army)
///   SECTION    255.0 m Ar_Plt_US_Column.frm                    Mortar Section (US Army M1064)
///   PLATOON    320.9 m Formation-Column-US-Army-Mech-Plt-w-IFV.frm  Mechanized Platoon (USA) IFV
///   (company)  660.0 m Formation-Column-Armor-Co(US).frm       Tank Company (USA)
///
/// NOTE ON THE RULING'S 630: Formation-Column-Armor-Co(US).frm's raw offsets are 0, +200, -430,
/// -230 with each relative to the previous entry, so the resolved slots are 0, +200, -230, -460 -
/// a span of 660 m, not 630. The ruled 700 m still clears it, so C14's conclusion stands; the
/// figure in the prereg is 30 m low. Recorded here rather than silently corrected there.
///
/// SPACING = the next 50 m step strictly above the span, floored at 100 m (a spread smaller than
/// that is not worth the parity cost - a vehicle is ~7 m). Derived once, written down as a
/// constant, and re-asserted against its span by --destack-selftest, so the table cannot drift
/// from its own arithmetic.
///
/// COMPANY AND ABOVE ARE DELIBERATELY NOT IN THE TABLE. They fall back to the configured
/// Vrf:DeStackSpacingMeters (700 m), which is the value the user RULED on 2026-09-07 and the one
/// PREREG_ASSEMBLY_LAYOUT confirmed live (BlockedByVehicle 63 vs 4,828). The vendor does ship
/// company- and battalion-echelon formations that are LONGER than 700 m -
/// Mechanized_CO_US_Column.frm 1,400 m, FA_M109_Bty_Line.frm 2,250 m,
/// Mechanized_BN_US_Column.frm 4,500 m - so the ruling's geometric criterion is not universally
/// met at those echelons. Raising the ruled spacing to 4.5 km is a new ruling with live
/// consequences (it is also the R &lt;= 0.5 L collision of the D6 harvest, at ten times the size),
/// and this lane does not make it. Dissent recorded, work proceeds under the ruling.
///
/// PURE: no I/O, no clock, no bridge. Locked by --destack-selftest.
/// </summary>
public static class EchelonSpacing
{
    public const string Team = "TEAM";
    public const string Squad = "SQUAD";
    public const string Section = "SECTION";
    public const string Platoon = "PLATOON";

    /// <summary>The measured longest shipped formation span for each echelon in the table, in
    /// meters - the evidence each spacing is derived from (see the class remarks for the file
    /// each figure was measured on).</summary>
    public static readonly IReadOnlyDictionary<string, double> SpanMeters =
        new Dictionary<string, double>(StringComparer.Ordinal)
        {
            [Team] = 24.6,
            [Squad] = 110.0,
            [Section] = 255.0,
            [Platoon] = 320.9,
        };

    /// <summary>The derived ring spacing per echelon: the next 50 m step strictly above that
    /// echelon's span, floored at 100 m.</summary>
    public static readonly IReadOnlyDictionary<string, double> TableMeters =
        new Dictionary<string, double>(StringComparer.Ordinal)
        {
            [Team] = 100.0,
            [Squad] = 150.0,
            [Section] = 300.0,
            [Platoon] = 350.0,
        };

    /// <summary>The rounding rule the table is built with, exposed so the self-test can re-derive
    /// every constant from its span instead of copying it.</summary>
    public static double StepAbove(double spanMeters)
    {
        if (double.IsNaN(spanMeters) || spanMeters <= 0.0) return 100.0;
        double step = Math.Ceiling((spanMeters + 1e-9) / 50.0) * 50.0;
        if (step <= spanMeters) step += 50.0;          // exact multiples of 50 must still be cleared
        return Math.Max(100.0, step);
    }

    /// <summary>
    /// THE SIDC BATTLE DIMENSION, APP6C/2525B position 3 (index 2) of the 15-character symbol id:
    /// P space, A air, G ground, S sea surface, U sea subsurface, F SOF, X other, Z unknown.
    /// '\0' when the string is absent or too short. (Position 12 / index 11 is the ECHELON, which
    /// <see cref="UnitTypeMap.EchelonCharOf"/> reads - a different field entirely.)
    /// </summary>
    public static char BattleDimensionOf(string sidc)
        => string.IsNullOrEmpty(sidc) || sidc.Length < 3 ? '\0' : char.ToUpperInvariant(sidc[2]);

    /// <summary>
    /// Is this unit one the GROUND-measured table may size at all (SF-3)? Ground 'G' and SOF 'F'
    /// yes - SOF units in this catalogue are ground templates. Air 'A', space 'P', sea surface 'S'
    /// and subsurface 'U' NO: their shipped formations are 3,000-12,649 m and the table does not
    /// carry them. Anything else - 'X' other, 'Z' unknown, a missing or short SIDC - is UNKNOWN
    /// DOMAIN and is treated as ground, which is what every shipped fixture is and what the
    /// pre-SF-3 code did for all of them; the provenance string says which of the two it was, so
    /// "we assumed ground" is never silent.
    /// </summary>
    public static bool TableCoversDomain(char battleDimension)
        => battleDimension is not ('A' or 'P' or 'S' or 'U');

    /// <summary>
    /// THE ECHELON KEY for one C2SIM unit, from the init's own fields: the explicit
    /// Unit/EchelonCode first (mandatory in the schema and present in every fixture we ship), the
    /// SIDC echelon character (APP6C position 12) as the cross-check when the code says nothing.
    /// Returns "" for every echelon the table does not cover - company and above, air and naval
    /// echelons (FLIGHT, WING, SQDRNA/SQDRNM, FLEET), the schema's own "not specified" values
    /// (NOS, NKN), and - since SF-3 - EVERY unit whose SIDC battle dimension is air, space or
    /// naval, whatever its echelon code says. "" means FALLBACK, and the fallback is the caller's
    /// to apply.
    /// </summary>
    public static string KeyOf(string echelonCode, string sidc) => KeyOf(echelonCode, sidc, out _);

    /// <summary>
    /// The same answer, plus WHICH ROW WAS USED AND WHY, for the caller to log (SF-3). The
    /// provenance is a short human sentence, never parsed.
    /// </summary>
    public static string KeyOf(string echelonCode, string sidc, out string provenance)
    {
        string code = (echelonCode ?? "").Trim().ToUpperInvariant();
        char dim = BattleDimensionOf(sidc);
        string key = code switch
        {
            "TEAM" => Team,
            "SQUAD" => Squad,
            "SECT" => Section,
            "PLT" => Platoon,
            // SIDC position 12 (index 11): A crew/team, B squad, C section, D platoon/detachment.
            // E and above are company/battalion/regiment/brigade/... - not in the table.
            _ => UnitTypeMap.EchelonCharOf(sidc ?? "") switch
            {
                'A' => Team,
                'B' => Squad,
                'C' => Section,
                'D' => Platoon,
                _ => "",
            },
        };
        string from = code.Length > 0 && key.Length > 0
                      && code is "TEAM" or "SQUAD" or "SECT" or "PLT"
                          ? "EchelonCode '" + code + "'"
                          : key.Length > 0
                              ? "SIDC echelon character '" + UnitTypeMap.EchelonCharOf(sidc ?? "") + "'"
                              : "EchelonCode '" + code + "' / SIDC echelon character '"
                                + UnitTypeMap.EchelonCharOf(sidc ?? "") + "'";
        if (key.Length == 0)
        {
            provenance = from + " -> NO ROW (company and above, an air/naval echelon, or 'not "
                       + "specified') - the caller's fallback applies";
            return "";
        }
        if (!TableCoversDomain(dim))
        {
            provenance = from + " -> " + key + ", but the SIDC BATTLE DIMENSION is '" + dim
                       + "' (air/space/naval) and this table is the GROUND subset (the unrestricted "
                       + "max is SECTION 3,000 m / SQUAD 12,649 m) - NO ROW IS USED, the caller's "
                       + "fallback applies";
            return "";
        }
        provenance = from + " -> the GROUND " + key + " row ("
                   + (TableMeters.TryGetValue(key, out double m)
                      ? m.ToString("F0", System.Globalization.CultureInfo.InvariantCulture) + " m"
                      : "no spacing")
                   + "); battle dimension '"
                   + (dim == '\0' ? "(none in the SIDC)" : dim.ToString())
                   + (dim is 'G' or 'F' ? "' is ground" : "' is not stated as air/naval, so ground is assumed");
        return key;
    }

    /// <summary>
    /// THE SPACING FOR AN ECHELON KEY. An unknown key (or one the table does not cover) returns
    /// <paramref name="fallbackMeters"/>, which the caller documents: the de-stack passes
    /// Vrf:DeStackEchelonFallbackMeters, whose default 0 means "do not spread a group whose
    /// echelon the table cannot size" - the conservative direction, because the alternative is to
    /// spread a company's synthesized platoons at the 700 m company spacing, which is exactly the
    /// D3 defect this table exists to remove.
    /// </summary>
    public static double SpacingFor(string key, double fallbackMeters)
        => !string.IsNullOrEmpty(key) && TableMeters.TryGetValue(key, out double m) ? m : fallbackMeters;

    /// <summary>
    /// OPTIONAL OPERATOR OVERRIDE, Vrf:DeStackEchelonSpacingMeters: "PLT=400,SECT=250". Empty (the
    /// default) leaves the derived table exactly as measured. Unknown keys and unparsable values
    /// are IGNORED and named in <paramref name="note"/> so a typo is visible in the start-up line
    /// rather than silently ineffective. Returns a NEW table; the static one is never mutated.
    /// </summary>
    public static IReadOnlyDictionary<string, double> WithOverrides(string spec, out string note)
    {
        var table = new Dictionary<string, double>(TableMeters, StringComparer.Ordinal);
        note = "";
        if (string.IsNullOrWhiteSpace(spec)) return table;
        var applied = new List<string>();
        var ignored = new List<string>();
        foreach (string part in spec.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = part.Split('=', 2);
            string key = kv.Length == 2 ? KeyOf(kv[0].Trim(), "") : "";
            if (kv.Length != 2 || key.Length == 0
                || !double.TryParse(kv[1].Trim(), System.Globalization.NumberStyles.Float,
                                    System.Globalization.CultureInfo.InvariantCulture, out double v)
                || v <= 0.0)
            {
                ignored.Add(part.Trim());
                continue;
            }
            table[key] = v;
            applied.Add($"{key}={v:F0} m");
        }
        note = (applied.Count > 0 ? "overrides applied: " + string.Join(", ", applied) : "")
             + (ignored.Count > 0
                ? (applied.Count > 0 ? "; " : "") + "IGNORED (not an echelon the table covers, or not a "
                  + "positive number): [" + string.Join(", ", ignored) + "]"
                : "");
        return table;
    }
}
