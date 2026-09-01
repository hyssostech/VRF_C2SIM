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

## NOTE 2 - RESOLVED BY P2c (2026-09-01 evening): NO DEFECT REPORT.
Run 20260901T211310Z: the STOCK "Tank Headquarters Section (USA)" template works
end-to-end under correctly-authored (above-terrain) route vertices; the invalid-
formation warning fired once and the documented working-formation fallback covered it.
Fold ONE optional observation into Note 1:

3. Minor observation, no action needed: creating "Tank Company (USA)" (EntityLevel)
   logs `Aggregate state has invalid formation name "column-left"` for its HQ section
   (the company .frm files reference sub-formation names the HQ-section template does
   not define); the working-formation fallback covers it and the company operates
   normally. Mentioned only because at raised console verbosity it appears on every
   company creation.

Send order: Note 1 now if desired; Note 2 variant per P2c; license renewal is handled
separately (in process).
