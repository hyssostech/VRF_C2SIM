"""Build a small C2SIM Order that exercises the task codes STP #417/#429 export (RL-20261014-03), on units of an
STP-generated Initialization, so a live run shows each mapping even when the scenario's own order carries none of them.

Usage: make_code_probe_order.py <initialization.xml> <out_order.xml> [slot_seconds]
Three in-extent Iron Storm units, two chained tasks each (slot 0, then slot 1 after the first):
  28ID      HoldInPlace -> UNCONW      (both held where the unit is)
  1-112 IN  EVACT       -> HIDE        (move to a point and hold; then held in place)
  48 IBCT   CONSTR      -> ENGAGE      (move to a point + 'construction not simulated'; then advance ~0 m, fire at will)
Duration = slot_seconds (default 3600, STP #413's 60-min slot). Units are found by Name in the Initialization
(exact names below); a missing unit is an error (exit 2). Writes ASCII, LF. Prints one line per task.
"""
import sys
import uuid
import xml.etree.ElementTree as ET

NS = '{http://www.sisostds.org/schemas/C2SIM/1.1}'
UNITS = {'28ID': '28ID', '1-112': '1-112_IN/28ID', '48IBCT': '48_IBCT/28ID'}
# (unit key, code, lat, lon or None for 'no geometry', slot)
TASKS = [
    ('28ID', 'HoldInPlace', 53.9924, 23.2113, 0),
    ('28ID', 'UNCONW', 53.9924, 23.2113, 1),
    ('1-112', 'EVACT', 54.0400, 23.2850, 0),
    ('1-112', 'HIDE', None, None, 1),
    ('48IBCT', 'CONSTR', 54.0403, 23.3364, 0),
    ('48IBCT', 'ENGAGE', 54.0403, 23.3364, 1),
]


def iso_duration(seconds):
    h, rem = divmod(int(seconds), 3600)
    m, s = divmod(rem, 60)
    return 'P00Y00M00DT%02dH%02dM%02dS' % (h, m, s)


def unit_uuids(init_path):
    found = {}
    for ent in ET.parse(init_path).getroot().iter(NS + 'Entity'):
        name = ent.find('.//' + NS + 'Name')
        uid = ent.find('.//' + NS + 'UUID')
        if name is not None and uid is not None:
            found.setdefault(name.text.strip(), uid.text.strip())
    return found


def main(argv):
    if len(argv) < 3:
        print('usage: make_code_probe_order.py <initialization.xml> <out_order.xml> [slot_seconds]')
        return 2
    slot = int(argv[3]) if len(argv) > 3 else 3600
    names = unit_uuids(argv[1])
    missing = [n for n in UNITS.values() if n not in names]
    if missing:
        print('missing unit(s) in the Initialization: ' + ', '.join(missing))
        return 2
    ns = uuid.UUID('7a1b3c5d-0417-4429-9e0f-0123456789ab')
    out = ['<?xml version="1.0" encoding="utf-8"?>',
           '<MessageBody xmlns="http://www.sisostds.org/schemas/C2SIM/1.1">',
           '  <DomainMessageBody>', '    <OrderBody>',
           '      <FromSender>00000000-0000-0001-0001-000000000000</FromSender>',
           '      <ToReceiver>00000000-0007-0001-0000-000000000000</ToReceiver>',
           '      <IssuedTime><IsoDateTime>2026-10-15T06:00:00Z</IsoDateTime></IssuedTime>',
           '      <OrderID>%s</OrderID>' % uuid.uuid5(ns, 'order')]
    prev = {}
    for key, code, lat, lon, slot_no in TASKS:
        tid = str(uuid.uuid5(ns, key + '/' + code))
        perf = names[UNITS[key]]
        out += ['      <Task>', '        <ManeuverWarfareTask>']
        if key in prev:  # xsd order: ActionTemporalRelationship first (as STP writes it)
            out += ['          <ActionTemporalRelationship><ActionTemporalAssociationCode>STREND</ActionTemporalAssociationCode>'
                    '<TemporalAssociationWithAction>%s</TemporalAssociationWithAction></ActionTemporalRelationship>' % prev[key]]
        if lat is not None:
            out += ['          <Location><GeodeticCoordinate><Latitude>%s</Latitude><Longitude>%s</Longitude>'
                    '</GeodeticCoordinate></Location>' % (lat, lon)]
        out += ['          <Name>PROBE_%s_%s</Name>' % (key, code),
                '          <UUID>%s</UUID>' % tid,
                '          <AffectedEntity>%s</AffectedEntity>' % perf,
                '          <DesiredEffectCode>TaskSuccess</DesiredEffectCode>',
                '          <Duration><IsoTimeDuration>%s</IsoTimeDuration></Duration>' % iso_duration(slot),
                '          <PerformingEntity>%s</PerformingEntity>' % perf,
                '          <StartTime><SimulationTime><DelayTimeAmount><IsoTimeDuration>%s</IsoTimeDuration>'
                '</DelayTimeAmount></SimulationTime></StartTime>' % iso_duration(slot * slot_no)]
        # ROEHold on every task, as STP's export writes it (task roe, else the shipped default ROEHold; STP-1140): HIDE
        # must then hold fire, ENGAGE is still fire at will (the unit ATTACK path overrides the order's ROE).
        out += ['          <TaskActionCode>%s</TaskActionCode>' % code,
                '          <RuleOfEngagement><MipWeaponUseROE><WeaponROECode><WeaponRuleOfEngagementCode>ROEHold'
                '</WeaponRuleOfEngagementCode></WeaponROECode></MipWeaponUseROE></RuleOfEngagement>',
                '        </ManeuverWarfareTask>', '      </Task>']
        prev[key] = tid
        print('%-7s %-12s slot %d %s' % (key, code, slot_no, 'no geometry' if lat is None else '%s,%s' % (lat, lon)))
    out += ['    </OrderBody>', '  </DomainMessageBody>', '</MessageBody>', '']
    with open(argv[2], 'w', encoding='utf-8', newline='\n') as f:
        f.write('\n'.join(out))
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv))
