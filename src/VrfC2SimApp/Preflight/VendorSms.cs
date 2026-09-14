using System.Globalization;
using System.Text.RegularExpressions;

namespace VrfC2SimApp.Preflight;

/// <summary>
/// An index of the vendor .entity files: objectType -> label / max-slope / subordinates,
/// with parentFile inheritance (M3A2_Bradley_CFV, for one, inherits M2A2_Bradley_IFV).
/// I/O once, in the constructor; resolution afterwards is pure table walking.
///
/// This is how a UNIT's limit is RESOLVED rather than tabulated: a template is expanded to
/// the leaves of its subordinate tree and the unit's limit is the MINIMUM max-slope over
/// them (Tank Headquarters Section (USA) -> 2x M1A2 0.94, M3A2 CFV 0.94, 2x HMMWV 1.0,
/// M577A2 1.0 -> 0.94). Ported from tools/preflight/leg_check.py's VendorSms.
/// </summary>
public sealed class VendorSms
{
    private sealed class Rec
    {
        public string ObjectType = "";
        public string MatchType;
        public string Parent;
        public string File = "";
        public string Label = "";
        public double? MaxSlope;
        public List<string> Subs = new();
    }

    private static readonly Regex RxObj = new("<simObject\\s+objectType=\"([^\"]+)\"", RegexOptions.Compiled);
    private static readonly Regex RxMatch = new("matchType=\"([^\"]+)\"", RegexOptions.Compiled);
    private static readonly Regex RxParent = new("parentFile=\"([^\"]+)\"", RegexOptions.Compiled);
    private static readonly Regex RxLabel = new("<string paramName=\"gui-label\">([^<]*)</string>", RegexOptions.Compiled);
    private static readonly Regex RxSlope = new("<real paramName=\"max-slope\">([^<]+)</real>", RegexOptions.Compiled);
    private static readonly Regex RxSub = new("<subordinate\\s+objectType=\"([^\"]+)\"", RegexOptions.Compiled);

    private readonly Dictionary<string, Rec> _byType = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Rec> _byFile = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Rec> _byLabel = new(StringComparer.Ordinal);

    /// <summary>The fallback limit used when the vendor SMS is unreachable, and said so in output.</summary>
    public const double MaxSlopeFallbackMin = 0.94;

    /// <summary>No vendor ground VEHICLE exceeds max-slope 1.0; every DI-Guy lifeform is 1.5 or 1.57.</summary>
    public const double LifeformSlope = 1.2;

    public string Directory { get; }
    public bool Ok { get; }
    public int FileCount => _byFile.Count;

    public VendorSms(string smsDir)
    {
        Directory = smsDir;
        Ok = System.IO.Directory.Exists(smsDir);
        if (!Ok) return;

        // Enumeration order is left as the file system gives it, which is what python's glob
        // sees too - it decides only which record wins a duplicate label, and the vendor set
        // has none that matter here.
        foreach (var fn in System.IO.Directory.EnumerateFiles(smsDir, "*.entity"))
        {
            string txt;
            try { txt = File.ReadAllText(fn, System.Text.Encoding.UTF8); }
            catch { continue; }
            var mo = RxObj.Match(txt);
            if (!mo.Success) continue;
            int gt = txt.IndexOf('>', mo.Index);
            string head = gt > mo.Index ? txt.Substring(mo.Index, gt - mo.Index) : txt.Substring(mo.Index);
            var lbl = RxLabel.Match(txt);
            var slope = RxSlope.Match(txt);
            var mt = RxMatch.Match(head);
            var pf = RxParent.Match(head);
            string stem = Path.GetFileNameWithoutExtension(fn);
            var rec = new Rec
            {
                ObjectType = mo.Groups[1].Value,
                MatchType = mt.Success ? mt.Groups[1].Value : null,
                Parent = pf.Success ? pf.Groups[1].Value : null,
                File = stem,
                Label = lbl.Success ? lbl.Groups[1].Value : stem,
                MaxSlope = slope.Success && double.TryParse(slope.Groups[1].Value, NumberStyles.Float,
                                                            CultureInfo.InvariantCulture, out var sv)
                         ? sv : (double?)null,
                Subs = RxSub.Matches(txt).Select(m => m.Groups[1].Value).ToList(),
            };
            _byType.TryAdd(rec.ObjectType, rec);
            _byFile.TryAdd(stem, rec);
            _byLabel.TryAdd(rec.Label, rec);
            _byLabel.TryAdd(stem, rec);
        }
    }

