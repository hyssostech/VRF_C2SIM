# Offline check of our three report bodies against STP's own parser (Opus, 2026-09-14 ~13:10Z; B10 part 1, STP-787).
# Result: all three parse on SDK 1.3.0 / 1.3.1 / 1.4.0; the C++ oracle's empty OperationalStatusCode fails on 1.3.x
# exactly as STP-615 says (the harness reproduces the defect); STP's ParseReportContent then DISCARDS everything but
# PositionReportContent (NameObservation and TaskStatus never reach STP today); bundle shape A (repeated ReportContent,
# our bundler) is accepted, shape B (several PositionReportContent in one ReportContent) silently drops all but the first.
# 2026-09-14 ~14:40Z GATE G-B (sec 3.6): the B7 shape (HeadingAngle + Speed, from --report-selftest @02b51de) PARSES on
# all three pins and both values are DELIVERED; a non-numeric HeadingAngle FAILS on all three, so the verdict is not
# vacuous. No element order or name changed. STP still discards the two values (TODO at C2SimXmlBuilder.cs:720).

# STP_PARSE_CHECK - do our three C2SIM report shapes survive STP's parser?

Reporting item B10 part 1 (Jira STP-787). Offline, no simulation, no repo modified.
Date: 2026-09-14. Harness: C:\Users\PAULOB~1\Temp\claude\c--Users-PauloBarthelmess-Source-Repos-C2SIM-c2simVRFinterfacev2-36\a7f6a276-7ebc-4507-ac9d-c6bd361bd64e\scratchpad\stpparse\

## 0. Verdict in one line

All three shapes we emit (position report, name observation, task status) PARSE cleanly
on every SDK version STP pins (1.3.0, 1.3.1, 1.4.0). The C++ oracle's empty
`<OperationalStatusCode></OperationalStatusCode>` FAILS on 1.3.0/1.3.1 exactly as STP-615
describes, which proves the harness reproduces the defect rather than merely agreeing with
us. Bundling by repeated `<ReportContent>` (the shape our P4b bundler builds) is accepted
and all members are delivered; bundling by repeated `<PositionReportContent>` inside ONE
`<ReportContent>` parses without error but SILENTLY DROPS all but the first.

## 1. What STP actually runs (verified from source, not assumed)

Release line - C:\Users\PauloBarthelmess\Source\Repos\STP\STP-release\NallSuite\Agents\C2SimBridge\

  - C2SimBridgeAgent.csproj    : `<TargetFrameworks>net10.0</TargetFrameworks>`,
                                 `<PackageReference Include="HyssosTech.Sdk.C2SIM" Version="1.3.1" />`
  - C2SimXmlBuilder.cs:642     : `var report = C2SIMSDK.ToC2SIMObject<ReportBodyType>(reportBody);`
  - C2SimXmlBuilder.cs:9       : `using C2SIM.Schema102;`
    -> the generic argument resolves to **C2SIM.Schema102.ReportBodyType** (the CWIX2024 /
       1.0.2 generated schema), NOT Schema100/Schema101. Confirmed at runtime by the harness
       printing `ReportBodyType : C2SIM.Schema102.ReportBodyType`.
  - There is exactly ONE `ToC2SIMObject` call site in the whole bridge (grep across the
    C2SimBridge directory).
  - C2SimBridgeAgent.cs:822-827: `var report = C2SimXmlBuilder.ParseReportContent(e.Body);`
    and on null -> `CoreTrace.WriteLine($"Could not parse report: {e.Body}")` + return.
    C2SimXmlBuilder.cs:729 catches every exception from the parse+extract block and returns
    null, so ANY deserialization failure silently drops the whole report.

Same pin on STP-release-stp6.0, STP-5.11, STP-net10 (all net10.0 + 1.3.1).
Older STP\HEAD (net6.0-windows10.0.17763.0) pins 1.3.0 - tested too.

Input shape: the SDK hands STP a BARE `<ReportBody>` element, not a wrapped MessageBody.
  C2SIMSSDK.cs:661-672 - `case "DomainMessageBody": bodyElement = bodyElement.FirstNode as
  XElement; ... case "ReportBody": OnReportReceived(new C2SIMNotificationEventParams(header,
  bodyElement.ToString()));`
So the verbatim bus captures used here (root `<ReportBody xmlns="http://www.sisostds.org/schemas/C2SIM/1.1">`)
are the correct harness input; the `XmlRootAttribute("ReportBody", ...)` on ReportBodyType
matches that root.

## 2. SDK versions located and tested

