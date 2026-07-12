namespace VrfC2SimApp;

/// <summary>
/// Offline check of VerbMapping (Layer 1 of the semantic map; no bridge, no MAK, no
/// VR-Forces): `VrfC2SimApp --verb-selftest`. Asserts the grounded verb -> intent table
/// (docs/SEMANTIC_MAPPING.md sec 3) and the fallback/edge behavior.
/// </summary>
public static class VerbMappingSelfTest
{
    public static int Run()
    {
        int failures = 0;

        // Grounded verb -> intent rows (SEMANTIC_MAPPING.md sec 3), from the real orders.
        CheckIntent(ref failures, "MOVE",   TaskIntent.Move);
        CheckIntent(ref failures, "BREACH", TaskIntent.Breach);
        CheckIntent(ref failures, "ATTACK", TaskIntent.Attack);
        CheckIntent(ref failures, "DESTRY", TaskIntent.Attack);
        CheckIntent(ref failures, "FIX",    TaskIntent.Attack);
        CheckIntent(ref failures, "DISRPT", TaskIntent.Attack);
        CheckIntent(ref failures, "PENTRT", TaskIntent.Attack);
        CheckIntent(ref failures, "SECURE", TaskIntent.HoldObjective);
        CheckIntent(ref failures, "OCCUPY", TaskIntent.HoldObjective);
        CheckIntent(ref failures, "SEIZE",  TaskIntent.HoldObjective);
        CheckIntent(ref failures, "RETAIN", TaskIntent.HoldObjective);
        CheckIntent(ref failures, "BLOCK",  TaskIntent.HoldObjective);
        CheckIntent(ref failures, "DEFEND", TaskIntent.HoldObjective);
        CheckIntent(ref failures, "GUARD",  TaskIntent.HoldObjective);
        CheckIntent(ref failures, "SCREEN", TaskIntent.Reconnoiter);
        CheckIntent(ref failures, "SCOUT",  TaskIntent.Reconnoiter);
        CheckIntent(ref failures, "ESCRT",  TaskIntent.Escort);
        CheckIntent(ref failures, "CLRLND", TaskIntent.Clear);

        // Layer-2 wiring status. Move/Attack/Breach/Reconnoiter/Escort are wired to real vrftasks;
        // HoldObjective (DtHoldUntilTask + scan) and Clear (composite) stay bare-move fallbacks.
        Check(ref failures, VerbMapping.Classify("MOVE").Implemented, "MOVE is implemented (bare move)");
        Check(ref failures, VerbMapping.Classify("ATTACK").Implemented, "ATTACK is implemented (fires, unit 3)");
        Check(ref failures, VerbMapping.Classify("DESTRY").Implemented, "DESTRY is implemented (fires)");
        Check(ref failures, VerbMapping.Classify("BREACH").Implemented, "BREACH is implemented (unit 2, DtBreachTask)");
        Check(ref failures, VerbMapping.Classify("SCREEN").Implemented, "SCREEN is implemented (Reconnoiter, patrol)");
        Check(ref failures, VerbMapping.Classify("ESCRT").Implemented, "ESCRT is implemented (Escort, follow)");
        Check(ref failures, !VerbMapping.Classify("SECURE").Implemented, "SECURE not yet wired (HoldObjective bare-move fallback)");
        Check(ref failures, !VerbMapping.Classify("CLRLND").Implemented, "CLRLND not yet wired (Clear bare-move fallback)");

        // Recognized flag: every real verb is in the table (recognized); an unlisted one is not.
        Check(ref failures, VerbMapping.Classify("ATTACK").Recognized, "ATTACK is recognized (in table)");
        Check(ref failures, !VerbMapping.Classify("NOTAVERB").Recognized, "unlisted verb is NOT recognized");

        // Fallback: an unlisted verb classifies as Move and is treated as implemented.
        {
            var p = VerbMapping.Classify("NOTAVERB");
            Check(ref failures, p.Intent == TaskIntent.Move && p.Implemented && !p.Recognized,
                  "unlisted verb falls back to bare Move (unrecognized)");
        }

        // Edge: null / empty / whitespace -> Move fallback (no throw).
        Check(ref failures, VerbMapping.Classify(null).Intent == TaskIntent.Move, "null verb -> Move (no throw)");
        Check(ref failures, VerbMapping.Classify("").Intent == TaskIntent.Move, "empty verb -> Move");
        Check(ref failures, VerbMapping.Classify("   ").Intent == TaskIntent.Move, "whitespace verb -> Move");

        // Case/whitespace insensitivity: the parser hands us the schema enum's ToString(), but
        // classify defensively normalizes.
        Check(ref failures, VerbMapping.Classify("breach").Intent == TaskIntent.Breach, "lowercase 'breach' -> Breach");
        Check(ref failures, VerbMapping.Classify(" Attack ").Intent == TaskIntent.Attack, "padded ' Attack ' -> Attack");

        // Every classification must carry a non-empty composition string (for the log line).
        Check(ref failures, !string.IsNullOrEmpty(VerbMapping.Classify("ATTACK").Composition),
              "classified verb carries a composition description");

        Console.WriteLine(failures == 0 ? "ALL CHECKS PASSED" : $"{failures} CHECK(S) FAILED");
        return failures == 0 ? 0 : 1;
    }

    private static void CheckIntent(ref int failures, string verb, TaskIntent expected)
    {
        var actual = VerbMapping.Classify(verb).Intent;
        Check(ref failures, actual == expected, $"{verb} -> {expected}" + (actual == expected ? "" : $" (got {actual})"));
    }

    private static void Check(ref int failures, bool ok, string label)
    {
        Console.WriteLine($"  [{(ok ? "PASS" : "FAIL")}] {label}");
        if (!ok) failures++;
    }
}