    private Rec Find(string nameOrType)
    {
        if (string.IsNullOrEmpty(nameOrType)) return null;
        if (_byLabel.TryGetValue(nameOrType, out var r)) return r;
        if (_byFile.TryGetValue(nameOrType, out r)) return r;
        string t = nameOrType;
        if (t.Count(c => c == ':') == 7)                 // 8-field VRF type: strip the superType
            t = t.Substring(t.IndexOf(':') + 1);
        if (_byType.TryGetValue(t, out r)) return r;
        foreach (var rec in _byType.Values)              // matchType wildcards (-1 fields)
        {
            if (rec.MatchType == null) continue;
            var a = rec.MatchType.Split(':');
            var b = t.Split(':');
            if (a.Length != b.Length) continue;
            bool all = true;
            for (int i = 0; i < a.Length && all; i++)
                if (a[i] != "-1" && a[i] != b[i]) all = false;
            if (all) return rec;
        }
        return null;
    }

    private Rec ParentOf(Rec rec)
    {
        string p = rec?.Parent;
        if (string.IsNullOrEmpty(p)) return null;
        string stem = p.EndsWith(".entity", StringComparison.OrdinalIgnoreCase)
                    ? p.Substring(0, p.Length - ".entity".Length) : p;
        return _byFile.TryGetValue(stem, out var r) ? r : null;
    }

    private double? InheritedSlope(Rec rec)
    {
        for (int d = 0; rec != null && d <= 8; d++, rec = ParentOf(rec))
            if (rec.MaxSlope.HasValue) return rec.MaxSlope;
        return null;
    }

    private List<string> InheritedSubs(Rec rec)
    {
        for (int d = 0; rec != null && d <= 8; d++, rec = ParentOf(rec))
            if (rec.Subs is { Count: > 0 }) return rec.Subs;
        return new List<string>();
    }

    /// <summary>The leaves of a template's subordinate tree as (label, max-slope-or-null).</summary>
    public List<(string Label, double? MaxSlope)> Vehicles(string template)
        => Vehicles(template, 0, new HashSet<string>(StringComparer.Ordinal));

    private List<(string, double?)> Vehicles(string template, int depth, HashSet<string> seen)
    {
        var rec = Find(template);
        if (rec == null) return new List<(string, double?)> { ($"UNRESOLVED:{template}", null) };
        if (seen.Contains(rec.ObjectType) || depth > 6) return new List<(string, double?)>();
        var seen2 = new HashSet<string>(seen, StringComparer.Ordinal) { rec.ObjectType };
        var subs = InheritedSubs(rec);
        if (subs.Count == 0)
            return new List<(string, double?)> { (rec.Label, InheritedSlope(rec)) };
        var outp = new List<(string, double?)>();
        foreach (var s in subs) outp.AddRange(Vehicles(s, depth + 1, seen2));
        return outp;
    }

    /// <summary>
    /// The unit's own limit on level ground: the MINIMUM max-slope over the leaves. Null (and
    /// the caller's documented fallback) when nothing in the tree carries one.
    /// </summary>
    public (double? Limit, List<(string Label, double? MaxSlope)> Vehicles) MinMaxSlope(string template)
    {
        var veh = Vehicles(template);
        var slopes = veh.Where(v => v.MaxSlope.HasValue).Select(v => v.MaxSlope.Value).ToList();
        return (slopes.Count == 0 ? (double?)null : slopes.Min(), veh);
    }
}