NuGet cache C:\Users\PauloBarthelmess\.nuget\packages\hyssostech.sdk.c2sim\ contained 1.3.1
and 1.4.0. 1.3.0 was restored successfully as well (feed reachable), so all three pins in
use across STP branches were covered. Runtime identity printed by the harness:

  | Version | AssemblyInformationalVersion                     | SanitizeInboundXml present |
  |---------|--------------------------------------------------|----------------------------|
  | 1.3.0   | 1.3.0+0bce7e37afd8c4425794ee13ae9f61f03f474b00   | False                      |
  | 1.3.1   | 1.3.1+da6a3c885db376aaf4e76800c2b72906c3380acc   | False                      |
  | 1.4.0   | 1.4.0+6506e59653ed3e161f0086b0c2fbab78d393b013   | True                       |

Local SDK source, C:\Users\PauloBarthelmess\Source\Repos\C2SIM\OpenC2SIM.github.io\Software\Library\CS\C2SIMSDK
  - C2SIMSDK\C2SIMSDK.csproj declares `<Version>1.4.0</Version>` (twice: PropertyGroup and
    PackageId block); C2SIMClientLib declares 4.8.3.3; TargetFrameworks net10.0;netstandard2.0.
  - CAUTION: the working tree is on branch `dev/sdk-fixes` and does NOT contain
    SanitizeInboundXml. Its `ToC2SIMObject<T>` is the plain
    `new XmlSerializer(typeof(T)) ... serializer.Deserialize(reader)` with no pre-pass.
    `git merge-base --is-ancestor 6506e59 HEAD` -> NOT an ancestor. The shipped 1.4.0 is
    commit 6506e59 ("Make empty-element strip cascading (fixpoint) so unspecified != default",
    on master and release/sdk-1.4.0), whose ToC2SIMObject reads
    `using (TextReader reader = new StringReader(SanitizeInboundXml(xml)))`.
    So: do not read the checked-out source as "what 1.4.0 does" - it declares 1.4.0 but is
    behind the release. The harness measured the packaged assemblies, not this source.

Harness: two-file throwaway console app (Program.cs shared by three csproj, each with one
PackageReference), TargetFramework net10.0 (same as the STP release branch), Release build,
approx 1-4 s per build. It calls the same generic on the same type, then replicates STP's
own extraction loop (PositionReportContentType only, AltitudeAGL-then-MSL, the
EntityHealthStatus-null raw-XML fallback) so the printed values are what STP would see.

## 3. Results

  | Case                             | 1.3.0  | 1.3.1  | 1.4.0  |
  |----------------------------------|--------|--------|--------|
  | position_report.xml              | PARSED | PARSED | PARSED |
  | name_observation.xml             | PARSED | PARSED | PARSED |
  | task_status.xml                  | PARSED | PARSED | PARSED |
  | control_empty_opstatus.xml       | FAILED | FAILED | PARSED |
  | bundle_multi_reportcontent.xml   | PARSED | PARSED | PARSED |
  | bundle_multi_positioncontent.xml | PARSED*| PARSED*| PARSED*|

  (*) parses with no error but only the FIRST of three positions survives - see 3.5.

Identical behavior on 1.3.0 and 1.3.1 in every case.

GATE G-B, 2026-09-14 ~14:40Z: sec 3.6 adds the B7 shape (HeadingAngle + Speed) built by the
integration build 02b51de, plus two negative controls. Result: PARSED on all three, both values
delivered. (The supervisor's brief called this "sec 3.4"; 3.4 and 3.5 were already taken by the
13:10Z pass, so it is appended as 3.6 rather than renumbering the existing record.)

### 3.1 position_report.xml - PARSED on all three

Values STP extracts:
  ReportID        6ec5cccf-4132-476f-b347-55130d33f05f
  ReportingEntity 7dd0334e-91c7-3a53-9869-efdd8ee6d5e4
  ReportContent[] 1, Item = PositionReportContentType
  SubjectEntity   7dd0334e-91c7-3a53-9869-efdd8ee6d5e4   (this is the uuid STP correlates on)
  Lat/Lon         34.21864268746047 , -116.38509114461914 (round-trips exactly)
  Altitude        0.0  - we send neither AltitudeAGL nor AltitudeMSL, so STP's
                  `if (loc.AltitudeAGLSpecified) ... else if (loc.AltitudeMSLSpecified)`
                  both miss and altitude stays at its initializer 0.0.
  Health          EntityHealthStatus is null (we omit the element). STP then re-parses the
                  RAW body looking for OperationalStatusCode / StrengthPercentage; both
                  absent, so status stays `Status.fully_capable` and strength "100".
  STP-visible positions returned: 1

