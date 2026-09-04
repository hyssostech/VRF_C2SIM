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

A probe is registered only AFTER the relevant docs are read and CITED. Two failed fix attempts
on the same symptom = stop and research before the third.

## 2. READ THE MIGRATION DIFF BEFORE ANY 5.2 BEHAVIOUR WORK

`docs/VRF_5.2_MIGRATION_DIFF.md` maps 5.0.2 -> 5.2d row by row, cited, with the effect on this
federate and whether a decision is owed. Real effort went into it. **It is not a porting
checklist - it records SHIFTS IN APPROACH that code porting will not surface.**

WHY THIS PARAGRAPH EXISTS: on 2026-09-04 a session spent a day re-deriving VR-Forces altitude
handling from a stale code comment, got it wrong, and had to be corrected by the user twice.
Row **C8** of that diff already said it, cited to Release Notes p64, two days earlier, naming
the exact source file. Nobody opened it. Do not be the next one.

Live rows that change APPROACH, not just symbols - read them before touching those areas:
- **D1 (Y-10)**: MAK's own words - "remote-control applications that issue movement tasks to
  ground vehicles may need to be updated". That is OUR MoveToLocation/PlanAndMoveTo path.
- **D2 (Y-11)**: Unit Move To is silently redirected to Maneuver To; sub-route mechanism changed.
- **D7 (Y-13)**: Navigation Preferences now decide road behaviour.
- **A2/A9 (Y-2)**: federation identity lives in MAK-ONE-2025-Config.xml; FOM modules are ADDITIVE.
- **C5 (Y-9)**: blockOnAsynchronousOperations - determinism knob for fixed-frame runs.

## 3. SETTLED - DO NOT RE-DERIVE

- **Altitude**: `docs/VRF_ALTITUDE_FRAMES.md` is canonical and header-cited. An ENTITY takes an
  AGL flag directly (`setAltitude(uuid, m, aboveGroundLevel)`) and needs no terrain query; a
  ROUTE VERTEX has no AGL frame in the API and does need one. The 10000 m MSL birth is the wrong
  frame and is retiring. "Born buried therefore never moves" is FALSIFIED.
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
