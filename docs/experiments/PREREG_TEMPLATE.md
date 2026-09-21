# PREREG TEMPLATE - copy to docs\experiments\PREREG_<TOPIC>_<YYYY-MM-DD>.md

Fill every marker line. The offline suite lints every PREREG_*.md whose FILE-NAME date is after
2026-09-21 (check 13d of tests\RunnerTurnaround.Tests.ps1). This file is that check's CLEAN
control: the lint runs against this text on every suite run, so the template and the rule it
encodes cannot drift apart. Editing one without the other turns the suite red.

Delete the parenthetical guidance as you fill it in. Keep the marker spellings exactly.

## Registration

PREREG ID: <topic>-<YYYY-MM-DD>-<n>
DATE (UTC): <YYYY-MM-DDTHH:MMZ, written BEFORE the run>
BINARY / COMMIT: <hash of the build under test>
TIER AND GATE: <LIGHT|STANDARD|HEAVY> / <PLAN|PREREG|SPEND|RULE|->

VENDOR CITATION: <UG52 section + page, a help-page path under C:\MAK\vrforces5.2d\doc\help\Content,
a header + line, or a docs.mak.com URL - what the vendor says is supposed to happen>

OWN-RECORD CITATION: <the diff row, decision-evidence entry, ruling id, corrections-log entry or
earlier prereg that says what this project already established>

RUN KIND: movement
(one of movement | non-movement | offline - movement means anything whose success depends on a
platform or unit changing position)

## Conditions

CONSOLE LEVEL: 4
(the object's own console at notify level 4 is the first instrument for any behaviour question;
level 3 cannot tell a mover from a non-mover on a lone platform)

PRE-ORDER GATE: --pre-order-gate nav-area
(gate PushOrder on the first "New Primary nav area" row; warm the area first)

DurationScale: 1.0

ARMED ENDS VS STALL WINDOW: <every armed end in sim seconds against the 360 s stall window; a
compressed scale that puts an armed end inside the window manufactures a symptom. Required
whenever DurationScale is anything other than 1.0>

DEVIATION FROM RECORD: <omit this line unless you are departing from a standing rule. When you
keep it, quote the rule you depart from in double quotes and say why in one sentence. A line with
no quoted text does not count.>

## Predictions (written BEFORE the run; a missed HIGH prediction is a stop, not an adjustment)

| # | Prediction | Confidence | What counts as a MISS | Measured |
|---|---|---|---|---|
| P1 | <expected observable> | HIGH | <the observation that falsifies it> | <fill in after> |
| P2 | <expected observable> | MEDIUM | <the observation that falsifies it> | <fill in after> |

ONE VARIABLE: <name the single thing that differs from the control run, and name the control run>

## Result (written after the harvest, never from a live read)

<verdict per prediction, then the measurement and its design implication in SEPARATE sentences>