### 3.2 name_observation.xml - PARSED on all three

  ReportContent[] 1, Item = ObservationReportContentType, Observation[] 1,
  Observation.Item = NameObservationType
    ActorReference 040b2b3e-ef7c-115f-9377-6a6e9745641d
    Name           7913/HQ_71
    Marking        7913/HQ_71~PXY [Proxy: Tank Headquarters Section (RUS) - rocket battalion CP: HQ-Section substitution.]
  The long free-text Marking (brackets, tilde, parentheses, colon) survives intact.

  FINDING (not a parse failure, but decisive for B10): STP's ParseReportContent handles ONLY
  `content.Item is PositionReportContentType`. An ObservationReportContentType falls through
  the if, the method returns an EMPTY list, and C2SimBridgeAgent's `foreach (var item in
  report)` iterates zero times. The report is accepted and then discarded - STP has no
  consumer for name observations today. Surviving the parser is necessary, not sufficient.
  STP-visible positions returned: 0

### 3.3 task_status.xml - PARSED on all three

  ReportContent[] 1, Item = TaskStatusType
    CurrentTask    468c0325-99b9-4f97-afc6-39fe301e0c55
    TaskStatusCode TASKCMPLT   (valid member of Schema102 TaskStatusCodeType)
  Full enum, for reference - only these five strings deserialize:
    TASKABRT TASKCMPLT TASKINPRG TASKPEND TASKSTRT
  Our interface emits only TASKCMPLT (ReportBuilder.cs:50, typed, not a string), so no risk
  of an out-of-enum code from this path.

  Same FINDING as 3.2: TaskStatusType is parsed and then discarded by ParseReportContent.
  STP-visible positions returned: 0

### 3.4 CONTROL - control_empty_opstatus.xml (the C++ oracle's shape)

A position body identical to 3.1 plus the oracle's health block, element order per the
generated schema (Duration, TimeOfObservation, EntityHealthStatus*, HeadingAngle, Location,
Speed, SubjectEntity):

    <EntityHealthStatus>
      <OperationalStatus>
        <OperationalStatusCode></OperationalStatusCode>
      </OperationalStatus>
    </EntityHealthStatus>

1.3.0 and 1.3.1 - FAILED, verbatim:

    System.InvalidOperationException: There is an error in XML document (14, 11).
      inner: System.InvalidOperationException: Instance validation error: '' is not a valid
             value for OperationalStatusCodeType.

  (OperationalStatusCodeType members: FullyOperational, MostlyOperational, NotOperational,
  PartlyOperational - an empty string is not among them.) In STP this exception is caught at
  C2SimXmlBuilder.cs:729, ParseReportContent returns null, and C2SimBridgeAgent logs
  "Could not parse report" and drops it. That is STP-615, reproduced offline.

1.4.0 - PARSED. The sanitizer strips the empty leaf and, running to a fixpoint, the
  now-empty `<OperationalStatus>` and `<EntityHealthStatus>` containers too, so
  `prc.EntityHealthStatus` comes back null. Note the second-order effect: STP's fallback then
  re-scans the RAW (unsanitized) body, finds `OperationalStatusCode` with value "", and
  `Enum.TryParse("")` fails - so status stays fully_capable. The 1.4.0 fix turns a dropped
  report into a delivered position with health "unspecified"; it does not invent a health
  value.

This control is the falsification check on the whole exercise: the same harness that says
our three shapes pass says the oracle's shape fails, with the exact message from the ticket.
It also rules out "the control failed because I wrote the element order wrong" - the same
file parses on 1.4.0, so its structure is schema-valid.

### 3.5 BUNDLES (relevant to B9)

Two shapes are possible and they are NOT equivalent.

Shape A - repeated `<ReportContent>`, one PositionReportContent each (bundle_multi_reportcontent.xml,
3 units): PARSED on 1.3.0 / 1.3.1 / 1.4.0. ReportContent[] = 3, all three SubjectEntity /
lat-lon pairs extracted, STP-visible positions returned: 3. **This is the accepted shape.**
Our P4b bundler already builds exactly this: ReportBuilder.cs:114
`ReportContent = list.Select(f => new S.ReportContentType { Item = new S.PositionReportContentType ... })`
produces an array of ReportContentType, i.e. N sibling `<ReportContent>` elements
(VrfSettings.BundlePositionReports, default false, BundleMaxReports 10).

Shape B - ONE `<ReportContent>` containing several `<PositionReportContent>`
(bundle_multi_positioncontent.xml, 3 units): parses with NO exception on all three versions,
but ReportContent[] = 1 and only the FIRST unit (7dd0334e...) is returned. The other two are
gone with no error, no warning, no log line. Cause: the generated
`ReportContentType` has a single choice member
(C2SIM_SMX_LOX_CWIX2024.cs:10208-10222, `private object itemField;` with three
XmlElementAttribute alternatives), so XmlSerializer binds the first matching child and treats
the siblings as unknown nodes, which are ignored by default. **Never emit Shape B** - it is
silent data loss, strictly worse than a rejected report.

### 3.6 B7 shape (HeadingAngle + Speed) - GATE G-B, 2026-09-14 ~14:40Z

WHY THIS RE-RUN. The 13:10Z pass above predates B7. B7 adds two elements to EVERY position
report - the one report shape STP actually consumes (sec 3.2/3.3: ParseReportContent handles only
PositionReportContentType) - and STP wraps its whole parse+extract in one catch that returns null
and drops the WHOLE report (C2SimXmlBuilder.cs:729). If our fork's generated
PositionReportContentType disagreed with STP's pinned 1.3.1 on these two elements, 100% of position
reports would vanish. Cold-start review of feat/integration 02b51de, gate G-B (MAJOR-3).

BODIES. Produced by THIS build, not hand-written: `VrfC2SimApp --report-selftest` on
feat/integration 02b51de (Release, BridgeConfig=Release-5.2) prints the wire xml of every shape it
round-trips; the two position bodies were lifted verbatim from that output and the `<?xml ...?>`
declaration stripped, because the SDK hands STP a BARE `<ReportBody>` element
(`bodyElement.ToString()`, sec 1).

  bodies\position_b7.xml   single PositionReportContent, HeadingAngle 275.25 + Speed 8.75
                           (ReportSelfTest.cs: headingDeg = 275.25, speedMps = 8.75)
  bodies\bundle_b7.xml     the P4b 3-fix bundle, per-fix kinematics: fix 0 BOTH (12.5 / 3.25),
                           fix 1 NEITHER (a failed read), fix 2 HEADING ONLY (359.9)

Element order as emitted (the schema's own sequence): TimeOfObservation, HeadingAngle, Location,
Speed, SubjectEntity.

RESULTS - verbatim verdicts, `out_b7_<version>.txt`:

  | Case                     | 1.3.0  | 1.3.1  | 1.4.0  |
  |--------------------------|--------|--------|--------|
  | position_b7.xml          | PARSED | PARSED | PARSED |
  | bundle_b7.xml            | PARSED | PARSED | PARSED |
  | ctl_b7_badheading.xml    | FAILED | FAILED | FAILED |
  | ctl_b7_disorder.xml      | PARSED | PARSED | PARSED |

**GATE G-B: PASS on 1.3.0, 1.3.1 and 1.4.0.** Identical on all three. No element order or name was
changed (the brief's stop condition was not reached).

The values are not merely "not fatal" - they are DELIVERED. The harness reads the deserialized
PositionReportContentType by reflection (so it compiles even against an SDK whose generated type
lacks the members) and printed, on every version:

  ---- CASE position_b7.xml ----
    VERDICT: PARSED
      [0] Item = PositionReportContentType
          SubjectEntity = 001aa71b-4c26-a1ea-28b2-f7dfe8e76342
          HeadingAngle = 275.25   (HeadingAngleSpecified = True)
          Speed = 8.75   (SpeedSpecified = True)
          Lat/Lon       = 58.703 , 16.4992

and for the bundle, per fix, exactly the shape that was built - 12.5/3.25 specified, then
False/False, then 359.9 with SpeedSpecified False:

  ---- CASE bundle_b7.xml ----
    VERDICT: PARSED
    ReportContent[] : 3
      [0] HeadingAngle = 12.5  (True)   Speed = 3.25 (True)
      [1] HeadingAngle = 0     (False)  Speed = 0    (False)
      [2] HeadingAngle = 359.9 (True)   Speed = 0    (False)
    STP-visible positions returned: 3

So the OMISSION rule survives the wire too: a fix whose kinematics read failed arrives at STP with
HeadingAngleSpecified/SpeedSpecified FALSE, not as a fabricated 0.

NEGATIVE CONTROLS (why "PARSED" is not vacuous). XmlSerializer IGNORES unknown elements by default,
so a PARSED verdict on its own is equally consistent with "the SDK never heard of these elements and
silently skipped them". Two controls separate the cases:

  - ctl_b7_badheading.xml - the same body with `<HeadingAngle>north</HeadingAngle>`. FAILED on all
    three: `System.InvalidOperationException: There is an error in XML document (12, 8)` ->
    `System.FormatException: The input string 'north' was not in a correct format.` The element is
    therefore genuinely BOUND to a double member of the pinned type, not skipped.
  - ctl_b7_disorder.xml - HeadingAngle and Speed moved OUT of their declared sequence position (after
    SubjectEntity). PARSED with both values still delivered, on all three. So element ORDER is not
    load-bearing for these two on the pinned SDKs - useful to know, but the emitted order is the
    schema's and there is no reason to move it.

REGRESSION. The six original cases were re-run in the same pass and their verdicts are byte-identical
to the 13:10Z table, including the STP-615 control (control_empty_opstatus.xml FAILED on 1.3.0/1.3.1,
PARSED on 1.4.0) - so the harness still reproduces a known defect.

WHAT THIS DOES NOT SETTLE. STP does not USE these two values: `// TODO: Add HeadingAngle, Speed`
sits at C2SimXmlBuilder.cs:720, so today they are parsed and discarded (the same fate as
TaskStatus and Observations - sec 3.2/3.3, and MAJOR-2 of the review). G-B's claim is exactly the
one that mattered: adding them does not cost us the POSITION reports STP does consume. The live
evidence for heading/speed is the BUS capture, not STP behaviour.

