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

        // ---- The two verbs the real STP export added (2026-09-20) -----------------------------
        // Both are valid TaskActionCodeType members and both used to classify as UNRECOGNISED, so
        // five of Iron Storm's 23 tasks ran as bare movement behind a coverage-gap warning. The
        // schema annotates no enumeration member at all, so each row is grounded in the project's
        // record - the citations are on the table rows in VerbMapping.cs.
        //
        // ExecutePlanPhase is a PLAN-PHASE MARKER (the schema's only sentence about it is on
        // OnOrderTriggerType, xsd:4388-4396; docs/STP_TASK_VOCABULARY_2026-09-03.md:39 calls it a
        // "phase marker"). A marker names no movement, so it must NOT manufacture one.
        CheckIntent(ref failures, "ExecutePlanPhase", TaskIntent.HoldInPlace);
        CheckIntent(ref failures, "EXECUTEPLANPHASE", TaskIntent.HoldInPlace);
        Check(ref failures, VerbMapping.Classify("ExecutePlanPhase").Recognized,
              "ExecutePlanPhase is RECOGNISED - no more 'add it to VerbMapping' coverage warning");
        Check(ref failures, VerbMapping.Classify("ExecutePlanPhase").Implemented,
              "ExecutePlanPhase is IMPLEMENTED: the in-place dispatch IS its composition, not a fallback");
        Check(ref failures, VerbMapping.Classify("ExecutePlanPhase").Intent != TaskIntent.Move,
              "ExecutePlanPhase is NOT mapped to Move - a marker with a route is still a marker, and " +
              "driving it would be the fake move the vocabulary work exists to stop");

        // CRESRV = constitute reserve (STP_TASK_VOCABULARY_2026-09-03.md:37): a hold-family posture
        // verb with NO vendor task (":81-83", TASK_VOCABULARY_ASSESSMENT:692-694), which is exactly
        // what HoldObjective already records as Implemented=false. Recognising it is the change:
        // "unrecognised" says nobody has looked at this verb, and somebody now has.
        CheckIntent(ref failures, "CRESRV", TaskIntent.HoldObjective);
        Check(ref failures, VerbMapping.Classify("CRESRV").Recognized,
              "CRESRV is RECOGNISED (a ruled verb, not an unknown one)");
        Check(ref failures, !VerbMapping.Classify("CRESRV").Implemented,
              "CRESRV stays UNWIRED - HoldObjective's Layer-2 gap is real and stays loud; no vendor " +
              "task is invented for it");

        // AND NOTHING ELSE MOVED. The two rows must not have changed any verb that was already
        // ruled on - in particular no fire/attack behaviour was wired for anything new.
        Check(ref failures, VerbMapping.Classify("ExecutePlanPhase").Intent != TaskIntent.Attack
                            && VerbMapping.Classify("CRESRV").Intent != TaskIntent.Attack
                            && VerbMapping.Classify("ExecutePlanPhase").Intent != TaskIntent.Breach
                            && VerbMapping.Classify("CRESRV").Intent != TaskIntent.Breach,
              "neither new verb touches the fire/attack or breach families");
        Check(ref failures, !VerbMapping.Classify("HoldInPlace").Recognized
                            && !VerbMapping.Classify("CNFPSL").Recognized,
              "verbs nobody has ruled on are still UNRECOGNISED (CNFPSL, STP's own passage-of-lines " +
              "code, is the one an export that MEANS 'move' should send)");

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
