"""applog_chain.py - the MOVE TO PER VERTEX lines of the app log, for the offline analysis tools.

RL-20260927-01 (docs/PLAN_MOVEMENT_2026-09-27.md M1, M1b). Since 2026-09-27 a LONE ground platform is
driven as one Move To per route vertex (VrfC2SimService.StartVertexChain / ConsumeVertexChainCompletion).
Two things the older tools assumed are no longer true for such a unit:
  - its dispatch logs NO "CreateRoute ... for <unit>" / "Route '<r>' ... created; MoveAlongRoute issued
    for VRF_UUID:<u>" pair, so a tool that attributes a taskee through route lines misses it;
  - each vertex logs its own "VRF task complete: <unit> / move-to", so a tool that took a unit's FIRST
    completion took vertex 1's, and one that counted completions counted every vertex.
This module is the ONE parser for the lines the chain DOES log (formats verbatim from the service):

  Task '<task>': MOVE TO PER VERTEX for <unit> (VRF_UUID:<u>) - vertex 1 of <n>: MoveToLocation (...)
  VERTEX CHAIN <unit> task '<task>': vertex <k> of <n> COMPLETED - ...; issuing vertex <k+1> (...)
  VERTEX CHAIN <unit> task '<task>': vertex <k> of <n> reported COMPLETE, but ... - a VACUOUS completion ...
  VERTEX CHAIN <unit> task '<task>': LAST vertex <n> of <n> COMPLETED - ...
  VERTEX CHAIN <unit> task '<task>': LAST vertex <n> of <n> reported COMPLETE, but ... - VACUOUS ...
  VERTEX CHAIN <unit> task '<task>': vertex <k> of <n> FAILED (...)
  VERTEX CHAIN <unit> task '<task>': vertex <k> of <n> issued - MoveToLocation (...)
  VERTEX CHAIN <unit> task '<task>': vertex <k> of <n> is NOT issued - the VR-Forces object ...
  VERTEX CHAIN <unit> task '<task>': a VR-Forces completion (...) arrived while ... SWALLOWED ...
  VERTEX CHAIN <unit> task '<task>': a VR-Forces completion (...) arrived for a chain FROZEN ...

THE COMPLETION RULE. The service logs "VRF task complete: <unit> / <type>" FIRST and the VERTEX CHAIN
line about that same completion right after it (OnVrfTaskCompleted), so a tool that counts completions
un-counts the one before an event in CONSUMED: an intermediate vertex, a vacuous last vertex (withheld
from the completion rules), a stray and a frozen-chain completion are not the TASK's completion. Only
'last' (the LAST vertex COMPLETED) and 'failed' (success=false) are passed on to the task's completion
path. All files the callers read use encoding='utf-8' (errors replaced).

usage:  python tools/analysis/applog_chain.py --selftest
"""
import re
import sys

RE_START = re.compile(r"Task '(?P<task>[^']*)': MOVE TO PER VERTEX for (?P<unit>.+?) "
                      r"\(VRF_UUID:(?P<hex>[0-9a-fA-F-]{36})\) - vertex 1 of (?P<n>\d+):")
RE_EVENT = re.compile(r"VERTEX CHAIN (?P<unit>.+?) task '(?P<task>[^']*)': (?P<body>.+)$")
RE_VRFDONE = re.compile(r"VRF task complete: (?P<unit>.+?) / (?P<type>\S+)")

# (kind, pattern on the event body). ORDER MATTERS: 'LAST vertex' before 'vertex'.
_BODY = [
    ('last', re.compile(r'^LAST vertex (?P<k>\d+) of (?P<n>\d+) COMPLETED')),
    ('last_vacuous', re.compile(r'^LAST vertex (?P<k>\d+) of (?P<n>\d+) reported COMPLETE, but')),
    ('advance', re.compile(r'^vertex (?P<k>\d+) of (?P<n>\d+) COMPLETED')),
    ('vacuous', re.compile(r'^vertex (?P<k>\d+) of (?P<n>\d+) reported COMPLETE, but')),
    ('failed', re.compile(r'^vertex (?P<k>\d+) of (?P<n>\d+) FAILED')),
    ('not_issued', re.compile(r'^vertex (?P<k>\d+) of (?P<n>\d+) is NOT issued')),
    ('issued', re.compile(r'^vertex (?P<k>\d+) of (?P<n>\d+) issued')),
    ('retired', re.compile(r'^a VR-Forces completion .*? arrived for a chain FROZEN.*? at vertex (?P<k>\d+) of (?P<n>\d+)')),
    ('stray', re.compile(r'^a VR-Forces completion .*? arrived while .*? at vertex (?P<k>\d+) of (?P<n>\d+)')),
]