## 4. What remains unknown (NOT established by this exercise)

1. uuid -> unit correlation. Offline parsing stops at SubjectEntity. STP then does
   (C2SimBridgeAgent.cs:1332 QueryStpSymbolAsync):
       string symbolPoid = Id.Generate("id", new Guid(itemUuid));
       result = await QueryStpSymbolByPropertyAsync(symbolPoid, "task_org_unit_poid");
       // if null, retry with symbolPoid = "uuid" + itemUuid.ToUpperInvariant()
   i.e. the uuid is re-encoded into an STP poid and matched against the fsdb property
   task_org_unit_poid, with an "uuid<UPPERCASE>" fallback. Whether OUR SubjectEntity uuids
   land on either form is UNTESTED - it needs a live STP (fsdb query) and is the natural
   B10 part 2. On a miss STP logs "Unit matching uuid ... was not found" / "C2SIM report
   received for unknown uuid" and skips the item, so a correlation failure is silent at the
   parse layer and looks identical to "no reports arrived".
2. Rendering. What STP draws once a symbol is found (position move, status glyph) was not
   exercised.
3. The gate. C2SimBridgeAgent.cs:818 returns immediately unless
   `_bridgeParams.UpdateUnitPositions` is true. If that flag is off, every report is dropped
   before the parser is ever reached. Unverified for the demo configuration.
