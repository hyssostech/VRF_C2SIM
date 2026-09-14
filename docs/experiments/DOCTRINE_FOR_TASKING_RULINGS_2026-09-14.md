# Doctrine for the tasking rulings R1-R6 (Opus research executor, 2026-09-14 ~14:10Z; lane L8, Jira STP-797).
# SUPERVISOR CORRECTIONS (read before sec 4): (1) the executor's claim that Duration and EndTime are ABSENT from all
# 42 tasks is WRONG for Duration - data/COA-STP1_Order.xml carries a Duration on every task (32 x PT1H20M, 10 x PT2H;
# StartTime is a relative delay, 0 on 41 tasks and 3h20m on T13); EndTime is indeed absent. So the "time" half of R4
# is in the data: end = start + Duration. (2) USER RULINGS 2026-09-14 (supersede sec 7's open questions): R1 = the
# missing MapGraphicID is an STP EXPORT DEFECT (IncludeMapGraphicIdInTasks was off, the Location is the first graphic
# linearised) - fix on the STP side, not by name heuristics; R2 = a task without geometry uses the geometry of the
# performing (who) unit - remaining cases are STP issues; R3 = the target IS the objective in doctrinal terms, enemies
# may happen to be inside it (VR-Forces' tactical tasks take the objective graphic as the parameter, which matches);
# R4 = completion is given by the end time (start + Duration). R5/R6 remain the user's (engineering).
# Everything else in this record (per-verb table, the SIDC census - every init graphic carries an APP6C-SIDC and 16
# of them are FM 3-90 App. B task symbols - the STP builder cross-check, the verb-vs-symbol mismatch) stands.

# DOCTRINE FOR THE TASKING RULINGS (R1-R6)

Opus research executor, 2026-09-14. Read-only pass over US Army doctrine, MIL-STD-2525C,
the SISO C2SIM standards, the COA-STP1 data on disk, and the STP source tree.
Companion to `docs/experiments/TASK_VOCABULARY_ASSESSMENT_2026-09-14.md` secs 1, 4, 7.

Question put by the user: "Check doctrine to see what can be resolved that way" before
ruling R1-R6.

Answer in one line: **doctrine settles R2, R3 and R4 almost completely, and it settles
more of R1 than expected - because every graphic in `COA-STP1_Initialization.xml` carries
an `APP6C-SIDC`, and 16 of them ARE the FM 3-90 Appendix B tactical-mission-task symbols.**

---

## 0. THE FINDING THAT CHANGES THE RULINGS

This was not in the assessment and it is the single most useful thing in this pass.

Every `MapGraphic` in the init carries an `APP6C-SIDC` element (MIL-STD-2525C /
APP-6 symbol identification code) *before* its `Name`. Verified by parsing
`data/COA-STP1_Initialization.xml` this pass; 409 of 409 graphics carry one.
That means the DOCTRINAL IDENTITY of every graphic is already in our data. Census:

| kind | SIDC | 2525C hierarchy (verbatim from the standard) | doctrinal name | n | examples |
|------|------|----------------------------------------------|----------------|---|----------|
| TacticalArea | `GFGPOAO--------` | TACGRP.C2GM.OFF.ARS.OBJ | **Objective** | 15 | MADISON, MONROE, JEFFERSON, HAMILTON |
| TacticalArea | `GFGPGAA--------` | TACGRP.C2GM.GNL.ARS.ABYARA | **Assembly area** | 5 | BANDIT, BANDIT_II, BANDIT_III, IRON_FIST, EAGLES |
| TacticalArea | `GFGPDAB--------` | TACGRP.C2GM.DEF.ARS.BTLPSN | **Battle position** | 4 | BP_PL_BLUE, BP_PL_B_00/01/02 |
| TacticalArea | `GFGPSAT--------` | TACGRP.C2GM.SPL.ARA.TAI | **Targeted area of interest** | 3 | TAI_PAA_25, TAI_ABF_JU |
| TacticalArea | `GFGPOAK--------` | TACGRP.C2GM.OFF.ARS.ATKPSN | **Attack position** | 2 | NORMANS, SAXONS |
| TacticalArea | `GFFPAT---------` | TACGRP.FSUPP.ARS.ARATGT | **Area target** | 2 | AT_PAA_25E, AT_PAA_25F |
| TacticalArea | `GFMPOGB--------` | TACGRP.MOBSU.OBST.GNL | **Obstacle belt** | 2 | OBSENG_PL_, OBSENG_OEB |
| TacticalArea | `GFSPASD----I---` | TACGRP.CSS.ARA.SUPARS.DSA | **Division support area** | 1 | OILERS |
| TacticalArea | `GFGPAAR--------` | TACGRP.C2GM.AVN.ARS | aviation area | 1 | AAR_PAA_25 |
| Line | `GFGPGLP--------` | TACGRP.C2GM.GNL.LNE.PHELNE | **Phase line** | 10 | BRONZE, GOLD, BLUE, PURPLE, WHITE, YELLOW, ORANGE, PL_OBJ_MAD, PL_OBJ_MON, PL_PL_BLUE |
| Line | `GFGPOLAGS------` | TACGRP.C2GM.OFF.LNE.AXSADV.GRD.SUPATK | **Axis of advance, ground, supporting attack** | 9 | AOA_SE_1-1, AOA_SE_1-6, AOA_SE_40_, AOA_SE_C/1 |
| Line | `GFGPGLB----H/I/J/K---` | TACGRP.C2GM.GNL.LNE.BNDS | **Boundary** (echelon H=bde, I=div, J=corps, K=army) | 15 | 1AD, 1/AD, 25ID, I_CORPS, SIXTH_ARMY |
| Line | `GFSPLRM--------` | TACGRP.CSS.LNE.SLPRUT (main) | **Main supply route** | 1 | LRM_PL_YEL |
| Line | `GFSPLRW--------` | TACGRP.CSS.LNE.SLPRUT.2WTRFF | **Alternate supply route** | 1 | LRW_PL_GOL |
| Line | `GFSPLCM--------` | TACGRP.CSS.LNE (convoy) | **Moving convoy** | 1 | CNVY_PL_YE |
| Line | `GFMPBCL--------` | TACGRP.MOBSU.OBSTBP.CSGSTE.LANE | **Crossing-site lane** | 1 | BYPASS_PL_ |
| Line | `GFFPLTS--------` | TACGRP.FSUPP.LNE.LNRTGT.LSTGT | **Linear smoke target** | 1 | LTS_PAA_25 |
| Point | `GFGPGPRI-------` | TACGRP.C2GM.GNL.PNT.REFPNT.PNTINR | **Reference point** (label anchors) | 304 | every unit/graphic label |
| Point | `GFGPGPPD-------` | TACGRP.C2GM.GNL.PNT.ACTPNT.DCNPNT | **Decision point** | 5 | DP1..DP5 |
| Point | `GFGPOAF--------` | TACGRP.C2GM.OFF.ARS.AFP | **Attack by fire position** | 3 | __FRIEN_14/19/22 |
| Point | `GFMPOEB--------` | TACGRP.MOBSU.OBST.OBSEFT (block) | **Obstacle effect - block** | 2 | OEB_PL_BLU, OEB_OEB_PL |
| Point | `GFGPDPO--------` | TACGRP.C2GM.DEF.PNT.OBSPST | **Observation post** | 1 | DPO_PAA_25 |
| Point | `GFSPPSZ--------` | TACGRP.CSS.PNT.SPT | CSS support point | 2 | 1-6_IN_Com, 4-27_FA_Fi |

**And the 16 `TaskGraphic` elements are the FM 3-90 Appendix B tactical mission task
symbols themselves** (MIL-STD-2525C TACGRP.TSK.* branch):

| SIDC | 2525C | tactical mission task | n | name in init |
|------|-------|-----------------------|---|--------------|
| `GFTPAS---------` | TACGRP.TSK.FLWASS.FLWSUP | **Follow and support** | 3 | __FRIEN_08, _10, _11 |
| `GFTPH----------` | TACGRP.TSK.BRH | **Breach** | 2 | __FRIEN_05, _16 |
| `GFTPP----------` | TACGRP.TSK.PNE | **Penetrate** | 1 | __FRIEN_04 |
| `GFTPN----------` | TACGRP.TSK.NEUT | **Neutralize** | 1 | __FRIEN_06 |
| `GFTPS----------` | TACGRP.TSK.SEC | **Secure** | 1 | __FRIEN_07 |
| `GFTPA----------` | TACGRP.TSK.FLWASS | **Follow and assume** | 1 | __FRIEN_09 |
| `GFTPZ----------` | TACGRP.TSK.SZE | **Seize** | 1 | __FRIEN_12 |
| `GFTPX----------` | TACGRP.TSK.CLR | **Clear** | 1 | __FRIEN_13 |
| `GFTPF----------` | TACGRP.TSK.FIX | **Fix** | 1 | __FRIEN_15 |
| `GFTPD----------` | TACGRP.TSK.DSTY | **Destroy** | 1 | __FRIEN_17 |
| `GFTPUS---------` | TACGRP.TSK.SEC.SCN | **Screen** | 1 | __FRIEN_18 |
| `GFTPO----------` | TACGRP.TSK.OCC | **Occupy** | 1 | __FRIEN_20 |
| `GFTPUG---------` | TACGRP.TSK.SEC.GUD | **Guard** | 1 | __FRIEN_21 |

FM 3-90 1-84 (verbatim): "A tactical mission task is the specific activity a unit
performs while executing a tactical operation or form of maneuver. Tactical mission tasks
are used as components of a mission statement or given as tasks to subordinate units.
While all tactical mission tasks are defined, most have a symbol. Appendix B lists
tactical mission tasks, their definitions, and shows their associated symbol."

