# DRAFT notes to MAK support (2026-09-01) - FOR USER REVIEW, NOTHING SENT

Posture (user ruling 2026-09-01): VR-Forces is a mature, widely-deployed product; where
our results look like "vendor bugs" the stronger prior is that WE are off the beaten
path. Both notes below are written from that posture. Note 2 has two variants; run P2c
(in flight) selects one. The user sends these, not the tooling.

---

## NOTE 1 - documentation access question (send any time; independent of P2c)

Subject: VR-Forces 5.x Developer's Guide - aggregate/organization chapters

We are integrating an external C2 system with VR-Forces 5.0.2 via the Remote Control
API and are working from the public Developer's Guide at
docs.mak.com/api/vrforces5.2/classref/ (also 5.1.1). Two questions:

1. The 4.10 guide's chapters "The Aggregate Entity Behavior Model", "The Organization
   Manager", "Echelon IDs", "Object Console Messages", "Ground Disaggregated Movement
   System" do not appear in the 5.x guides; their titles remain in the 5.2 keyword
   index with empty links. Is there a 5.x home for this material, or should we treat
   the 4.10 text as authoritative for 5.0.2?
2. Is there recommended reading (or training material) for driving ENTITY-LEVEL units
   of company echelon and above purely through the Remote Control API (createAggregate
   with createSubordinates + move-along tasking)? We want to confirm we are on the
   intended path rather than an exotic one.

---

## NOTE 2, VARIANT A - if P2c shows the STOCK template works (beaten-path confirmed)

(Then there is NO defect report. At most, fold one observation into Note 1:)

3. Minor observation, no action needed: creating "Tank Company (USA)" (EntityLevel)
   logs `Aggregate state has invalid formation name "column-left"` for its HQ section
   at every creation - the company formation files reference sub-formation names the
   HQ-section template does not define, and the working-formation fallback then covers
   it. If that warning is expected, fine; we mention it only because at raised console
   verbosity it appears on every company creation.

## NOTE 2, VARIANT B - if P2c shows the aliases are LOAD-BEARING

Subject: Question on entity-level company move-along - formation-name resolution for
sub-units

We suspect we are off the documented path and would like guidance. Setup: VR-Forces
5.0.2, EntityLevel model set, "Tank Company (USA)" created via the Remote Control API
(createAggregate, createSubordinates=true, disaggregated), tasked with a bare
move-along on a route authored above terrain. Observed: the company's HU controller
creates its per-sub-unit working routes but sends none; creation logs
`AR HQ Sec 1: Aggregate state has invalid formation name "column-left"` (the company
.frm files assign the HQ slot "Column-Left"/"Line-Left", which "Tank Headquarters
Section (USA).entity" does not define). Adding alias formation entries to the
HQ-section template resolves it end-to-end (evidence available: run logs + traces,
before/after).

Questions: (a) is per-sub-unit formation-name resolution expected to be strict on this
path, or should the working-formation fallback have covered it? (b) is the intended
usage a different one (e.g. aggregate-level scenarios for company+ echelons, MSDL
import, or pre-authored scenarios) that avoids this combination entirely? (c) if the
template/formation mismatch is unintended, we can share the exact files and a minimal
reproduction.

---

Send order: Note 1 now if desired; Note 2 variant per P2c; license renewal is handled
separately (in process).
