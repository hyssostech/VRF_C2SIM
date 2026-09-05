# VRF_C2SIM - read this before you touch anything

This file is loaded into every session in this repo. It is short on purpose. It exists because
sessions keep re-deriving, from probes and from stale code comments, things the vendor already
documents and this repo already recorded - twice costing more than a day each time.

## 1. READ THE VENDOR'S DOCUMENTATION. THIS IS THE STANDING RULE, NOT ADVICE.

**You cannot develop against VR-Forces from what you already know. It is not in the model.**
Consult the vendor docs AS YOU DESIGN, not after a probe fails.

Where they are (all local, all searchable, no excuse):
- `C:\MAK\vrforces5.2d\doc\help\Content\**` - the HTML Users Guide. **grep this first**; it is
  the fastest and it is the "how is this meant to be done" layer.
- `C:\MAK\vrforces5.2d\doc\*.pdf` - Users Guide, Migration Guide, Release Notes,
  Interoperability Guide, Model Catalog, First Experience.
- `C:\MAK\vrforces5.2d\doc\classdoc\classref\**` - API class reference.
- `C:\MAK\vrforces5.2d\include\**` - the HEADERS. A signature settles an API question outright;
  default arguments carry real behaviour (see the AGL case below).
- Online: docs.mak.com/support (every product PDF, every version) and
  docs.mak.com/api/vrforces5.2/classref.

### NO PROBE UNTIL YOU CAN CITE THE DOCS IT RESTS ON - BOTH KINDS

**Probing is not a remedy. You will never learn how VR-Forces works by random walks.** A probe
that is not derived from a document is a guess with a run attached, and this project has paid
for that repeatedly.

Before ANY experiment, prereg, or "let's just try it", write down BOTH:
1. the VENDOR citation - file + section/page, or a header + line - saying what is supposed to
   happen; and
2. the OWN-RECORD citation - the diff row, decision-evidence ruling, prereg or corrections-log
   entry that says what we already established.

If you cannot produce both, you are not ready to run anything; go read. Two failed attempts on
the same symptom = stop and research before the third. State the citations IN the prereg; a
prereg without them is not registered.

**Check the RULING before proposing work.** `docs/VRF_5.2_DECISION_EVIDENCE.md` is the ruling
ledger and it BEATS the effect-cell prose in the diff, which can lag. On 2026-09-04 a session
proposed a HEAVY probe of the ground-movement path that had already been RETIRED by ruling Y-10
two days earlier - "no consumer, no class": `DtMoveToLocationTask` is deleted in 5.2, so the
probe had nothing to probe.

## 2. READ THE MIGRATION DIFF BEFORE ANY 5.2 BEHAVIOUR WORK

`docs/VRF_5.2_MIGRATION_DIFF.md` maps 5.0.2 -> 5.2d row by row, cited, with the effect on this
federate and whether a decision is owed. Real effort went into it. **It is not a porting
checklist - it records SHIFTS IN APPROACH that code porting will not surface.**

WHY THIS PARAGRAPH EXISTS: on 2026-09-04 a session spent a day re-deriving VR-Forces altitude
handling from a stale code comment, got it wrong, and had to be corrected by the user twice.
Row **C8** of that diff already said it, cited to Release Notes p64, two days earlier, naming
the exact source file. Nobody opened it. Do not be the next one.

Read the row AND its ruling in `docs/VRF_5.2_DECISION_EVIDENCE.md` (the ruling wins):
- **D1 / Y-10 - RULED, no probe owed.** MAK flags that remote-control apps issuing ground
  movement "may need to be updated", but the ruling settles it: KEEP MoveAlongRoute.
  `moveAlongRoute` is byte-identical and undeprecated; `DtMoveToLocationTask` is DELETED.
  Vendor: Move To plans a route then follows it; Move Along Route "does not use road movement,
  nor does the entity plan paths before moving" - so an AUTHORED C2SIM route MUST use
  MoveAlongRoute, or the planner overrides the route we were asked to drive.
- **D2 / Y-11**: unit move-along is repackaged as maneuver-along; per-subordinate offset routes
  are computed at task time; units wait for a valid formation before moving (RN VRF-8977 is a
  competing hypothesis for the 5.0.2 company non-determinism).
- **D7 / Y-13 - THE DEFAULT MOVEMENT FLIPPED. Armour now IGNORES ROADS.** 5.0.2
  `ground-tracked.sysdef:71` hard-coded `(default-preference "Prefer Roads")`; 5.2d :132 is
  `(default-preference $road-preference (default "Ignore Roads"))` and
  `M1A2_Abrams_MBT.entity:218` pins "Ignore Roads" (MG 2.4 p18). Same file: **near-distance
  25 -> 15 m** on every movement controller (wait-at-waypoint 35 -> 15), ALL per-soil
  max-speed-factors removed, pathfinder weights populated, obstruction-sensor and
  collision-avoidance controllers REMOVED, maneuver-in-formation + react-to-collision-event
  actuator ADDED. Ruling: rely on SMS defaults, send no per-entity preference unless a C2SIM
  order carries road-use intent.
