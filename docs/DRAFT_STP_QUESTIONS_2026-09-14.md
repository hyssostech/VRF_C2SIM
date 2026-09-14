# DRAFT - five questions for the STP side about what STP renders from our reports

Status: DRAFT for the user's review; NOT sent. Outward-facing (SPEND gate: the user sends it or not).
Written for: the STP owner / integrator. Origin: docs/experiments/REPORTING_ASSESSMENT_2026-09-14.md sec 4
(the record says what we send, never what STP shows) and build item B10.

---

Subject: VR-Forces interface -> STP: five questions about report rendering

We are finishing the VR-Forces side of the C2SIM demo. The interface already sends three kinds of
C2SIM 1.0.2 reports (samples below, taken verbatim from the bus). Before we add more, we would like to
know what STP actually renders, so we build what you can see:

1. Position reports. We send one PositionReportContent per unit every 10 s with time, latitude and
   longitude and the SubjectEntity uuid. Does STP use HeadingAngle and Speed if we add them? Does it
   use EntityHealthStatus? Does it prefer one report per unit or several PositionReportContent blocks
   bundled in one ReportBody?

2. Task status. We send TaskStatus with TASKCMPLT when a task completes. We are adding TASKSTRT at
   dispatch and TASKABRT when the simulation refuses a task or a unit stops making progress. Does STP
   show TASKSTRT / TASKINPRG / TASKABRT distinctly (icon, colour, text), or only completion?

3. Observations. We send ObservationReportContent / NameObservation at start-up (a text marking that
   says which VR-Forces model stands in for a unit). Does STP display NameObservation anywhere? If so,
   where (unit tooltip, log panel, map)?

4. Map graphics. For a route leg our pre-flight predicts a vehicle cannot climb, we can send an
   ObservationReportContent carrying a LocationObservation (a point) plus a NameObservation (the text).
   Does STP plot a LocationObservation on the map? If STP prefers one carrier for "a point with a
   message", which element should we use?

5. Identity fields. Every report carries FromSender and ToReceiver as the zero uuid (inherited from
   the earlier interface). Does STP need real values there, and if so which?

Sample bodies (as captured on the bus, 2026-09-07 and 2026-09-14):

PositionReportContent:
<ReportBody xmlns="http://www.sisostds.org/schemas/C2SIM/1.1">
  <FromSender>00000000-0000-0000-0000-000000000000</FromSender>
  <ToReceiver>00000000-0000-0000-0000-000000000000</ToReceiver>
  <ReportContent>
    <PositionReportContent>
      <TimeOfObservation><DateTime><IsoDateTime>2026-09-14T00:30:24Z</IsoDateTime></DateTime></TimeOfObservation>
      <Location><GeodeticCoordinate><Latitude>34.66...</Latitude><Longitude>-116.73...</Longitude></GeodeticCoordinate></Location>
      <SubjectEntity>(unit uuid)</SubjectEntity>
    </PositionReportContent>
  </ReportContent>
  <ReportID>(fresh guid)</ReportID>
  <ReportingEntity>(unit uuid)</ReportingEntity>
</ReportBody>

TaskStatus:
<ReportBody xmlns="http://www.sisostds.org/schemas/C2SIM/1.1">
  <FromSender>00000000-0000-0000-0000-000000000000</FromSender>
  <ToReceiver>00000000-0000-0000-0000-000000000000</ToReceiver>
  <ReportContent>
    <TaskStatus>
      <TimeOfObservation><DateTime><IsoDateTime>2026-09-07T15:36:05Z</IsoDateTime></DateTime></TimeOfObservation>
      <CurrentTask>468c0325-99b9-4f97-afc6-39fe301e0c55</CurrentTask>
      <TaskStatusCode>TASKCMPLT</TaskStatusCode>
    </TaskStatus>
  </ReportContent>
  <ReportID>11ecd20a-dd32-4442-8ade-efefc42f1cb0</ReportID>
  <ReportingEntity>de16a337-b2a6-c029-07b5-869191631621</ReportingEntity>
</ReportBody>

ObservationReportContent / NameObservation:
<ReportBody xmlns="http://www.sisostds.org/schemas/C2SIM/1.1">
  <FromSender>00000000-0000-0000-0000-000000000000</FromSender>
  <ToReceiver>00000000-0000-0000-0000-000000000000</ToReceiver>
  <ReportContent>
    <ObservationReportContent>
      <TimeOfObservation><DateTime><IsoDateTime>2026-09-14T00:30:05Z</IsoDateTime></DateTime></TimeOfObservation>
      <Observation>
        <NameObservation>
          <ActorReference>040b2b3e-ef7c-115f-9377-6a6e9745641d</ActorReference>
          <Marking>7913/HQ_71~PXY [Proxy: Tank Headquarters Section (RUS) - rocket battalion CP: HQ-Section substitution.]</Marking>
          <Name>7913/HQ_71</Name>
        </NameObservation>
      </Observation>
    </ObservationReportContent>
  </ReportContent>
  <ReportID>bed19094-e8ac-45c3-a275-f61b7a6ecce9</ReportID>
  <ReportingEntity>040b2b3e-ef7c-115f-9377-6a6e9745641d</ReportingEntity>
</ReportBody>

Full verbatim samples: scratchpad stpq\{position_report,name_observation,task_status}.xml (session)
and runs\20260914T002716Z_run\reports-captured.log / runs\20260907T150643Z_run\reports-captured.log.