# A vendor completion followed by one of these was NOT passed on as the task's completion.
CONSUMED = frozenset({'advance', 'vacuous', 'last_vacuous', 'stray', 'retired'})
# ... and one of these WAS (the move's completion, or its failure).
PASSED = frozenset({'last', 'failed'})


def chain_start(line):
    """-> {'task', 'unit', 'uuid' ('VRF_UUID:<hex>'), 'hex', 'n'} for a chain's dispatch line, else None."""
    m = RE_START.search(line)
    if not m:
        return None
    return {'task': m.group('task'), 'unit': m.group('unit').strip(), 'hex': m.group('hex').lower(),
            'uuid': 'VRF_UUID:' + m.group('hex').lower(), 'n': int(m.group('n'))}


def chain_event(line):
    """-> {'unit', 'task', 'kind', 'k', 'n'} for a VERTEX CHAIN line, else None."""
    m = RE_EVENT.search(line)
    if not m:
        return None
    body = m.group('body')
    for kind, rx in _BODY:
        b = rx.search(body)
        if b:
            return {'unit': m.group('unit').strip(), 'task': m.group('task'), 'kind': kind,
                    'k': int(b.group('k')), 'n': int(b.group('n'))}
    return None


def task_completions(lines):
    """Count the TASK-level vendor completions per unit, the way the service passes them on:
    {unit: [type, ...]}. A 'VRF task complete' line counts; an immediately following chain event in
    CONSUMED un-counts it; a 'last' event re-labels it 'move-to (LAST vertex k of n)'."""
    done = {}
    for raw in lines:
        line = raw.rstrip('\r\n')
        m = RE_VRFDONE.search(line)
        if m:
            done.setdefault(m.group('unit').strip(), []).append(m.group('type'))
            continue
        ev = chain_event(line)
        if not ev:
            continue
        got = done.get(ev['unit'])
        if ev['kind'] in CONSUMED and got:
            got.pop()
        elif ev['kind'] == 'last' and got:
            got[-1] = '%s (LAST vertex %d of %d)' % (got[-1], ev['k'], ev['n'])
    return {u: t for u, t in done.items() if t}


def starts(lines):
    """Every chain dispatch line, in order."""
    out = []
    for raw in lines:
        s = chain_start(raw)
        if s:
            out.append(s)
    return out


