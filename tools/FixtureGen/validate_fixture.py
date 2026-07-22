#!/usr/bin/env python
"""Adversarial offline validation of the authored fixtures. ASCII only."""
import re
import sys
import zipfile

SEXPR = (".scn", ".oob", ".xtr", ".orb", ".pln", ".omp")


def paren_balance(text):
    """Quote-aware paren balance. Returns (net, min_depth)."""
    depth, mind, instr = 0, 0, False
    i, n = 0, len(text)
    while i < n:
        c = text[i]
        if instr:
            if c == "\\":
                i += 2
                continue
            if c == '"':
                instr = False
        elif c == '"':
            instr = True
        elif c == "(":
            depth += 1
        elif c == ")":
            depth -= 1
            mind = min(mind, depth)
        i += 1
    return depth, mind, instr


def nonascii(text):
    return [(i, ord(ch)) for i, ch in enumerate(text) if ord(ch) > 126 or (ord(ch) < 9)]


def check(path):
    print("=" * 76)
    print("VALIDATE", path)
    print("=" * 76)
    ok = True
    members = {}
    with zipfile.ZipFile(path) as z:
        for name in z.namelist():
            raw = z.read(name)
            members[name] = raw
            if name.endswith(SEXPR):
                txt = raw.decode("utf-8", "replace")
                net, mind, instr = paren_balance(txt)
                na = nonascii(txt)
                status = "OK" if (net == 0 and mind == 0 and not instr and not na) else "FAIL"
                if status == "FAIL":
                    ok = False
                print("  %-34s paren net=%d min=%d openstr=%s nonascii=%d  [%s]"
                      % (name, net, mind, instr, len(na), status))
                if na:
                    print("      first nonascii:", na[:5])

    oob = next(v.decode("utf-8", "replace") for k, v in members.items() if k.endswith(".oob"))
    pln = next(v.decode("utf-8", "replace") for k, v in members.items() if k.endswith(".pln"))
    omp = next(v.decode("utf-8", "replace") for k, v in members.items() if k.endswith(".omp"))

    # aggregate own uuid = the class-3 block's uuid
    agg_uuid = None
    for m in re.finditer(r"\(local-vrf-object", oob):
        pass
    m = re.search(r'\(object-type\s+3\s+\(11 1 225 3 2 0 0\)\)', oob)
    # find the uuid nearest before the header object-type-3 marking "AR Plt 1"
    hm = re.search(r'marking-text "AR Plt 1".*?\(uuid\s+"(VRF_UUID:[0-9a-f-]+)"\)', oob, re.S)
    agg_uuid = hm.group(1) if hm else None
    print("  aggregate uuid           :", agg_uuid)

    # plan checks
    pn = re.search(r'\(plan-name\s+"(VRF_UUID:[0-9a-f-]+)"\)', pln)
    rt = re.search(r'\(route\s+"(VRF_UUID:[0-9a-f-]+)"\)', pln)
    tt = re.search(r'\(task-type "([^"]+)"\)', pln)
    trg = re.search(r'\(triggers\s*\)', pln)
    print("  plan-name                :", pn.group(1) if pn else None,
          "== agg?", (pn and pn.group(1) == agg_uuid))
    print("  plan task-type           :", tt.group(1) if tt else None)
    print("  plan route ref           :", rt.group(1) if rt else None)
    print("  plan (triggers ) empty   :", bool(trg))
    # route uuid present in oob?
    if rt:
        print("  route uuid in .oob        :", ('(uuid  "%s")' % rt.group(1)) in oob)

    # members parent = aggregate?
    par = re.findall(r'\(parent-name\s+"(VRF_UUID:[0-9a-f-]+)"\)', oob)
    n_par_agg = sum(1 for p in par if p == agg_uuid)
    print("  members parented to agg  :", n_par_agg, "(expect 4)")

    # demo scripted task must be gone
    print("  demo task stripped       :", "test-vehicle-platoon-position-query" not in oob)
    # aggregate task-status-list empty? (no task-status under the aggregate)
    aggblk = re.search(r'marking-text "AR Plt 1".*?(?=\(local-vrf-object|\Z)', oob, re.S)
    ab = aggblk.group(0) if aggblk else ""
    print("  agg has NO task-status   :", "(task-status " not in ab)
    print("  agg aggregate-state      :", (re.search(r'\(aggregate-state\s+(\w+)\)', ab) or ['', '?'])[1]
          if re.search(r'\(aggregate-state\s+(\w+)\)', ab) else "?")
    print("  agg move-along PSR present:",
          "vrf-aggregate-move-along-process-state-repository-default" in ab
          or "aggregated-move-along-process-state-repository-default" in ab)

    # omp entry count
    n_omp = len(re.findall(r"\(map-entry", omp))
    print("  .omp map-entries         :", n_omp, "(expect 9)")
    # every oob object uuid has an omp entry?
    oob_uuids = set(re.findall(r'\(uuid\s+"(VRF_UUID:[0-9a-f-]+)"\)', oob))
    # keep only header uuids (approx: those that also are object identities) - compare omp set subset
    omp_uuids = set(re.findall(r'\(uuid\s+"(VRF_UUID:[0-9a-f-]+)"\)', omp))
    print("  omp uuids not in oob     :", sorted(omp_uuids - oob_uuids), "(expect [])")

    print("  RESULT                   :", "OK" if ok else "FAIL")
    return ok


if __name__ == "__main__":
    allok = True
    for p in [r"C:\MAK\vrforces5.0.2\userData\scenarios\TankPltFixture_Sweden.scnx",
              r"C:\MAK\vrforces5.0.2\userData\scenarios\TankPltFixture_Mojave.scnx"]:
        allok &= check(p)
    print("\nALL FIXTURES:", "OK" if allok else "FAIL")