4. Runtime assembly identity on STP's side. This test measured the NuGet packages; it did not
   confirm which C2SIMSDK.dll a deployed STP actually loads.
5. Only ONE name_observation / task_status / position sample each was tested (the captures
   provided). Other markings (very long, or with XML-special characters that our serializer
   would escape) were not swept.
6. Health/altitude enrichment. We currently emit neither EntityHealthStatus nor any Altitude,
   so STP records altitude 0.0 and status fully_capable for every unit. That is a content
   decision, not a parse problem, but it means STP's health/altitude fields carry no
   information from us today.

## 5. Files

  stpparse\Program.cs                         harness (shared by all three projects)
  stpparse\v130\, v131\, v140\                one csproj each, pinned to 1.3.0 / 1.3.1 / 1.4.0
  stpparse\bodies\position_report.xml         verbatim bus capture
  stpparse\bodies\name_observation.xml        verbatim bus capture
  stpparse\bodies\task_status.xml             verbatim bus capture
  stpparse\bodies\control_empty_opstatus.xml  C++ oracle shape (STP-615 control)
  stpparse\bodies\position_b7.xml            sec 3.6 - B7 body from --report-selftest @02b51de
  stpparse\bodies\bundle_b7.xml              sec 3.6 - B7 P4b bundle, per-fix kinematics
  stpparse\bodies\ctl_b7_badheading.xml      sec 3.6 - negative control (non-numeric HeadingAngle)
  stpparse\bodies\ctl_b7_disorder.xml        sec 3.6 - negative control (elements out of sequence)
  stpparse\out_b7_1.3.0.txt / _1.3.1 / _1.4.0 sec 3.6 - verbatim harness output
  stpparse\bodies\bundle_multi_reportcontent.xml     bundle shape A (accepted)
  stpparse\bodies\bundle_multi_positioncontent.xml   bundle shape B (silent loss)
  stpparse\out_1.3.0.txt, out_1.3.1.txt, out_1.4.0.txt   full run output