So the planner's DOCTRINAL intent for (at least) 16 of the COA's tasks is already shipped
to us as symbols, with 1-3 anchor points each, and Appendix B tells us what each of those
points means. Three consequences, all bearing on the rulings:

1. The verb-to-graphic-class table in section 1 below is not an invention: it is the same
   table MIL-STD-2525C and FM 3-90 Appendix B already agree on, and the data already
   carries the codes.
2. The `TaskActionCode` in the order and the task SYMBOL in the init sometimes DISAGREE.
   `GFTPAS` (follow and support) x3 and `GFTPN` (neutralize) exist in the init, but
   FOLASS/FOLSPT/NTRCOM appear nowhere in the 42 `TaskActionCode` values - those tasks
   were exported as ATTACK or SECURE. The exported verb is LOSSY relative to the drawn
   doctrinal task. (See section 6, R1 note 4.)
3. Selecting the right graphic when several are co-located is a DOCTRINAL question, and
   the SIDC answers it. See section 6, R1 note 3.

---

## 1. PER-VERB DOCTRINE TABLE

Column "graphic" = what the task is doctrinally defined against, with the FM 3-90 text on
the symbol's anchor points where it exists, and the 2525C SIDC.
Column "completion" = the doctrinal end state, i.e. what a simulation would have to
observe to declare the task done.
All quotes are verbatim. FM 3-90 = FM 3-90, *Tactics*, HQDA, 01 May 2023.

### 1.1 The 17 verbs in COA-STP1_Order.xml

| verb (C2SIM code) | doctrinal status + definition (verbatim) | graphic it is defined against | doctrinal completion / end state | citation |
|---|---|---|---|---|
| **ATTACK** (x10) | NOT a tactical mission task. "An attack is a type of offensive operation that defeats enemy forces, seizes terrain, or secures terrain." | An attack's minimum control measures are, verbatim: "A phase line as the LD, which may also be the line of contact (LC). The time to initiate the operation. **The objective.**" Objective = area graphic `G*GPOAO---`. | Ends on the objective: the objective is seized/secured. Not self-terminating without one. "If the objective is an enemy force, the reconnaissance element orients on it..." - the objective may be terrain OR an enemy force. | FM 3-90 5-1, 5-8, 5-23; glossary "attack" |
| **SECURE** (x4) | Tactical mission task. "Secure is a tactical mission task in which a unit prevents the enemy from damaging or destroying a force, facility, or geographical location. This task normally involves conducting area security operations." | Area symbol; "The direction of the arrow has no significance, but the symbol includes the entire area to be secured." Task symbol `G*TPS-----`. | **OPEN-ENDED by construction; doctrine REQUIRES a stated duration:** "The commander states the mission duration in terms of time or event when assigning a mission to secure a given unit, facility, or geographic location." Also: "the secured area or location is safe enough to build and project combat power" - no enemy direct or observed indirect fires may impact it. | FM 3-90 B-56 |
| **FIX** (x3) | Tactical mission task. "Fix is a tactical mission task in which a unit prevents the enemy from moving from a specific location for a specific period." | Enemy-oriented arrow: "The point of the arrow faces toward the desired enemy unit to fix. The broken part of the arrow indicates the desired location for that event to occur." Task symbol `G*TPF-----`. Also an obstacle effect. | **Time- or event-bounded and doctrine says so explicitly:** "This task usually has a time constraint, such as 'fix the enemy reserve force until OBJECTIVE FALON is secured.'" Distinguishing test: "a fixed enemy force cannot move from a given location, but a blocked enemy force can move in any direction other than the one obstructed." | FM 3-90 B-35, B-36, B-37 |
| **OCCUPY** (x3) | Tactical mission task. "Occupy is a tactical mission task in which a unit moves into an area to control it without enemy opposition. Both the friendly force's movement to and occupation of the area occur without enemy opposition." | Area symbol: "The symbol should encompass the entire area that a commander desires to occupy. Units typically occupy assembly areas, objectives, and defensive positions." Task symbol `G*TPO-----`; areas `G*GPGAA` (AA), `G*GPOAO` (obj), `G*GPDAB` (BP). | **EVALUABLE:** arrival inside the area + control of it. "A unit can control an area without occupying it, but not vice versa." Control = "maintains physical influence over an assigned area ... resulting from friendly forces occupying the specified area or dominating that area by their weapon systems." | FM 3-90 B-53, B-20 |
| **BREACH** (x3) | Tactical mission task. "Breach is a tactical mission task in which a unit breaks through or establishes a passage through an enemy obstacle. An enemy obstacle can include enemy defenses, obstacles, minefields, or fortifications." | "The area located between the arms of the graphic shows the general location for the breach. The length of the arms extend to include the entire depth of the area that must be breached." Task symbol `G*TPH-----`; the obstacle is `G*MPOGB---` (obstacle belt). A lane is a 2-point line. | **EVALUABLE:** a passage/lane exists through the obstacle. "A breach is a synchronized combined arms operation under the control of the maneuver commander." | FM 3-90 B-7, B-8 |
| **SCREEN** (x3) | NOT a tactical mission task; a TYPE OF SECURITY OPERATION. "Screen is a type of security operation that primarily provides early warning to the protected force (ADP 3-90)." | Oriented on the PROTECTED FORCE, not on ground the order names: "For a security force operating to the front of the main body, the lateral boundaries of the security area are normally an extension of the lateral boundaries of the main body. The security force's rear boundary is normally the battle handover line." Control measures: "phase lines, observation posts, named areas of interest, handover lines, and contact points." Task symbol `G*TPUS----`. | **NEVER SELF-COMPLETING.** "Displacement to these subsequent PLs is event driven (enemy or friendly) or time driven. The approach of an enemy force, relief of a friendly unit, or movement of the protected force dictates the movement of security forces." A screen ends on relief or battle handover, not on arrival. | FM 3-90 13-52, 13-54, 13-56, 13-59, 13-62 |
| **PENTRT** (x2) | NOT a tactical mission task in the 2023 edition (absent from table B-1); a FORM OF MANEUVER. "A penetration is a form of maneuver in which a force attacks on a narrow front." Retains a 2525C task symbol `G*TPP-----`. | An axis of advance on a narrow front + an objective beyond the enemy's forward defense. In our init: `G*GPOLAGS-` axis + `G*GPOAO---` objective (one is literally named OBJ_PENETR). | Same as ATTACK: on the objective. "A rupturing of the enemy's forward defense that occurs as a result of an attack" (glossary, breakthrough). | FM 3-90 2-36; glossary "penetration" |
| **BLOCK** (x2) | Tactical mission task. "Block is a tactical mission task that denies the enemy access to an area or an avenue of approach." | Enemy-avenue-oriented: "The line perpendicular to the enemy's line of advance indicates the limit of enemy advance." Task symbol `G*TPB-----`; obstacle effect `G*MPOEB---`. | **Time- or event-bounded and doctrine says so explicitly:** "A blocking task normally requires the friendly force to block the enemy force for a certain time, or until a specific event has occurred. A blocking unit may have to hold terrain and become decisively engaged." | FM 3-90 B-5, B-6 |
| **DESTRY** (x2) | Tactical mission task. "Destroy is a tactical mission task that physically renders an enemy force combat-ineffective until it is reconstituted." | Enemy-oriented symbol placed on the target unit. Task symbol `G*TPD-----`. | **EVALUABLE, but only against a NAMED ENEMY FORCE:** target is combat-ineffective. "The amount of damage needed to render a unit combat ineffective depends on the unit's type, discipline, and morale." | FM 3-90 B-23 |
| **DISRPT** (x2) | Tactical mission task. "Disrupt is a tactical mission task in which a unit upsets an enemy's formation or tempo and causes the enemy force to attack prematurely or in a piecemeal fashion." | "The center arrow points toward the targeted enemy unit." Task symbol `G*TPT-----`; also an obstacle effect. | **EFFECT-BASED, NOT DIRECTLY EVALUABLE.** "Disruption is not an end; it is the means to an end." The attacking force must "achieve the desired results with one mass attack or sustain the attack until it achieves the desired results." | FM 3-90 B-29, B-30, B-31 |
| **DEFEND** (x2) | NOT a tactical mission task; a TYPE OF OPERATION. "defensive operation - An operation to defeat an enemy attack, gain time, economize forces, and develop conditions favorable for offensive or stability operations. (ADP 3-0)" | Battle position `G*GPDAB---`: "A battle position is a defensive location oriented on a likely enemy avenue of approach (ADP 3-90) ... The battle position (BP) is a symbol that depicts the location and general orientation of most of the defending forces." NOT an assigned area. | **OPEN-ENDED.** Ends on the higher commander's transition decision, not on any observable the defending unit produces. | FM 3-90 glossary; A-63, A-64 |
| **MOVE** (x1) | Not a tactical mission task; movement. | A route or a destination point. In our init: the axis `G*GPOLAGS-`. | Arrival at the destination. | n/a |
| **ESCRT** (x1) | Not a tactical mission task. Doctrinally CONVOY SECURITY: "Convoy security. A convoy security operation is a specialized type of line of communication or route security operations." FM 1-02.1: "convoy security - A specialized area security task conducted to protect convoys. (ATP 3-91)" | The ROUTE and the escorted force. "Line of communication and route security operations are defensive in nature and are **terrain oriented**. A route security force prevents an enemy or adversary force from impeding, harassing, or destroying traffic along a route or portions of a route." In our init: `G*SPLCM---` moving convoy, `G*SPLRM---` MSR. | Ends when the convoy completes its move / the route-security period expires. Not self-completing as a "follow" task. | FM 3-90 14-x (LOC and route security bullet list); FM 1-02.1 "convoy security" |
| **GUARD** (x1) | NOT a tactical mission task; a TYPE OF SECURITY OPERATION. "Guard is a type of security operation conducted to protect the main body by fighting to gain time while preventing enemy ground observation of and direct fire against the main body (ADP 3-90)." | Same as screen: oriented on the protected main body; "It operates within the range of the main body's fire support weapons, deploying over a narrower front than a comparable-sized screening force." Task symbol `G*TPUG----`. | **NEVER SELF-COMPLETING** - same relief/handover logic as screen. | FM 3-90 13-72, 13-73, 13-75 |
| **SEIZE** (x1) | Tactical mission task. "Seize is a tactical mission task in which a unit takes possession of a designated area by using overwhelming force. An enemy force can no longer place direct fire on a seized objective." | "The arrow points to the location or objective to seize." Task symbol `G*TPZ-----`; the objective is `G*GPOAO---`. | **THE MOST EVALUABLE VERB IN THE SET:** "Once a friendly force seizes a physical objective, it clears the terrain within that objective by killing, capturing, or forcing the withdrawal of all enemy forces." Distinguishers: "This task differs from secure because it requires offensive action to obtain control of the designated area or objective. It differs from the task of occupy because it involves overcoming anticipated enemy opposition." | FM 3-90 B-57 |
| **RETAIN** (x1) | Tactical mission task. "Retain is a tactical mission task in which a unit prevents enemy occupation or use of terrain." | "The direction of the arrow has no significance, but the symbol includes the entire area to be retained." Task symbol `G*TPQ-----`. | **Doctrine REQUIRES an area and a duration:** "A commander assigning this task specifies the area to retain and the duration of the retention, which is time or event driven. While a unit is conducting this task, it expects enemy forces to attack and prepares for decisive engagement. A unit tasked to retain a specific piece of terrain does not necessarily have to occupy it." | FM 3-90 B-55 |
| **CLRLND** (x1) | Tactical mission task CLEAR. "Clear is a tactical mission task in which a unit eliminates all enemy forces within an assigned area. Friendly forces do this by destroying, capturing, or forcing the withdrawal of enemy forces, so they cannot execute organized resistance and interfere with the friendly unit's mission." | "The bar connecting the arrows designates the **desired limit of advance** for the clearing force. The bar also establishes the **width of the area to clear**." Task symbol `G*TPX-----`. | **EVALUABLE, AREA-DEFINED:** no enemy remains within the area, and the limit of advance is reached. Doctrine permits a size threshold: "a commander can modify the objective associated with this task to destroying, capturing, or forcing the withdrawal of only enemy forces larger than a stated size." Warning: clear is ALSO a mobility task (total elimination of an obstacle). | FM 3-90 B-16, B-17, B-18 |