- **D6 / Y-12 - AUTONOMY IS NOW THE DEFAULT.** `AIEnabled` -> `AutonomousActionsEnabled`, and
  it now gates PATH PLANNING as well as firing/collision (UG52 23.6 p508: disabled = no
  planning, straight line through obstacles). Planning and dynamic obstacle avoidance are ON.
  Ruling: leave them on - disabling to get repeatable traces produces traces of a mode the
  vendor calls dumb driving. Repeatability comes from Y-9, not from crippling the sim.

**CONSEQUENCE FOR EVERY 5.2 BEHAVIOURAL RUN - do not learn this the hard way:** the 5.0.2
golden traces were produced by road-preferring armour with a 25 m arrival threshold and no
dynamic avoidance. On 5.2 the same order drives OFF-ROAD, arrives within 15 m, may back up and
replan ONCE when blocked, and waits for a valid formation before moving. **5.0.2 positions and
timings are NOT a valid comparison basis on 5.2.** Assert STRUCTURE (creation lines naming the
mapped templates, routes addressed by uuid, movement occurring, TASKCMPLT pairing) and derive
any tolerance from 5.2 behaviour. A 5.2 path that differs from the 5.0.2 golden is very likely
CORRECT.
- **A2/A9 (Y-2)**: federation identity lives in MAK-ONE-2025-Config.xml; FOM modules are ADDITIVE.
- **C5 (Y-9)**: blockOnAsynchronousOperations - determinism knob for fixed-frame runs.
- **C8 (Y-17)**: altitude - see sec 3.

## 3. SETTLED - DO NOT RE-DERIVE

- **Altitude**: `docs/VRF_ALTITUDE_FRAMES.md` is canonical and header-cited - read sec 0 first.
  The root was a SOURCE frame error: C2SIM has `AltitudeAGL` AND `AltitudeMSL`, both optional
  (xsd :2716-2717); every init we have carries NEITHER; the oracle read either into one field
  and used it as absolute. FIXED 2026-09-05 (4b4d0f9, `PlacementPolicy.cs`): create at the
  authored lat/lon, place with `setAltitude(agl, aboveGroundLevel=TRUE)`; land + nothing given =
  on the ground (AGL 0); "ground" = DIS domain of the created type, never an APP6 character. The
  10000 m birth, the oracle's +1 and the SIDC 'G' test are GONE - do not reintroduce any of them.
  A ROUTE VERTEX has no AGL frame in the API and does need the terrain query (TerrainProfile).
  "Born buried therefore never moves" is FALSIFIED. Live confirmation of the rewrite is still owed.
- `docs/HANDOFF_2026-09-01_R9_COMPLETE.md` opens with a **CLOSED - DO NOT REOPEN** list. Each
  line names its record and its reopening evidence. Read it before proposing a cause.
- `docs/CORRECTIONS_LOG.md` holds refuted claims. If a code comment and this log disagree, the
  log wins and the comment is a bug - fix it where you found it.

## 4. HOW CLAIMS GET WRITTEN DOWN (this is where the rot comes from)

**Keep the measurement and its design implication in SEPARATE SENTENCES, and never put a design
verdict inside a falsifier clause.** A falsifier says what the world will look like; it does not
say what should then be built. Both major regressions in this project have the same shape: a
true narrow finding recorded FUSED to a design conclusion, after which the fusion is what the
next reader inherits - once seven weeks later, once the same session an hour later.

Corollary: a docs finding that KILLS a hypothesis must also be checked for what it ENABLES.
In July a session correctly found the AGL parameter, used it only to exonerate a suspect, then
shipped the workaround that parameter made unnecessary.

When you refute something, write the refutation AT EVERY SITE that repeats the claim - not only
in the corrections log. A reader of the stale site never learns it is dead.

## 5. HOUSE RULES

- ASCII-only and CRLF in tracked files. Check with
  `rg -P "[^\x09\x0a\x0d\x20-\x7E]" <file>`, gated on a known-dirty control first.
- Fresh ledgered appNumber per federate join; the ONE authoritative marker is
  `*** NEXT FREE: <n> ***` in `docs/OPUS_EXECUTION_PLAN.md` Appendix B. Never reuse.
- Never kill rtiexec / rtiForwarder / rtiAssistant. Never write under `C:\MAK` except the
  sanctioned fixture deploy. Never edit the frozen C++ oracle repo `c2simVRFinterfacev2.36`.
- Vendor sim logs dump the whole environment in cleartext - never attach one anywhere; send the
  `.callstack.log` / `.dmp`.
- Live doc caps: HANDOFF and the 5.2 DIFF are 200 lines. Adding means collapsing something else.
