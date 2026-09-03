# What STP can say to us - the task vocabulary, from the STP source (2026-09-03)

Source of truth: STP repo checkout `Source\Repos\STP\STP-5.11` (branch release/5.11).
Files read: `SDK\StpSDK\Data\StpTaskFactory.cs` (task definition loader),
`SharedResources\ActivitySignatures\AS_Table_compact.txt` (the 266 task definitions),
`NallSuite\Agents\C2SimBridge\C2SimTask.cs` :126-672 (STP task -> C2SIM TaskActionCode),
`C2SimXmlBuilder.cs` :420-475 (what goes into each ManeuverWarfareTask),
`SharedResources\c2simbridge.json` (bridge defaults). All counts are from those files.

## 1. STP's task model is How x What x Who, not a verb

Each of the 266 definitions has a HOW (tactical method), a WHAT (tactical task/effect)
and a WHO (unit class), plus the tactical graphics (TGs) the task is drawn with.
- HOW (29 values): ATTACK 52, AREA_DEFENSE 29, ATTACK_BY_FIRE 19, COUNTERATTACK 16,
  SCREEN 10, ATTACK_IN_ZONE 8, SUPPORT_BY_FIRE 7, COUNTERATTACK_BY_FIRE 5, WITHDRAWAL 4,
  MOVING_SCREEN 3, PASSAGE_OF_LINES 2, GUARD/DEFEND/COVER/MOBILE_DEFENSE/ASSAULT/
  SEARCH_AND_ATTACK/CORDON_AND_SEARCH/AIR_ASSAULT/AIR_RECONNAISSANCE 1 each; 110
  NOT_SPECIFIED; the rest are stability/civil (INSURGENT, CIVILIAN, SFA, IO, CERP...).
- WHAT (60+ values): DESTROY 25, FIX 20, SECURE 14, OCCUPY 11, BLOCK 10, CLEAR 9, MOVE 8,
  DISRUPT 7, SEIZE 5, RETAIN 5, PATROL 5, DEFEAT 5, TURN 4, PENETRATE 4,
  FOLLOW_AND_SUPPORT 4, FOLLOW_AND_ASSUME 4, SUPPRESS 3, FOLLOW 3, BYPASS 3, BREACH 3,
  AMBUSH 3, CONTAIN 2, NEUTRALIZE 2, REINFORCE 2, ISOLATE, INTERDICT, OBSERVE, MOVE_TO,
  MOVE_ALONG, ... plus supply/psyop/civil.
- WHO: GroundManeuverUnitSymbol 113, ManeuverUnitSymbol 42, FriendlyManeuverUnitSymbol 28,
  CSS 17, RotaryWing 15, ArmedNonMil 14, ... (ground maneuver dominates).

## 2. What reaches us: the C2SIM code STP emits (C2SimTask.cs MWTaskCode)

Rule: WHAT decides first; if WHAT is NOT_SPECIFIED, HOW decides; otherwise ATTACK.
51 distinct codes are emittable. Ground-maneuver ones (our scope):
- Effects on enemy: ATTACK, ATTMN (main attack), ATTSPT (supporting), CTRATK, CTRFIR,
  ARMAS (assault), DESTRY, DEFEAT, FIX, DISRPT, PENTRT, SUPPRS, NTRCOM (neutralize),
  AMBUSH, HARASS, SERCH (cordon/search-and-attack), BYPASS, BREACH, CLRLND.
- Terrain/objective control: SECURE, OCCUPY, SEIZE, RETAIN, BLOCK, DEFEND (area defense
  and defend), GUARD, COVER, SCREEN (screen and moving screen), TURN, DELAY, WITHDR.
- Movement/relationship: MOVE, FOLSPT, FOLASS, ESCRT, CNFPSL (passage of lines), OBSRV,
  SCOUT (STP "patrol" is emitted as SCOUT, not PATROL), CRESRV (constitute reserve).