### 1.2 The other Appendix B tasks STP can emit (mapping fodder for the 33 unmapped codes)

| task | definition (verbatim) | graphic | completion | citation |
|---|---|---|---|---|
| **ATTACK BY FIRE** (ARMAS/ATTSPT family) | "Attack by fire is a tactical mission task using direct and indirect fires to engage an enemy from a distance." | "The arrow points at the targeted force or objective, and the commander places the base of the arrow in the general area from which the commander wants to deliver the attack." `G*GPOAF---` is the attack-by-fire POSITION area. | Effect-based; "When assigning this task, the commander must state the desired effect on enemy forces, such as neutralize, fix, or disrupt." Note: "Attack by fire positions are rarely applicable to units larger than company size." | FM 3-90 B-3, B-4 |
| **SUPPORT BY FIRE** (ATTSPT) | "Support by fire is a tactical mission task in which a unit engages the enemy by direct fire in support of another maneuvering force." | "The ends of the arrows point in the general direction of the targeted unit or location. The base of the area indicates the general area from which to deliver fires." | Ends with the supported force's maneuver. "The support by fire tasked is rarely applicable to units larger than company size." | FM 3-90 B-58, B-59, B-61 |
| **SUPPRESS** (SUPPRS) | "Suppress is a tactical mission task in which a unit temporarily degrades a force or weapon system from accomplishing its mission." | Enemy/system-oriented. | **Explicitly temporary:** "the original target regains its effectiveness without needing to reconstitute once the effects of the systems involved in the suppression effort lift or shift to another target." | FM 3-90 B-62 |
| **NEUTRALIZE** (NTRCOM) | "Neutralize is a tactical mission task in which a unit renders the enemy incapable of interfering with an operation." | "The two lines cross over the symbol of the unit or facility targeted for neutralization." `G*TPN-----`. | **Doctrine REQUIRES a duration:** "a commander specifies the enemy force or materiel to neutralize and the duration, which is time or event driven." | FM 3-90 B-52 |
| **INTERDICT** | "Interdict is a tactical mission task where a unit prevents, disrupts, or delays the enemy's use of an area or route in any domain." | "The two arrows should cross on the unit or location targeted for interdiction." | **Doctrine REQUIRES a duration:** "An interdiction tasking must specify how long to interdict, defined as a length of time or some event that must occur before the interdiction is lifted, and the exact effect desired." | FM 3-90 B-46 |
| **CONTAIN** | "Contain is a tactical mission task in which a unit stops, holds, or surrounds an enemy force." | "The contain graphic encompasses the entire area desired to contain enemy forces." | Bounded by "geographic terms or time"; "Containment allows an enemy force to reposition itself within the designated geographic area, while fixing an enemy does not." | FM 3-90 B-19 |
| **CONTROL** | "Control is a tactical mission task in which a unit maintains physical influence over an assigned area." | Area symbol. | Open-ended. Key distinction from SECURE: "The control tactical mission task allows enemy direct and indirect fires to affect the location being controlled while secure does not." | FM 3-90 B-20 |
| **ISOLATE** | "Isolate is a tactical mission task in which a unit seals off an enemy, physically and psychologically, from sources of support and denies it freedom of movement." | "The position or direction of the arrow has no significance, but the graphic surrounds the targeted enemy unit." | Open-ended; the isolating force continues offensive action. | FM 3-90 B-51 |
| **TURN** | "Turn is a tactical mission task in which a unit forces an enemy force from one avenue of approach or movement corridor to another." | "The place where the arrow breaks indicates the general location of the obstacle complex." Also an obstacle effect. | Effect-based. | FM 3-90 B-63, B-64 |
| **CANALIZE** | "Canalize is a tactical mission task in which a unit restricts enemy movement to a narrow zone." | Symbol over the canalizing terrain. | Effect-based. | FM 3-90 B-15 |
| **BYPASS** | "Bypass is a tactical mission task in which a unit deliberately avoids contact with an obstacle or an enemy force." | "The arms of the graphic go on both sides of the location or unit that will be bypassed." | Evaluable: past the bypassed object with contact avoided. Two techniques: "Avoiding the enemy force totally" / "Fixing the enemy force in place with fires and then conducting the bypass." | FM 3-90 B-9 to B-14 |
| **FOLLOW AND ASSUME** (FOLASS) | "Follow and assume is a tactical mission task in which a committed force follows and supports a lead force conducting an offensive operation and continues the mission if the lead force cannot continue." | "Planners place the box part of the associated task military symbol around the icons of units assigned this task." `G*TPA-----`. | Open-ended; conditional (assume the lead's mission). Tasks include "Maintaining contact with the trail elements of the leading force." | FM 3-90 B-38, B-39 |
| **FOLLOW AND SUPPORT** (FOLSPT) | "Follow and support is a tactical mission task in which a committed force follows and supports a lead force conducting an offensive operation." | "It contains an arrow graphic around the symbol of the unit being assigned this task." `G*TPAS----`. | Open-ended. Tasks include "Destroying bypassed enemy units...", "Securing lines of communications", "Clearing obstacles", "Guarding prisoners, key areas, and installations", "Securing key terrain". | FM 3-90 B-42, B-43 |
| **COVER** | "cover - (Army) A type of security operation done independent of the main body to protect them by fighting to gain time while preventing enemy ground observation of and direct fire against the main body. (ADP 3-90)" | Oriented on the main body; `G*TPUC----`. | Never self-completing (security). | FM 3-90 glossary; 13-51 |
| **COUNTERRECONNAISSANCE** | "Counterreconnaissance is a tactical mission task that encompasses all measures taken by a unit to counter enemy reconnaissance and surveillance efforts. Counterreconnaissance is not a distinct mission, but a component of all security operations." | No standalone symbol. | Continuous, part of a security operation. | FM 3-90 B-21, B-22 |
| **REDUCE** | "Reduce is a tactical mission task in which a unit destroys an encircled or bypassed enemy force." | "There is no symbol for this task." | Enemy destroyed. | FM 3-90 B-54 |
| **DISENGAGE** | "Disengage is a tactical mission task in which a unit breaks contact with an enemy to conduct another mission or to avoid becoming decisively engaged." | Movement-away symbol. | Evaluable: out of enemy direct/observed indirect fire range. | FM 3-90 B-24 |
| **EXFILTRATE** (EVACT-adjacent) | "Exfiltrate is a tactical mission task in which a unit removes Soldiers or units from areas under enemy control by stealth, deception, surprise, or clandestine means." | Rally points + exfiltration lanes. | Evaluable: linkup complete. | FM 3-90 B-32 to B-34 |

Note on scope: FM 3-90 (2023) table B-1 lists exactly 27 tactical mission tasks -
Attack by fire, Block, Breach, Bypass, Canalize, Clear, Contain, Control,
Counterreconnaissance, Destroy, Disengage, Disrupt, Exfiltrate, Fix, Follow and assume,
Follow and support, Interdict, Isolate, Neutralize, Occupy, Reduce, Retain, Secure, Seize,
Support by fire, Suppress, Turn. **DEFEAT was removed as a tactical mission task in this
edition** (it is now a defeat mechanism, ADP 3-0), so a DEFEAT code from STP has no
Appendix B definition to anchor on - use DESTROY or NEUTRALIZE semantics with the
`DesiredEffectCode` (NUTRLD) as the discriminator, and log the substitution.

FM 3-90 B-2 is also directly relevant to any "unrecognised verb" policy (verbatim):
"Commanders are not limited to the tactical mission tasks listed in this appendix in
specifying desired subordinate actions in operation orders or operation plans. Many of the
words and terms used to describe the what of a mission statement do not have special
connotations beyond their common English language meanings. However, units must have a
shared understanding of the what of the operation."

### 1.3 The schema's enumeration, and what the C2SIM standard does NOT give us

Re-verified from `SDK\C2SIMSDK\C2SIM_SMX_LOX_CWIX2024.cs` this pass:

- The type is `TaskActionCodeType` (line 4295). There is no `TaskNameCode` type in the
  CWIX2024 schema. The assessment's correction stands.
- It has **453 members**: 7 simple C2SIM names (AssistOtherUnit, HoldInPlace,
  MoveToLocation, Observe, OrientToLocation, ReportPosition, UseCapability) followed by
  446 MIP codes, alphabetically ACQUIR..WLDWSL.
- **Every member is a bare identifier with an empty `/// <remarks/>` block. The schema
  carries NO definitions.** Confirmed by reading the generated source; the doc comments
  are empty for all 453.
- SISO-STD-019-2020 (C2SIM Core) and SISO-STD-020-2020 (LOX) do not define the task-code
  semantics either. Both normatively reference `[15] MIP JC3IEDM, Annexes, and .xsd Domain
  Values; www.mip-interop.org` and `[10] NATO APP-6, MILITARY SYMBOLS FOR LAND BASED
  SYSTEMS`. The standards give the transport and the ontology-to-XSD transformation
  (Annex B of SISO-STD-019), not the meaning of ATTACK vs SECURE.

**So the semantics of the verb are, by the standard's own reference chain, the
doctrinal/symbological ones: JC3IEDM domain values, APP-6 symbols, and behind those the
national tactical-mission-task definitions above. Doctrine IS the specification here; we
are not substituting it for one.**

---

## 2. R2 - WHAT A TASK WITHOUT GEOMETRY MEANS DOCTRINALLY

### 2.1 The general doctrinal rule (three prongs)

1. **A tactical mission task without a graphic is an incomplete order, not an
   "in place" order.** FM 3-90 1-84: "While all tactical mission tasks are defined, most
   have a symbol." 1-85: a mission statement contains "the elements of who, what, when,
   **where**, and why". For an attack specifically, 5-8 makes the objective a MINIMUM
   control measure: "Within these assigned areas, units at a minimum designate these
   control measures: A phase line as the LD ... The time to initiate the operation.
   The objective." For SECURE, RETAIN, NEUTRALIZE, INTERDICT, FIX, BLOCK, the appendix
   text literally says the commander SPECIFIES the area and the duration.
2. **For security tasks the geometry is derived from the PROTECTED FORCE, by rule, and the
   order does not have to state it.** FM 3-90 13-15 (verbatim): "Security forces focus all
   their actions on protecting and providing early warning to force, area, or facility
   they are securing. They operate **between** the force, area, or facility and known or
   suspected enemy units. If the force they are securing moves, security forces move
   moves [sic] and orients on their movement." And 13-52: "The force's main body
   establishes the security area. For a security force operating to the front of the main
   body, the lateral boundaries of the security area are normally an extension of the
   lateral boundaries of the main body. The security force's rear boundary is normally the
   battle handover line." Also 13-3: "security operations orient on the force, area, or
   facility, while reconnaissance operations orient on enemy and terrain."
3. **For air and missile defence the geometry is derived from the DEFENDED ASSET, by
   rule.** FM 3-01.44 p. 6-2 (verbatim): "M-SHORAD systems will generally be placed behind
   the lead elements of the supported force during movement to provide overwatch of enemy
   air avenues of approach. Air defense coverage is extended forward of the lead
   elements." And "the Avenger platoon will be employed to protect the supported unit's
   critical assets, such as the maneuver formations executing the main effort, artillery
   units, and C2 elements ... The platoon leader should position the squads and teams so
   that two-thirds of the weapon system's effective range extends in front of the maneuver
   force, if possible." Employment tenets, p. 1-3: "Early Engagement. Early engagement
   generally requires extending the defense away from the defended asset." / "Defense in
   Depth. Sensors and weapons are positioned so that the threat is exposed to a
   continuously increasing volume of fire as it approaches the friendly protected asset or
   force."

**Doctrinal verdict on R2: a zero-geometry task is neither a refusal nor a blind hold.
It is a task whose geometry doctrine tells you how to derive - from the named graphic in
the task statement, from the supported/protected force, or from the predecessor task -
and the derivation must be reported, never silent.** Refuse only when none of the three
prongs resolves.

### 2.2 The nine zero-geometry tasks, one by one

Taskee names resolved this pass by joining `PerformingEntity` UUID to the init's `Unit`
elements. "Derivable" = can be computed from data already on disk.

| # | verb | taskee | task name | what doctrine says the geometry IS | derivable from the order + init? |
|---|------|--------|-----------|-------------------------------------|-----------------------------------|
| **T8** | ATTACK | 4-27/2/1_A (4-27 FA bn) | ProvidePriorityFires**InSupportOfBrigadeDefensiveOperations** | Not a manoeuvre task at all. "priority of fires - The commander's guidance to his staff, subordinate commanders, fire support planners, and supporting agencies to organize and employ fire support in accordance with the relative importance of the unit's mission. (FM 3-09)". The FA battalion's own geometry is its **position area for artillery**: "A position area for artillery is an area assigned to an artillery unit to deliver surface to surface fires. A position area for artillery (PAA) is not an AO for the artillery unit occupying it." (FM 3-90 A-34) | **YES.** Remain in the current PAA; the init ships PAA areas (OBJ_PAA_25, OBJ_PAA_00, AT_PAA_25E/F, AAR_PAA_25, TAI_PAA_25/00) and predecessor T7 already anchors on the PAA cluster. Doctrinally correct rendering = an in-place fire-support posture, not a move. |
| **T9** | DEFEND | A/6-56/HHC (6-56 ADA) | ProvideAirDefenseCoverageFor**BrigadeManeuver**AndSustainmentCorridorsDuringPenetrationOperations | Orient on the defended asset: the supported brigade's manoeuvre elements and its sustainment routes. Position behind the lead elements with coverage extended forward (FM 3-01.44 p. 6-2). | **PARTIAL.** The supported force is identifiable (the 2/1 AD ORBAT is in the init, 128 units); the "sustainment corridors" are identifiable by SIDC (`G*SPLRM` LRM_PL_YEL = MSR, `G*SPLRW` LRW_PL_GOL = ASR, `G*SPASD` OILERS = division support area). But doctrine gives a RULE, not a polygon - "behind the lead elements", "two-thirds of range forward" - so the polygon is a POLICY choice we must make and report. **This is the only one of the nine where doctrine does not hand us an area.** |
| **T10** | DEFEND | A/6-56/HHC | ContinueAirDefenseCoverageForManeuverAndArtilleryPositions**SouthOfPlBronze** | Same rule as T9, but the task statement NAMES the control measure. "phase line - An easily identified feature in the operational area utilized for control and coordination of military operations. (JP 3-09)" | **YES.** The init has phase line **BRONZE** (`GFGPGLP--------`, 10 points) plus OBJ_PL_BRO and reference point PL_BRONZE; "artillery positions" = the PAA areas above. Geometry = the band south of PL BRONZE containing those PAAs. "Continue" + STREND from T9 is doctrinally a continuation of the same mission on the same asset. |
| **T16** | MOVE | 1-6/2/1_AD | AdvanceToSupportTheBrigadeTransitionTowardFixingOperations**NorthOfObjMonroe** | An objective is "A location used to orient operations, phase operations, facilitate changes of direction, and provide for unity of effort. (ADP 3-90)" (FM 3-90 A-29). The task statement names it. | **YES.** The init has TacticalArea **MONROE** with SIDC `GFGPOAO--------` = OBJECTIVE, plus OBJ_OBJ_MO and phase line PL_OBJ_MON. Predecessor T15 is the ATTACK along axis AOA_SE_1-6 (4 vertices). Destination = north of / short of OBJ MONROE along that axis. Unambiguous. |
| **T21** | BREACH | 40/2/1_AD (40 EN) | SupportBreachingAndMobilityOperationsThrough**ObstacleBelts**SouthOf**PlGold** | "The area located between the arms of the graphic shows the general location for the breach. The length of the arms extend to include the entire depth of the area that must be breached." (B-8) | **YES.** The init has phase line **GOLD**, TacticalAreas OBSENG_PL_ and OBSENG_OEB with SIDC `GFMPOGB--------` = **OBSTACLE BELT**, and obstacle-effect points OEB_PL_GOL. Predecessor T20 anchors on the PL GOLD cluster. **Doctrinal caution:** "support breaching" performed by a following unit is doctrinally FOLLOW AND SUPPORT, whose listed missions include "Clearing obstacles" (B-43) - and the init carries three `GFTPAS` (follow-and-support) task symbols. The BREACH code may be the lossy one here. |
| **T24** | SCREEN | 1-1/2/1_AD (1-1 CAV) | Screen**ForwardMovementOfTheBrigadeMainBody**AndIdentifyEnemyReserveMovementCorridors | **The most completely settled of the nine.** 13-52: security area's lateral boundaries = an extension of the main body's lateral boundaries; rear boundary = the battle handover line. 13-15: operate between the protected force and the enemy, and move with it. 13-62: control measures are phase lines, OPs, NAIs, handover lines, contact points. 13-61: "A screen normally requires the subordinate elements of the security force to deploy abreast." | **YES, by rule.** The init supplies brigade boundaries (`GFGPGLB----H---`: 1AD, 1/AD, __FRIEN_01) and 10 phase lines; the brigade main body's units are in the ORBAT. Screen line = the phase line forward of the main body, inside the brigade lateral boundaries. The chain's own later geometry brackets it: T25 anchors at the OBJ MONROE / PL_OBJ_MON cluster and T26 is literally named PL_PL_BLUE. |
| **T34** | RETAIN | 5-20/2/1_A | Retain**ObjMadison**AndSecureBrigadeRearApproaches | "A commander assigning this task specifies the area to retain and the duration of the retention" (B-55). The task statement names the area. | **YES, unambiguously.** TacticalArea **MADISON**, SIDC `GFGPOAO--------` = OBJECTIVE. Predecessor T33 (SECURE) already anchors on exactly that cluster (its single Location is shared by OBJ_OBJ_MA, OBJ_BND_OB, PL_OBJ_MAD, BND_OBJ_MA). |
| **T37** | ATTACK | 1375ca0a = B/5-20 | Provide**Follow-OnSecurityAndSupport**ForArtilleryDisplacementAndSustainmentOperations | Doctrinally FOLLOW AND SUPPORT, not attack: "A follow and support force is not a reserve but is a force committed to specific tasks" whose missions include "Securing lines of communications", "Guarding prisoners, key areas, and installations", "Securing key terrain" (B-42, B-43). Geometry = the supported force (the displacing artillery), by 13-15. | **YES.** Predecessor T36 (CLRLND) has 3 Locations at the PAA 25E cluster and the init has AT_PAA_25E / AS_PAA_25E / PAA_25E. In-place continuation oriented on the artillery. |
| **T38** | SECURE | B/5-20 | Secure**ArtillerySupportAreas**And**SustainmentCorridors** | "Secure ... prevents the enemy from damaging or destroying a force, facility, or geographical location. This task normally involves conducting area security operations ... The commander states the mission duration in terms of time or event." (B-56). Route security is "defensive in nature and ... terrain oriented" (FM 3-90, LOC and route security). | **YES by name class.** "Artillery support areas" = the PAA-named graphics (AT_PAA_25E `GFFPAT`, AT_PAA_25F, AAR_PAA_25, OBJ_PAA_25 `GFGPOAO`, TAI_PAA_25 `GFGPSAT`); "sustainment corridors" = MSR LRM_PL_YEL and ASR LRW_PL_GOL. Duration is NOT in the data - see R4. |

**Score: doctrine resolves 8 of 9 to a determinate geometry that exists in the init;
the ninth (T9) gets a doctrinal RULE but needs one policy constant from us.**

Six of the nine name a graphic that is present in the init by name and by SIDC class:
OBJ MONROE (T16), OBJ MADISON (T34), PL GOLD + obstacle belts (T21), PL BRONZE + PAAs
(T10), artillery support areas + supply routes (T38), the artillery being supported (T37).
Two are resolved by the security/fire-support orientation rule (T24, T8).

---

## 3. R3 - IS "THE ENEMY IS WHATEVER IS INSIDE THE OBJECTIVE AREA" THE DOCTRINAL MEANING?

**Answer: YES for the area-possession tasks, and NO for the enemy-effect tasks. Doctrine
draws the line sharply, and it is the same line MIL-STD-2525C draws between area graphics
and enemy-overlay graphics.**

### 3.1 Where area-substitution IS the doctrinal meaning

| task | the doctrine text that says so |
|------|--------------------------------|
| **SEIZE** | "Once a friendly force seizes a physical objective, it **clears the terrain within that objective** by killing, capturing, or forcing the withdrawal of **all** enemy forces." (B-57) That is exactly "the enemy is whatever is inside the objective". |
| **CLEAR** | "Clear is a tactical mission task in which a unit eliminates **all enemy forces within an assigned area**." (B-16) The enemy is DEFINED by the area. The area's extent is given by the graphic: "The bar connecting the arrows designates the desired limit of advance ... The bar also establishes the width of the area to clear." |
| **OCCUPY** | "moves into an area to control it **without enemy opposition**" (B-53). There is no enemy at all; area-substitution is trivially correct, and a live enemy in the area means the task was mis-assigned. |
| **SECURE** | Area security: "prevents the enemy from damaging or destroying a force, facility, or geographical location ... not only prevents enemy forces from over running or occupying the secured location, but also prevents enemy direct fires and observed indirect fires from impacting the secured location." (B-56) The enemy is whoever comes to the area. |
| **RETAIN** | "prevents enemy occupation or use of terrain ... it expects enemy forces to attack and prepares for decisive engagement." (B-55) Same. |
| **CONTROL** | "maintains physical influence over an assigned area ... Control of an area does not require the complete clearance of all enemy soldiers from that area." (B-20) Area-defined, with a weaker clearance standard than seize. |
| **ATTACK** as a type of operation | "An attack is a type of offensive operation that defeats enemy forces, **seizes terrain, or secures terrain**." (5-1) A terrain objective is a legitimate whole objective, and 5-8 makes "The objective" a minimum control measure of every attack. |

### 3.2 Where a NAMED ENEMY FORCE is required instead

These tasks are defined by their EFFECT ON A SPECIFIC ENEMY, and every one of their
symbols is anchored ON the enemy, not on ground:

| task | the doctrine text that requires a named enemy |
|------|-----------------------------------------------|
| **DESTROY** | "physically renders **an enemy force** combat-ineffective until it is reconstituted" (B-23); the symbol goes on the target unit. The completion test is the target's combat effectiveness - meaningless without a target. |
| **NEUTRALIZE** | "a commander **specifies the enemy force or materiel** to neutralize and the duration" (B-52); "The two lines cross over the symbol of the unit or facility targeted." |
| **FIX** | "prevents **the enemy** from moving from a specific location" (B-35); "The point of the arrow faces toward the desired **enemy unit** to fix. The broken part of the arrow indicates the desired location for that event to occur." Note the symbol carries BOTH: the enemy AND the place. |
| **DISRUPT** | "upsets **an enemy's** formation or tempo" (B-29); "The center arrow points toward the targeted enemy unit." |
| **SUPPRESS** | "temporarily degrades **a force or weapon system**" (B-62). |
| **BLOCK** | "denies **the enemy** access to an area or an avenue of approach" (B-5) - enemy + avenue, and the limit line is drawn "perpendicular to the enemy's line of advance". |
| **TURN / CANALIZE** | both are defined as moving a specific enemy formation between avenues (B-63, B-15). |
| **ISOLATE / CONTAIN** | "seals off **an enemy**" (B-51); "stops, holds, or surrounds **an enemy force**" (B-19). |
| **INTERDICT** | "prevents, disrupts, or delays **the enemy's** use of an area or route" (B-46); "The two arrows should cross on the unit or location targeted." |
| **ATTACK BY FIRE / SUPPORT BY FIRE** | "the commander designates **the enemy force**, when to attack, the general location from which to operate, the friendly force to support, and the purpose" (B-59). |
| **BREACH** | the object is a specific ENEMY OBSTACLE, not an area: "breaks through or establishes a passage through **an enemy obstacle**" (B-7). Our data has that object: `G*MPOGB---` obstacle belts and `G*MPOEB---` obstacle effects. |

### 3.3 The ruling this supports

- For **SEIZE, CLEAR, OCCUPY, SECURE, RETAIN, CONTROL** and for an **ATTACK with a terrain
  objective**, "the enemy is whatever is inside the objective area" **is** the doctrinal
  semantics, verbatim, and no `AffectedEntity` is needed. The 42/42 self-targeting in
  COA-STP1 is therefore NOT a data defect for these verbs - it is the normal case, and the
  interface treating self-target as an error is the defect.
- For **DESTROY, NEUTRALIZE, FIX, DISRUPT, SUPPRESS, BLOCK, TURN, CANALIZE, ISOLATE,
  CONTAIN, INTERDICT, ATTACK BY FIRE, SUPPORT BY FIRE**, doctrine requires a designated
  enemy. Substituting "whatever is in the area" changes the task. For those verbs the
  honest fallback is: engage what is inside the named enemy-oriented graphic (the `G*GPSAT`
  TAI, the `G*FPAT` area target, the `G*MPOGB` obstacle belt), AND report that the named
  enemy was not supplied. COA-STP1 has exactly those graphics: TAI_PAA_25, TAI_PAA_00,
  TAI_ABF_JU, AT_PAA_25E, AT_PAA_25F. A **targeted area of interest** is doctrinally the
  right stand-in for a named enemy force: it is where the enemy is expected to be engaged.
- FM 3-90 B-2's caution applies to any substitution: "units must have a shared
  understanding of the what of the operation". A silent substitution breaks that; a
  reported one does not.

---

## 4. R4 - WHEN IS THE TASK COMPLETE?

Doctrine sorts the verbs into three groups, and it is unusually explicit.

### Group A - doctrine gives a state the simulation can evaluate

| verb | evaluable end state, doctrinally |
|------|----------------------------------|
| **SEIZE** | Taskee in possession of the objective AND no enemy able to place direct fire on it AND the terrain within the objective cleared. "An enemy force can no longer place direct fire on a seized objective ... it clears the terrain within that objective by killing, capturing, or forcing the withdrawal of all enemy forces." (B-57) |
| **OCCUPY** | Taskee inside the area, in control, no opposition encountered. (B-53 + B-20) |
| **CLEAR** | No enemy remaining in the area, and the limit of advance reached. (B-16) |
| **BREACH** | A passage/lane exists through the obstacle. (B-7) |
| **DESTROY** | Named enemy force combat-ineffective. (B-23) - requires a named enemy. |
| **BYPASS** | Past the bypassed object, contact avoided. (B-9 to B-13) |
| **DISENGAGE** | Out of enemy direct and observed indirect fire. (B-24) |
| **MOVE** | Arrival. |
| **ATTACK** (type of op) | The objective taken - i.e. it reduces to SEIZE or SECURE on the objective. (5-1, 5-8) |

### Group B - doctrine says the task is OPEN-ENDED and the ORDER MUST SUPPLY A DURATION

This is the important group, because doctrine does not merely permit a duration - it makes
the commander responsible for stating one. A task in this group **without** a duration is
an incomplete order by doctrine, not a "run forever" instruction.

| verb | the sentence that requires the duration |
|------|-----------------------------------------|
| **SECURE** | "The commander states the mission duration in terms of **time or event** when assigning a mission to secure a given unit, facility, or geographic location." (B-56) |
| **RETAIN** | "A commander assigning this task specifies the area to retain and **the duration of the retention, which is time or event driven**." (B-55) |
| **BLOCK** | "A blocking task normally requires the friendly force to block the enemy force **for a certain time, or until a specific event has occurred**." (B-5) |
| **FIX** | "This task usually has a time constraint, such as '**fix the enemy reserve force until OBJECTIVE FALON is secured**.'" (B-36) |
| **NEUTRALIZE** | "a commander specifies the enemy force or materiel to neutralize and **the duration, which is time or event driven**." (B-52) |
| **INTERDICT** | "An interdiction tasking **must specify how long to interdict**, defined as a length of time or some event that must occur before the interdiction is lifted." (B-46) |
| **CONTAIN** | "geographic terms **or time** may express the limits of the containment." (B-19) |

### Group C - never self-completing; terminated by RELIEF or by the higher commander

| verb | doctrine |
|------|----------|
| **SCREEN, GUARD, COVER** (all security operations) | "Displacement to these subsequent PLs is event driven (enemy or friendly) or time driven. **The approach of an enemy force, relief of a friendly unit, or movement of the protected force** dictates the movement of security forces." (13-54) The rear boundary is the **battle handover line** (13-52) - the doctrinal handoff event. |
| **DEFEND** (defensive operation) | Ends on the higher commander's transition decision. The battle position "is a symbol that depicts the location and general orientation of most of the defending forces" and is explicitly "not an assigned area" (A-63) - so "arrived at the BP" is not completion, it is occupation. |
| **FOLLOW AND ASSUME / FOLLOW AND SUPPORT** | Committed for the duration of the lead force's operation (B-38, B-42). |
| **CONTROL, ISOLATE** | Maintained (B-20, B-51). |
| **SUPPRESS** | Ends when fires lift or shift - i.e. when the SUPPORTING act stops, not when the taskee achieves something (B-62). |
| **DISRUPT / TURN / CANALIZE** | Effect-based; "Disruption is not an end; it is the means to an end." (B-30) |
| **ESCORT / convoy security** | Ends when the protected convoy completes its move; route security is terrain-oriented and periodic. |

### What C2SIM offers to close Group B and Group C - and what is in our data

C2SIM `ManeuverWarfareTaskType` already carries the two things doctrine asks for
(`SDK\C2SIMSDK\C2SIM_SMX_LOX_CWIX2024.cs:4026-4194`):

| doctrinal need | C2SIM field | present in COA-STP1_Order.xml? |
|---|---|---|
| "time" | `Duration` (:4132), `EndTime` (:4142) | **NO. Verified this pass by parsing the XML: `Duration` and `EndTime` are absent from all 42 tasks.** STP *computes* one (`C2SimXmlBuilder.cs:~450`, `mwtt.Duration = TimeSpanToIso8601Duration(TimeSpan.FromMinutes(task.Duration * _params.PhaseDuration))`) and `EndTime` is commented out in the builder - so the duration is lost between STP's model and the exported file. |
| "event" | `ActionTemporalRelationship` with association code **STREND** (start-at-end-of predecessor) + `TimeReferenceCode` IntervalEndTime | **YES, on 31 of 42.** This is the doctrinal "until a specific event has occurred" - expressed backwards: the SUCCESSOR is bound to the predecessor's end rather than the predecessor being bound to an end condition. |
| "event" (alternative) | `TaskFunctionalRelation` (:4194) | present in the schema; **0 uses**; not lifted by `OrderParser.cs`. |

**Doctrinal verdict on R4:**

1. Group A closes on a state the simulation can observe. Wire those.
2. Group B tasks in COA-STP1 (SECURE x4, RETAIN, BLOCK x2, FIX x3 = 10 of 42) are
   doctrinally incomplete as exported: no `Duration`, no `EndTime`. Doctrine's own answer
   is "time **or** event", and the order *does* supply the event in the STREND chain -
   **a Group B task should be closed when its successor in the chain is due to start**,
   which is precisely the FM 3-90 B-36 pattern ("fix ... until OBJECTIVE FALON is
   secured"). That is derivable from data already in the file, for the 31 tasks that have
   a chain. For a chain-terminal Group B task (T34 RETAIN, T38 SECURE, T42 BLOCK, T4
   BLOCK) there is no successor and no duration - those need either the STP re-export of
   `Duration` or an explicit policy constant.
3. Group C tasks must NOT be treated as completing. Chaining a never-completing task as a
   STREND predecessor stalls everything behind it - which is the T24 -> T25 -> T26 screen
   chain exactly. Doctrine's own termination event for a screen is battle handover or
   relief; the simulation's proxy for that is the `Duration`, which is why a
   reconnoiter-with-duration is the doctrinally faithful mapping and a bare patrol is not.
4. `DesiredEffectCode` is doctrinally load-bearing and is currently dropped by
   `OrderParser.cs`. FM 3-90 B-3 ("the commander must state the desired effect on enemy
   forces, such as neutralize, fix, or disrupt") and B-59 make the effect part of the task
   assignment. COA-STP1 carries TaskSuccess x40 and **DSTRYK x2** (the two DESTRY tasks).
   Lifting that one field distinguishes destroy from defeat from neutralize without any
   new data.

---

## 5. WHAT DOCTRINE DOES **NOT** SETTLE

- **R5 (EntityLevel first vs straight to AggregateTacticalLevel)** - an engineering and
  schedule question. Doctrine is indifferent to the vendor's simulation-model-set split.
  One doctrinal input only: FM 3-90 B-3 and B-58 both say attack-by-fire and support-by-
  fire positions are "rarely applicable to units larger than company size", so an
  entity-level company representation is doctrinally appropriate for those two tasks and
  an aggregate battalion is not. That argues for keeping BOTH columns, as the assessment
  recommends.
- **R6 (type map: create COA-STP1 companies as COMPANY types)** - an engineering question,
  but doctrine does bear on it: the vendor tasks that need a company type
  (`company_seize`, `co_clear`, `company_breach`) implement tasks doctrine assigns at
  company level, and COA-STP1's taskees are 5 battalions + 4 companies + 2 unattributed
  (per the settled "COA is not the ORBAT" ruling). So company-typing is right for the 4
  company taskees and WRONG for the 5 battalion taskees, which doctrinally get
  battalion-level treatment (an attack with a security force, a main body and a reserve -
  FM 3-90 5-2). A single global switch is the wrong shape; the type should follow the
  echelon of the taskee.
- **R1 (objective resolution by name vs MapGraphicID re-export)** - the *mechanism* is an
  STP data question, not a doctrinal one. But doctrine settles three sub-questions inside
  it, and the data settles a fourth. See below.

### What doctrine DOES say about R1

1. **A doctrinal task statement always names its objective or graphic.** FM 3-90 1-85: a
   mission statement contains "who, what, when, **where**, and why". The manual's own
   examples name the graphic: "fix the enemy reserve force **until OBJECTIVE FALON is
   secured**" (B-36); "Figure A-19 on page A-11 depicts **OBJECTIVE PAT**. OBJECTIVE PAT
   is further divided into two subordinate objectives: OBJECTIVE KAI and OBJECTIVE ZEKE."
   (A-29); "Figure A-7 depicts **CHECKPOINT 13**" (A-18). Resolving the objective FROM THE
   TASK NAME is therefore reading the order the way doctrine writes it, not a hack. STP's
   task names follow the same convention exactly: `T2_PL_OBJ_MADISON`, `T3_PL_OBJ_MONROE`,
   `T33_SecureObjMadison...`, `T34_RetainObjMadison...`, `T36_...FromPaa25E...`,
   `T10_...SouthOfPlBronze`, `T21_...SouthOfPlGold`, `T16_...NorthOfObjMonroe`.
2. **The task's own symbol carries its geometry.** FM 3-90 1-84: tactical mission tasks
   have symbols, and Appendix B "shows their associated symbol". Each Appendix B entry
   then tells you what the anchor points MEAN (the arrow points at the target; the bar is
   the limit of advance; the arms span the depth of the breach; the box goes around the
   unit). We have 16 such symbols in the init with 1-3 points each. Resolving a task to
   its own task graphic is the doctrinally primary route; resolving to the objective AREA
   is the secondary one.
3. **Where several graphics are co-located, the VERB selects which one is the anchor -
   and the SIDC makes that computable.** This matters because I measured the ambiguity:
   of the 33 tasks with geometry, the 20 single-Location tasks all land on a coordinate
   SHARED BY A WHOLE CLUSTER of graphics (T4/T11/T12/T14/T18/T22/T26/T30 all share one
   point that is simultaneously on BP_PL_BLUE, three more battle positions, two obstacle
   belts, phase line PL_PL_BLUE, the occupy task symbol and the guard task symbol, plus
   eleven reference points). Coordinate matching alone identifies the CLUSTER, never the
   graphic. The doctrinal verb-to-SIDC-class table resolves it:

   | verb | doctrinally correct anchor class | SIDC |
   |------|----------------------------------|------|
   | SEIZE, ATTACK, PENTRT, CLRLND(area) | Objective | `G*GPOAO---` |
   | OCCUPY, DEFEND, BLOCK(defensive) | Battle position (or assembly area / objective) | `G*GPDAB---`, `G*GPGAA---` |
   | SCREEN, GUARD | Phase line + boundaries of the protected force | `G*GPGLP---`, `G*GPGLB---` |
   | BREACH | Obstacle belt / obstacle effect | `G*MPOGB---`, `G*MPOEB---` |
   | DESTRY, DISRPT, FIX, NEUTRALIZE | TAI / area target | `G*GPSAT---`, `G*FPAT----` |
   | ESCRT | Moving convoy / MSR | `G*SPLCM---`, `G*SPLRM---` |
   | MOVE, and any task with a route | Axis of advance | `G*GPOLAGS-` |
   | SECURE (artillery/CSS) | PAA-named areas, DSA, supply routes | `G*FPAT----`, `G*SPASD---`, `G*SPLRM/W-` |

   (The 13 tasks whose Locations came from a ROUTE - T1, T15, T17, T19, T23, T31, T32,
   T35, T39 with 4 points each, plus T2, T3 with 2, T13, T36 with 3 - matched a single
   graphic EXACTLY, 4/4 or 3/3 points: the axes `AOA_SE_*` and the task symbols
   `GFTPH` breach (T13) and `GFTPX` clear (T36). Route-derived geometry is unambiguous;
   point-derived geometry is not.)
4. **The link already exists upstream and is thrown away at export.** Read this pass in
   `STP\...\C2SimBridge\C2SimTask.cs`: STP's own task object has an `Objective` property,
   populated in the constructor from the task's tactical graphics by SIDC -
   `else if (tg.IsObjective) Objective = tg;` where `IsObjective => C2SimUtil
   .IsObjectiveSidc(Sic)` and the regex is `G.G(P|A)OAO.*` - the same objective SIDC our
   init carries. In `C2SimXmlBuilder.cs` the branch that would emit it is commented out:

   ```
   //else if (task.Objective != null)
   //{
   //    listRct.Add(LatLonToC2SimLocation(task.Objective.Centroid, task.Objective.Altitude));
   //}
   ```

   So R1's "STP re-export" option is not a feature request. It is asking for a field STP
   already computes, by the same doctrinal rule (the objective SIDC) that we would use.

---

## 6. STP'S OWN CONVENTIONS (cross-check, read-only, `C:\Users\PauloBarthelmess\Source\Repos\STP`)

| finding | source |
|---|---|
| **A task's Name is the anchoring graphic's name when the task has a route.** `string name = (task.Routes != null && task.Routes.Count > 0) ? task.Routes[0].Name : task.Name.TrimStart('_'); mwtt.Name = $"T{taskCounter++}_{name}".Replace(" ", "_");` So `T2_PL_OBJ_MADISON` IS the route graphic named `PL_OBJ_MADISON`. This is a direct link, not a guess. | `HEAD\BridgingAgents\C2SimBridge\C2SimBridge\C2SimXmlBuilder.cs:~536` |
| **Otherwise the Name is STP's title-cased `task_description`** - the prose names. `name = (fs["task_description"] as Term)?.GetString(); name = name.ToTitleCase().Replace(" ", string.Empty);` That is why `T33_SecureObjMadisonAndSupportTheBrigadeTransition` reads like a sentence and names its objective: it IS the planner's task statement. | `STP\NallSuite\Agents\C2SimBridge\C2SimTask.cs:LoadFS` |
| **Task-to-graphic classification is by SIDC, and it is doctrinal.** `IsRoute => C2SimUtil.IsRouteSidc(Sic)`, `IsObjective => C2SimUtil.IsObjectiveSidc(Sic)`, `IsTaskTg => C2SimUtil.IsTaskTgSidc(Sic)`. `ObjectiveRE = "G.G(P|A)OAO.*"` (comment: "From prolog server task_generation_tables.pl objective definitions"); `TaskRE = "G.T.*"` (comment: "From MilFunctionTG.xlxs"); `RouteRE` enumerates axis-of-advance, direction-of-attack, supply-route, convoy, lane and air-corridor SIDCs. | `STP\NallSuite\Agents\C2SimBridge\C2SimUtil.cs:109-153`; `C2SimTg.cs:35-39` |
| **`IncludeMapGraphicIdInTasks` exists and was OFF for both exports on disk.** `if (_params.IncludeMapGraphicIdInTasks) { mwtt.MapGraphicID = task.Tgs.Select(i => i.Guid).ToArray(); }` - and 0 `MapGraphicID` elements appear in either export, while all 409 graphics DO appear in the Initialization, so the exports ran `PlaceAllTgInInitialization=True, IncludeMapGraphicIdInTasks=False`. | `C2SimBridgeAgentParams.cs:128`; `C2SimXmlBuilder.cs:383, 460` |
| **The exports were built with `NONAMES` defined.** The builder emits `$"{guid}%[{name}]%"` when `NONAMES` is not defined; our order carries bare GUIDs in `PerformingEntity`/`AffectedEntity`. So if `MapGraphicID` is switched on we should expect bare GUIDs there too - which is fine, because the init creates each area in VR-Forces under its C2SIM UUID. | `C2SimXmlBuilder.cs:440-470` |
| **`AffectedEntity` is hard-set to the performing unit.** `mwtt.AffectedEntity = new string[1] { task.Who.Guid };` - the 42/42 self-target is by construction, not a planner choice. Doctrinally this is fine for the area-possession verbs (sec 3.1) and a real gap for the enemy-effect verbs (sec 3.2). | `C2SimXmlBuilder.cs:~444` |
| **Location for a non-route task is the FIRST tactical graphic, linearised.** `listRct = task.Tgs[0].GetLinerarizedCoords()...` with a live `// TODO: What if there are multiple TGs??`. This is why point-derived geometry is cluster-ambiguous. | `C2SimXmlBuilder.cs:~490` |
| **Init graphic names are TRUNCATED; task names are not.** `baseName = baseName.Substring(0, _params.EntityNameCharLimit)` - observed limit 10 in our data (`OBJ_MADISO`, `AOA_SE_1-3`, `BANDIT_III`), with `__00/__01` disambiguation suffixes. But the task name carries the FULL route name (`T1_AOA_SE_1-35_AR;_2/1_AD_P1`). **Any name match must be a prefix/normalised match, never equality** - and the 10-char truncation collides (`AOA_SE_1-1` is both `AOA_SE_1-1_RECON` and, potentially, others). | `C2SimXmlBuilder.cs:1132-1160` |

---

## 7. WHAT REMAINS THE USER'S CALL

Doctrine narrows the six rulings to these residuals:

| # | what doctrine settled | what is still a decision |
|---|------------------------|---------------------------|
| **R1** | Resolving the objective from the task NAME and from the task's own SYMBOL is how doctrine writes and reads orders (1-84, 1-85, A-29, B-36). The SIDC class, present on all 409 graphics, disambiguates co-located graphics by verb. STP already computes `task.Objective` by the same objective SIDC and drops it at export. | Whether to (a) ask STP to re-enable `IncludeMapGraphicIdInTasks` and/or the commented-out `task.Objective` branch, or (b) compute the link locally from SIDC + name-prefix + route-exact-match. Doctrine does not choose between a clean upstream fix and a correct local one. **Note: (b) is now much stronger than the assessment assumed, because of the SIDC field.** |
| **R2** | Zero geometry is an incomplete order, not a hold. Doctrine derives the geometry from the named graphic, the protected/supported force, or the predecessor. 8 of the 9 resolve to areas that exist in our init. | The one policy constant for T9 (what polygon "air defence coverage for brigade manoeuvre" means), and whether an unresolvable task is refused or degraded - doctrine only requires that the shared understanding be preserved, i.e. that the substitution be REPORTED. |
| **R3** | Area-substitution IS the doctrinal meaning for SEIZE/CLEAR/OCCUPY/SECURE/RETAIN/CONTROL and terrain-objective ATTACK. It is NOT for DESTROY/NEUTRALIZE/FIX/DISRUPT/SUPPRESS/BLOCK/TURN/CANALIZE/ISOLATE/CONTAIN/INTERDICT/ABF/SBF. Self-target is the NORM for the first group. | Whether the second group falls back to the TAI/area-target graphics (which COA-STP1 ships) or is reported as unsatisfiable. |
| **R4** | Three groups (evaluable / duration-required / relief-terminated). Doctrine REQUIRES a stated duration for SECURE, RETAIN, BLOCK, FIX, NEUTRALIZE, INTERDICT, CONTAIN. The STREND chain is doctrine's "event" half and is present on 31 of 42 tasks. `Duration`/`EndTime` are absent from the file although STP computes a Duration. | Whether to close a Group B task on its successor's start (derivable now), on a re-exported `Duration`, or on a policy constant - and what to do with the 4 chain-terminal Group B tasks that have neither. |
| **R5** | Nothing directly; ABF/SBF are company-level tasks by doctrine (B-3, B-58). | Unchanged - an engineering/schedule call. |
| **R6** | The company-typed vendor tasks implement company-level doctrine; COA-STP1's taskees are a mix of battalions and companies, so the type should follow the taskee's echelon, not a global switch. | Unchanged - an engineering call, now with a shape constraint. |

---

## 8. ADVERSARIAL NOTES ON THIS DOCUMENT

- **Strongest competing hypothesis I tested and rejected:** that the doctrinal definitions
  are too abstract to constrain the mapping, so the rulings are purely engineering. That
  is falsified by the explicit duration clauses (B-5, B-36, B-46, B-52, B-55, B-56), by
  13-52/13-15 deriving the screen's geometry from the protected force by rule, and by the
  SIDC field making the verb-to-graphic table computable from data on disk. Doctrine is
  operative here, not decorative.
- **Second competing hypothesis, partly SUSTAINED:** that name-based objective resolution
  is fragile. It is - because the init truncates names to 10 characters while task names
  are untruncated, and because 20 of 33 located tasks share a cluster anchor point. The
  mitigation is the SIDC class filter plus route-exact-match, not name matching alone.
  Anyone building R1 on name equality will get silent mis-anchoring.
- **A symptom I cannot explain and am not burying:** the exported `TaskActionCode`
  disagrees with the drawn task SYMBOL in at least four places. The init contains three
  FOLLOW AND SUPPORT symbols (`GFTPAS`), one FOLLOW AND ASSUME (`GFTPA`) and one
  NEUTRALIZE (`GFTPN`), but FOLASS/FOLSPT/NTRCOM appear in none of the 42 task codes -
  those tasks were exported as ATTACK (T14, T22, T29, T37) or SECURE (T20) or DESTRY (T5,
  whose single Location sits on the `GFTPN` neutralize symbol). Whether the verb or the
  symbol is authoritative is an STP question I could not answer from the source I read.
  It is a live risk to any mapping keyed only on `TaskActionCode`.
- **Not verified:** that the `GFGPAAR` and `GFSPPSZ` decodings are exactly right (they are
  in the aviation-areas and CSS-support-points branches of 2525C but I did not pin the
  leaf). Neither is load-bearing for any ruling above.
- **Not verified:** FM 3-90 (2023) is the current edition as of 2026-09-14. I read the
  01 May 2023 printing; I did not check armypubs for a 2025/2026 change. The definitions
  quoted are corroborated by FM 1-02.1 (February 2024), which cites FM 3-90 for each, so a
  silent redefinition between the two is unlikely but not excluded.
- **Marine Corps publications (MCDP/MCWP):** consulted only as far as FM 3-90's own
  dual-designated references (ATP 3-90.4/MCTP 3-34A for breaching, ATP 4-01.45/MCRP
  3-40F.7 for convoy security). No MCDP text is quoted, and none was needed - the Army
  and Marine tactical mission task definitions are the dual-designated same text.

---

## 9. SOURCES

Primary doctrine (downloaded and read this pass, full text, not recalled):

- **FM 3-90, *Tactics*, HQDA, 01 May 2023.** Appendix B (Tactical Mission Tasks) B-1 to
  B-64; Chapter 1 paras 1-56, 1-84, 1-85; Chapter 5 paras 5-1, 5-2, 5-8, 5-23;
  Chapter 13 (Security Operations) paras 13-2, 13-3, 13-12, 13-15, 13-51, 13-52, 13-54,
  13-56 to 13-62, 13-72 to 13-75; Chapter 14 (LOC/route/convoy security bullets);
  Appendix A paras A-16, A-17, A-18, A-29, A-34, A-35, A-63, A-64; Glossary.
  https://archive.org/download/fm-3-90-tactics-2023/FM%203-90%20Tactics%202023.pdf
  (official copy: https://armypubs.army.mil)
- **FM 1-02.1, *Operational Terms*, HQDA, February 2024** (supersedes the 09 March 2021
  edition). Chapter 1, entries: attack, area security, battle position, block, breach,
  clear, convoy security, destroy, disrupt, fix, guard, line of departure, mission
  statement, movement to contact, objective, occupy, penetration, phase line, position
  area for artillery, priority of fires, retain, screen, secure, seize.
  https://armypubs.army.mil/epubs/DR_pubs/DR_a/ARN40321-FM_1-02.1-000-WEB-2.pdf
- **FM 3-01.44, *Short-Range Air Defense Operations*, HQDA, 21 July 2022.** p. 1-3 (AMD
  employment tenets: balanced fires, weighted coverage, early engagement, defense in
  depth, resilience); p. 4-1 (integration with the supported force's plan); pp. 6-2, 6-3
  (positioning M-SHORAD/Avenger relative to the supported manoeuvre force).
  https://armypubs.army.mil/epubs/DR_pubs/DR_a/ARN35838-FM_3-01.44-000-WEB-1.pdf
- ADP 3-90 and ADP 3-0 are cited only as FM 3-90 and FM 1-02.1 cite them (screen, guard,
  cover, area security, battle position, objective, axis of advance, defensive operation).

Symbology (downloaded and read this pass):

- **MIL-STD-2525C, *Common Warfighting Symbology*, 17 November 2008.** Appendix B,
  Table B-IV (Military operations tactical graphics): TACGRP.TSK.* (task symbols),
  TACGRP.C2GM.OFF.ARS.OBJ / .ATKPSN / .AFP, TACGRP.C2GM.DEF.ARS.BTLPSN,
  TACGRP.C2GM.GNL.ARS.ABYARA, TACGRP.C2GM.GNL.LNE.PHELNE / .BNDS,
  TACGRP.C2GM.OFF.LNE.AXSADV.GRD.SUPATK, TACGRP.C2GM.SPL.ARA.TAI,
  TACGRP.C2GM.GNL.PNT.REFPNT.PNTINR / .ACTPNT.DCNPNT, TACGRP.C2GM.DEF.PNT.OBSPST,
  TACGRP.MOBSU.OBST.GNL / .OBSEFT / .OBSTBP.CSGSTE.LANE, TACGRP.FSUPP.ARS.ARATGT,
  TACGRP.FSUPP.LNE.LNRTGT.LSTGT, TACGRP.CSS.LNE.SLPRUT, TACGRP.CSS.ARA.SUPARS.DSA.
  https://worldwind.arc.nasa.gov/milstd2525c/Mil-STD-2525C.pdf
- NATO APP-6 (*Military Symbols for Land Based Systems*, May 2011) is the normative
  symbology reference cited by SISO-STD-019-2020 ref [10]; the 2525C codes above are the
  ones actually present in our data (`APP6C-SIDC` elements).

Standards (downloaded and read this pass):

- **SISO-STD-019-2020, *Standard for Command and Control Systems - Simulation Systems
  Interoperation (C2SIM)*.** Sec 2.2 references [10] NATO APP-6 and [15] MIP JC3IEDM.
  Annex B (normative) is the ontology-to-XSD transformation. **No task-code semantics.**
  https://cdn.ymaws.com/www.sisostandards.org/resource/resmgr/standards_products/siso-std-019-2020_c2sim.pdf
- **SISO-STD-020-2020, *Standard for Land Operations Extension to C2SIM (LOX)*.** Same:
  references MIP JC3IEDM domain values; no task-code semantics.
  https://cdn.ymaws.com/www.sisostandards.org/resource/resmgr/standards_products/siso-std-020-2020_lox-c2sim.pdf
- MIP JC3IEDM domain values (www.mip-interop.org) - the normative home of the
  `ActionTaskActivityCode` values. **Not publicly retrievable in this pass**; the codes'
  meanings were taken from the doctrine and symbology they encode, which is the same
  chain SISO-STD-019 points at.

Repository evidence (read-only, this pass):

- `REPO\data\COA-STP1_Order.xml` - 42 tasks re-parsed: verb census, Location counts,
  zero-geometry set {T8, T9, T10, T16, T21, T24, T34, T37, T38}, absence of `Duration`
  and `EndTime` on all 42, exact-coordinate match of every located task against the init.
- `REPO\data\COA-STP1_Initialization.xml` - 409 MapGraphics re-parsed: full `APP6C-SIDC`
  census by graphic kind (the table in section 0), taskee-UUID-to-unit-name join.
- `SDK\C2SIMSDK\C2SIM_SMX_LOX_CWIX2024.cs:4026-4194, 4295-5655` - `ManeuverWarfareTaskType`
  fields; `TaskActionCodeType`, 453 members, all with empty doc comments.
- `STP\HEAD\BridgingAgents\C2SimBridge\C2SimBridge\C2SimXmlBuilder.cs` (lines ~383, 440-545,
  1113-1160), `C2SimBridgeAgentParams.cs:128`,
  `STP\STP\NallSuite\Agents\C2SimBridge\C2SimTask.cs`, `C2SimTg.cs:35-39`,
  `C2SimUtil.cs:109-153`.