# ------------------------------------------------------------------------------------------ selftest
_FIXTURE = """\
info: VrfC2Sim[0]
      Task 'T14': MOVE TO PER VERTEX for 48_IBCT/28ID (VRF_UUID:0a1b2c3d-0000-1111-2222-333344445555) - vertex 1 of 3: MoveToLocation (54.100000,23.100000); the other 2 vertex(es) are issued one at a time, each when the previous Move To COMPLETES. No route object is created (RL-20260927-01: ...).
info: VrfC2Sim[0]
      Task 'T10': CreateRoute 'T10 ROUTE' (4 pts) for 1-112_IN; move deferred to route-created.
info: VrfC2Sim[0]
      VRF task complete: 48_IBCT/28ID / move-to (success=True)
info: VrfC2Sim[0]
      VERTEX CHAIN 48_IBCT/28ID task 'T14': vertex 1 of 3 COMPLETED - the unit is 4 m from it and moved 1112 m since dispatch; issuing vertex 2 (RL-20260927-01).
info: VrfC2Sim[0]
      VERTEX CHAIN 48_IBCT/28ID task 'T14': vertex 2 of 3 issued - MoveToLocation (54.110000,23.100000) (RL-20260927-01).
info: VrfC2Sim[0]
      VRF task complete: 48_IBCT/28ID / move-to (success=True)
warn: VrfC2Sim[0]
      VERTEX CHAIN 48_IBCT/28ID task 'T14': vertex 2 of 3 reported COMPLETE, but the unit is 900 m from it and moved 3 m since vertex 1 - a VACUOUS completion (farther than Vrf:VertexArrivalRadiusMeters=100 m from the vertex; R11, docs/UNIT_MOVEMENT_RESEARCH.md :394-412). The chain CONTINUES to vertex 3; ...
info: VrfC2Sim[0]
      VRF task complete: 1-112_IN / move-along (success=True)
info: VrfC2Sim[0]
      VRF task complete: 48_IBCT/28ID / move-to (success=True)
info: VrfC2Sim[0]
      VERTEX CHAIN 48_IBCT/28ID task 'T14': LAST vertex 3 of 3 COMPLETED - the unit is 6 m from it and moved 1100 m since vertex 2. The chain ends and this completion goes to the task's own completion rules ... (RL-20260927-01).
info: VrfC2Sim[0]
      Task 'T02': MOVE TO PER VERTEX for 28ID (VRF_UUID:9f8e7d6c-aaaa-bbbb-cccc-ddddeeeeffff) - vertex 1 of 1: MoveToLocation (54.200000,23.200000); the other 0 vertex(es) ...
info: VrfC2Sim[0]
      VRF task complete: 28ID / move-along (success=True)
warn: VrfC2Sim[0]
      VERTEX CHAIN 28ID task 'T02': a VR-Forces completion ('move-along', success=True) arrived while a Move To is outstanding and this report is of another task type - it is not this chain's Move To and is SWALLOWED; the chain is unchanged at vertex 1 of 1 (RL-20260927-01).
info: VrfC2Sim[0]
      VRF task complete: 28ID / move-to (success=True)
warn: VrfC2Sim[0]
      VERTEX CHAIN 28ID task 'T02': LAST vertex 1 of 1 reported COMPLETE, but the unit is 2400 m from it and moved 2 m since dispatch - VACUOUS (farther than Vrf:VertexArrivalRadiusMeters=100 m; R11). It is NOT taken as the task's arrival ...
"""


def selftest():
    failures = 0

    def check(ok, label):
        nonlocal failures
        print('  [%s] %s' % ('PASS' if ok else 'FAIL', label))
        if not ok:
            failures += 1

    lines = _FIXTURE.splitlines()
    st = starts(lines)
    check(len(st) == 2 and st[0]['unit'] == '48_IBCT/28ID' and st[0]['task'] == 'T14' and st[0]['n'] == 3
          and st[0]['uuid'] == 'VRF_UUID:0a1b2c3d-0000-1111-2222-333344445555'
          and st[0]['hex'] == '0a1b2c3d-0000-1111-2222-333344445555',
          'the chain dispatch line maps unit -> VRF uuid with NO route line (a unit name containing "/")')
    check(st[1]['unit'] == '28ID' and st[1]['n'] == 1, 'a two-point route is a chain of 1 vertex')
    kinds = [e['kind'] for e in (chain_event(l) for l in lines) if e]
    check(kinds == ['advance', 'issued', 'vacuous', 'last', 'stray', 'last_vacuous'],
          'every VERTEX CHAIN line is classified: %s' % kinds)
    check(chain_event("      VERTEX CHAIN 48_IBCT/28ID: vertex 2 is NOT issued - the chain was ended") is None,
          'the task-less "is NOT issued" line (a chain ended before its turn) is not an event about a completion')
    check(chain_event("VERTEX CHAIN U task 'T': vertex 2 of 3 FAILED (VR-Forces success=false ...)")['kind'] == 'failed'
          and chain_event("VERTEX CHAIN U task 'T': vertex 2 of 3 is NOT issued - the VR-Forces object ...")['kind'] == 'not_issued'
          and chain_event("VERTEX CHAIN U task 'T': a VR-Forces completion ('move-to', success=True) arrived for a "
                          "chain FROZEN by the back-end loss, at vertex 2 of 3 - x.")['kind'] == 'retired',
          'FAILED, NOT issued and a frozen-chain completion are classified')
    done = task_completions(lines)
    check(done.get('48_IBCT/28ID') == ['move-to (LAST vertex 3 of 3)'],
          'THE COMPLETION RULE: three move-to completions of a 3-vertex chain are ONE task completion, the LAST '
          "vertex's (got %s)" % done.get('48_IBCT/28ID'))
    check('28ID' not in done,
          'a VACUOUS last vertex is withheld and a stray is swallowed: no task completion for 28ID')
    check(done.get('1-112_IN') == ['move-along'], 'a unit on Move Along Route is counted as before')
    first_only = {}
    for l in lines:
        m = RE_VRFDONE.search(l)
        if m:
            first_only.setdefault(m.group('unit').strip(), []).append(m.group('type'))
    check(len(first_only.get('48_IBCT/28ID', [])) == 3 and len(first_only.get('28ID', [])) == 2,
          'FAIL-FIRST: counting "VRF task complete" lines alone gives the chained platform 3 completions, the first '
          "of them vertex 1's, and the vacuous one 2")
    failures += _tool_selftests(check)
    print('ALL CHECKS PASSED' if failures == 0 else '%d CHECK(S) FAILED' % failures)
    return 0 if failures == 0 else 1


