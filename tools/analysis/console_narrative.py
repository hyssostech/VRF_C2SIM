"""console_narrative.py - print a VR-Forces object's OWN account of a run from the WatchVrf trace.

The sim engine narrates what each object's controllers do on the object's console (UG52 21.9;
per-object notify level 0..4). With Vrf:ObjectConsoleNotifyLevel=4 the runner's WatchVrf trace
captures every message as CON,<t>,<uuid>,<level>,<xml> rows (the app log only carries the
objects this controller created). This tool decodes the DtRwTranslatableStringObject XML,
maps uuids to names via the app log, and prints a narrative.

usage:
  python console_narrative.py <run_dir>                      # summary: rows by object/level, warnings
  python console_narrative.py <run_dir> <name-or-uuid-tail>  # that object's task narrative
  python console_narrative.py <run_dir> <name> --all         # every row of that object
Levels > 4 carry a sim-time prefix in the level field (e.g. 4180); they are treated as level 4.
"""
import csv
import html
import re
import sys
import collections
import os

XML_STRING = re.compile(r'<string[^>]*>(.*?)</string>', re.S)
TASK_WORDS = re.compile(r'ask|ormation|eader|ubordinate|oute|complete|rcvd|Failed|Completed|clearing')


NESTED_TAG = re.compile(r'^<string[^>]*>')


def decode(msg):
    # the translatable template is itself wrapped in an escaped <string translate=...> tag;
    # unescape, then drop that inner tag so only the text remains
    s = msg.replace('\\n', '\n')
    parts = [NESTED_TAG.sub('', html.unescape(p).strip()).strip() for p in XML_STRING.findall(s) if p.strip()]
    parts = [p for p in parts if p]
    return ' | '.join(parts) if parts else s.strip()


def load_names(run_dir):
    names = {}
    p = os.path.join(run_dir, 'vrfc2simapp.log')
    if not os.path.exists(p):
        return names
    with open(p, encoding='utf-8', errors='replace') as f:
        for line in f:
            m = re.search(r'VRF console (?:level \d requested for|\[\d+\]) (\S+(?: \d+)?) \((VRF_UUID:[0-9a-f-]+)\)', line)
            if m and m.group(1) != '?':
                names[m.group(2)] = m.group(1)
            m = re.search(r"Route '([^']+)' \((VRF_UUID:[0-9a-f-]+)\) created; MoveAlongRoute issued for (VRF_UUID:[0-9a-f-]+)", line)
            if m:
                names.setdefault(m.group(2), m.group(1))
    return names


def load_rows(run_dir):
    rows = []
    with open(os.path.join(run_dir, 'watchvrf-trace.csv'), encoding='utf-8', errors='replace', newline='') as f:
        for r in csv.reader(f):
            if r and r[0] == 'CON' and len(r) >= 5:
                try:
                    lvl = int(r[3])
                except ValueError:
                    continue
                rows.append((float(r[1]), r[2], min(lvl, 4) if lvl > 4 else lvl, decode(','.join(r[4:]))))
    return rows


def main():
    if len(sys.argv) < 2:
        print(__doc__)
        return 2
    run_dir = sys.argv[1]
    names = load_names(run_dir)
    rows = load_rows(run_dir)
    label = lambda u: names.get(u, u[-12:])
    if len(sys.argv) == 2:
        print(f'{run_dir}: {len(rows)} CON rows; last t={max((t for t, _, _, _ in rows), default=0):.0f}')
        by_lvl = collections.Counter(l for _, _, l, _ in rows)
        print('rows by level:', dict(sorted(by_lvl.items())))
        by_obj = collections.Counter(label(u) for _, u, _, _ in rows)
        print('rows by object (top 15):', by_obj.most_common(15))
        print('\nlevel <= 1 rows (warnings/fatal), excluding notify-level echoes:')
        for t, u, l, m in rows:
            if l <= 1 and 'notify level' not in m:
                print(f'  t={t:7.1f} [{l}] {label(u)}: {m[:200]}')
        return 0
    key = sys.argv[2]
    show_all = '--all' in sys.argv
    uuids = [u for u, n in names.items() if n == key] or [u for u in {u for _, u, _, _ in rows} if u.endswith(key)]
    if not uuids:
        print(f'no object named/ending "{key}"; known names: {sorted(set(names.values()))[:40]}')
        return 1
    for u in uuids:
        sel = [(t, l, m) for t, uu, l, m in rows if uu == u]
        print(f'===== {label(u)} ({u}): {len(sel)} rows =====')
        for t, l, m in sel:
            if show_all or l <= 3 or TASK_WORDS.search(m):
                print(f'  t={t:7.1f} [{l}] {m[:300]}')
    return 0


if __name__ == '__main__':
    sys.exit(main())