- Not ours: ARASLT, TCARRC (aviation); EVACT, REFUEL, RESUPL, REINF (CSS); PSYCHW,
  UNCONW, PRVSCY/PRVHLT/PRVCNS (stability); ExecutePlanPhase (phase marker).
Coverage today: our VerbMapping.cs covers 18 of the 51; ALL 18 are emittable (nothing
dead). Missing ground codes: ATTMN ATTSPT CTRATK CTRFIR ARMAS DEFEAT SUPPRS NTRCOM AMBUSH
HARASS SERCH BYPASS COVER TURN DELAY WITHDR FOLSPT FOLASS CNFPSL OBSRV CRESRV.

## 3. What else each task carries (C2SimXmlBuilder.cs :420-475) - decisive for VRF mapping

- PerformingEntity = the unit. AffectedEntity = THE SAME UNIT (:427-429), never the
  enemy. CHECKED against the two real orders on disk: 127 of 127 tasks have
  AffectedEntity == PerformingEntity, and 0 MapGraphicID elements (those exports ran
  with IncludeMapGraphicIdInTasks off or predate it). Target identity is NOT in
  AffectedEntity. Our Attack composition "fireAtTarget(affected)" therefore fires at
  nothing real by construction (the port's self-target guard was masking exactly this).
  For VRF exports the bridge must run with IncludeMapGraphicIdInTasks=True or the
  objective KIND (area vs line vs point) is lost and only its points survive.
- Geometry: Location list = the linearized ROUTE points (axis of advance / direction of
  attack) if the task has routes, else the first TG's points (objective area polygon,
  battle position, screen line). MapGraphicID = the TG GUIDs when
  IncludeMapGraphicIdInTasks (default True); the TGs themselves are placed in the
  Initialization when PlaceAllTgInInitialization (default True).
- DesiredEffectCode: DSTRYK for destroy, NUTRLD for defeat, else TaskSuccess.
- Timing: StartTime from phase sequencing; Duration = phases x PhaseDuration (10 min);
  ActionTemporalAssociation STREND chains tasks (observed 31x in COA-STP1).
- ROE: c2simbridge default "Hold" (COA-STP1 shows ROEHold on all 42 tasks).
- Main/supporting attack flags become ATTMN/ATTSPT, not ATTACK.

## 4. Consequences for the VRF side (feeds Y-15; no code changed)

1. The unit of mapping is (code, TG kind, route present?) - not the code alone. SEIZE
   with an objective polygon and an axis is "maneuver along axis, then occupy/clear the
   objective area"; the same code with only a polygon is "move to and hold".
2. Enemy targets come from the TG (objective area) + what the unit senses there, never
   from AffectedEntity. That is exactly how VR-Forces' own attack-to-objective tasks
   work (AggregateTacticalLevel unit-attack-to-objective: move to objective, engage what
   is there) - the vendor model and STP's semantics agree; the old bridge's
   "fire at affected entity" was the outlier.
3. The 5.2d scripted-task inventory has native homes for: attack-to-objective (ATTACK/
   ATTMN/ATTSPT/ARMAS/DESTRY/DEFEAT/SEIZE/CLRLND family), unit-defend (DEFEND/RETAIN/
   BLOCK/GUARD/COVER area-defense family), reconnoiter-route/location, perform-ground-
   reconnaissance (SCREEN/SCOUT/OBSRV), unit-ground-follow (FOLSPT/FOLASS/ESCRT),
   fire-support tasks (SUPPRS/NTRCOM/HARASS by fire), mount/dismount. Only in
   AggregateTacticalLevel for attack/defend/recon; EntityLevel has move/follow/fire only.
4. Not representable in any VRF task without authoring: BYPASS, TURN, DELAY, WITHDR
   (mobile-defense verbs), CNFPSL, AMBUSH, SERCH, CRESRV. These are Lua-authoring
   candidates or explicit "logged gap" verbs.
5. Route names (task.Routes[0].Name) are what the bridge names the task after (:513).