def _tool_selftests(check):
    """THE TOOLS, WIRED: each tool that reads the app log is run on the fixture above (a temp run dir), so a
    tool that stops calling this module fails here. Returns 0 - the checks count through `check`."""
    import importlib
    import os
    import tempfile
    sys.dont_write_bytecode = True   # no __pycache__ left in the tracked tools\analysis directory
    here = os.path.dirname(os.path.abspath(__file__))
    if here not in sys.path:
        sys.path.insert(0, here)
    tmp = tempfile.mkdtemp(prefix='applog_chain_')
    log = os.path.join(tmp, 'vrfc2simapp.log')
    with open(log, 'w', encoding='utf-8', newline='\n') as f:
        f.write(_FIXTURE)
    try:
        mc = importlib.import_module('movement_check')
        app = mc.read_app_log(log)
        chained = app['completed'].get('48_IBCT/28ID') or []
        check(len(chained) == 1 and 'LAST vertex 3 of 3' in chained[0]
              and '28ID' not in app['completed'] and len(app['completed'].get('1-112_IN') or []) == 1,
              'movement_check: a chained platform COMPLETES once, on its LAST vertex; a vacuous last vertex is '
              'not a completion (got %s)' % app['completed'])
        sr = importlib.import_module('stall_replay')
        _, unit_of_agg, _, tasked, completed = sr.read_app_log(tmp)
        check(('T14', '0a1b2c3d-0000-1111-2222-333344445555') in tasked
              and unit_of_agg.get('0a1b2c3d-0000-1111-2222-333344445555') == '48_IBCT/28ID'
              and '48_IBCT/28ID' in completed and '28ID' not in completed,
              'stall_replay: the chained platform is a tasked MOVE (task, own uuid) with its unit name, and only '
              'its LAST vertex marks it complete')
        cn = importlib.import_module('console_narrative')
        names = cn.load_names(tmp)
        check(names.get('VRF_UUID:0a1b2c3d-0000-1111-2222-333344445555') == '48_IBCT/28ID',
              'console_narrative: the chained platform uuid is named from its MOVE TO PER VERTEX line')
        td = importlib.import_module('taskee_displacement')
        issued = td.tasked_moves(_FIXTURE)
        check(('T14', 'VRF_UUID:0a1b2c3d-0000-1111-2222-333344445555') in [(t, u) for t, u, _ in issued]
              and any(k == 'chain' for _, _, k in issued),
              'taskee_displacement: the chained platform is a tasked unit (task, VRF uuid, chain)')
        st = importlib.import_module('straggler_track')
        dests, cmplt = st.read_moves_and_completions(_FIXTURE)
        check('T14' in dests and '48_IBCT/28ID' in cmplt and '28ID' not in cmplt,
              'straggler_track: the chained task is a dispatched move and only its LAST vertex completes the unit')
    finally:
        try:
            os.remove(log)
            os.rmdir(tmp)
        except OSError:
            pass
    return 0


if __name__ == '__main__':
    if len(sys.argv) > 1 and sys.argv[1] == '--selftest':
        sys.exit(selftest())
    print(__doc__)
    sys.exit(2)
