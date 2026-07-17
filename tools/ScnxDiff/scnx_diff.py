#!/usr/bin/env python
"""
scnx_diff.py - offline DUMP / DIFF for VR-Forces .scnx scenario containers.

Groundwork plan item 0.5 (VRF_GROUNDWORK_PLAN.md). OFFLINE ONLY - this tool
never touches a live VR-Forces / RTI process. It only reads .scnx files, which
are ZIP containers holding VR-Forces scenario members.

A .scnx member set (VR-Forces 5.0.2) contains, per scenario:
  .scn  scenario master (S-expression)  - terrain, model set, time params, member refs
  .oob  order of battle  (S-expression) - the simulation objects (units + entities)
  .xtr  scenario-extras  (S-expression) - force / hostility table
  .orb .pln .omp         (S-expression) - orbat / plan / object-id -> uuid address map
  .osrx .spt .sgr .ovl .gui_settings (boost XML) - observer views, scripts, GUI state

The object model (units, aggregates, entities) lives in the .oob as MAK
Lisp-style S-expressions, NOT XML. This tool parses that S-expression format.

Two modes:
  dump <a.scnx>            structure dump: container members, scenario params,
                           forces, reconstructed org tree, per-object detail.
  diff <a.scnx> <b.scnx>   semantic unit-by-unit diff after canonicalization.

See README.md for the canonicalization / ignore rules and their rationale.
"""

import argparse
import os
import re
import sys
import zipfile
from collections import Counter, defaultdict, OrderedDict


# ---------------------------------------------------------------------------
# container
# ---------------------------------------------------------------------------

def read_container(path):
    """Return (members, infolist). members: OrderedDict name -> raw text (decoded)."""
    members = OrderedDict()
    info = []
    with zipfile.ZipFile(path) as z:
        for zi in z.infolist():
            info.append((zi.filename, zi.file_size))
            raw = z.read(zi.filename)
            members[zi.filename] = decode_bytes(raw)
    return members, info


def decode_bytes(raw):
    """Decode member bytes to text. Members are UTF-8; fall back defensively so a
    non-ASCII byte inside scenario content never crashes the parser."""
    for enc in ("utf-8", "cp1252", "latin-1"):
        try:
            return raw.decode(enc)
        except UnicodeDecodeError:
            continue
    return raw.decode("utf-8", errors="replace")


def classify(text):
    t = text.lstrip()
    if t.startswith("<?xml") or t.startswith("<"):
        return "xml"
    if t.startswith("("):
        return "sexpr"
    return "other"


# ---------------------------------------------------------------------------
# S-expression tokenizer / parser
# ---------------------------------------------------------------------------

def tokenize(s):
    toks = []
    i = 0
    n = len(s)
    while i < n:
        c = s[i]
        if c == "(" or c == ")":
            toks.append(c)
            i += 1
        elif c == '"':
            j = i + 1
            buf = ['"']
            while j < n:
                if s[j] == "\\" and j + 1 < n:
                    buf.append(s[j])
                    buf.append(s[j + 1])
                    j += 2
                    continue
                if s[j] == '"':
                    break
                buf.append(s[j])
                j += 1
            buf.append('"')
            toks.append("".join(buf))
            i = j + 1
        elif c.isspace():
            i += 1
        else:
            j = i
            while j < n and s[j] not in '() \t\r\n"':
                j += 1
            toks.append(s[i:j])
            i = j
    return toks


def parse_sexpr(text):
    """Parse S-expression text into a list of nodes. A node is either a scalar
    string token or a Python list of nodes."""
    toks = tokenize(text)
    pos = 0

    def read():
        nonlocal pos
        t = toks[pos]
        pos += 1
        if t == "(":
            lst = []
            while pos < len(toks) and toks[pos] != ")":
                lst.append(read())
            if pos < len(toks):
                pos += 1  # consume ')'
            return lst
        return t

    out = []
    while pos < len(toks):
        out.append(read())
    return out


# ---------- node helpers ----------

TAG_RE = re.compile(r"^[A-Za-z_][A-Za-z0-9_.:+-]*$")
NONTAG = {"True", "False", "USE-DEFAULT"}


def is_tag(x):
    return isinstance(x, str) and bool(TAG_RE.match(x)) and x not in NONTAG


def head(n):
    if isinstance(n, list) and n and is_tag(n[0]):
        return n[0]
    return None


def find(n, tag):
    if not isinstance(n, list):
        return None
    for c in n[1:]:
        if head(c) == tag:
            return c
    return None


def find_all(n, tag):
    res = []
    if isinstance(n, list):
        for c in n[1:]:
            if head(c) == tag:
                res.append(c)
    return res


def find_deep(n, tag, out=None):
    """All descendant nodes with the given head tag (any depth)."""
    if out is None:
        out = []
    if isinstance(n, list):
        for c in n:
            if isinstance(c, list):
                if head(c) == tag:
                    out.append(c)
                find_deep(c, tag, out)
    return out


def scalars_after_head(n):
    if not isinstance(n, list):
        return []
    start = 1 if (n and is_tag(n[0])) else 0
    return [x for x in n[start:] if not isinstance(x, list)]


def unquote(tok):
    if isinstance(tok, str) and len(tok) >= 2 and tok[0] == '"' and tok[-1] == '"':
        return tok[1:-1]
    return tok


def sval(n, tag):
    """First scalar value of child <tag>, unquoted; or None."""
    c = find(n, tag)
    if c is None:
        return None
    sc = scalars_after_head(c)
    return unquote(sc[0]) if sc else ""


# ---------------------------------------------------------------------------
# object model extraction (.oob)
# ---------------------------------------------------------------------------

class SimObject(object):
    __slots__ = ("node", "uuid", "oid", "marking", "cls", "dis", "force",
                 "agg_state", "parent_ref", "destroyed", "indep_tasked",
                 "appearance", "capabilities", "request_task", "children",
                 "superior_label")

    def __init__(self):
        self.node = None
        self.uuid = None
        self.oid = None
        self.marking = None
        self.cls = None
        self.dis = None
        self.force = None
        self.agg_state = None
        self.parent_ref = None
        self.destroyed = None
        self.indep_tasked = None
        self.appearance = None
        self.capabilities = None
        self.request_task = None
        self.children = []          # ordered list of child SimObject
        self.superior_label = None  # resolved: marking / "Force N" / "(force-level)"


def object_type_of(o):
    ot = find(o, "object-type")
    if ot is None:
        return None, None
    sc = scalars_after_head(ot)
    cls = sc[0] if sc else None
    tup = None
    for c in ot[1:]:
        if isinstance(c, list):
            tup = [unquote(x) for x in c if not isinstance(x, list)]
            break
    return cls, tup


def extract_objects(oob_nodes):
    """From parsed .oob nodes return list of SimObject in document order."""
    objs = []
    roots = [n for n in oob_nodes if head(n) == "order-of-battle"]
    containers = roots if roots else oob_nodes
    for cont in containers:
        items = cont[1:] if isinstance(cont, list) else []
        for it in items:
            if head(it) != "local-vrf-object":
                continue
            o = SimObject()
            o.node = it
            o.uuid = sval(it, "uuid")
            o.oid = sval(it, "object-identifier")
            o.marking = sval(it, "marking-text")
            o.cls, o.dis = object_type_of(it)
            sr = find(it, "state-repository")
            o.force = sval(sr, "force") if sr else None
            o.agg_state = sval(sr, "aggregate-state") if sr else None
            o.parent_ref = sval(sr, "parent-name") if sr else None
            o.destroyed = sval(sr, "is-destroyed") if sr else None
            o.indep_tasked = sval(sr, "independently-tasked") if sr else None
            o.appearance = sval(sr, "appearance") if sr else None
            o.capabilities = sval(sr, "capabilities") if sr else None
            o.request_task = sval(sr, "request-task-needed") if sr else None
            objs.append(o)
    return objs


FORCE_REF_RE = re.compile(r"^VRF_UUID:(\d+)\s+Force$")


def build_tree(objs):
    """Resolve parent_ref (a uuid, a 'VRF_UUID:N Force', or empty) into an org
    tree. Returns (force_roots) mapping force-label -> ordered top objects, and
    sets each object's children (document order) and superior_label.

    Subordinate ORDER = document order of objects sharing a parent. Per the
    VR-Forces User's Guide, the first subordinate is the unit leader. Echelon
    IDs are assigned by VR-Forces at runtime and are NOT stored in the .scnx,
    so they cannot be surfaced from the file (see README)."""
    by_uuid = {}
    for o in objs:
        if o.uuid:
            by_uuid[o.uuid] = o

    force_roots = OrderedDict()
    for o in objs:
        ref = o.parent_ref or ""
        m = FORCE_REF_RE.match(ref)
        if ref == "" or ref is None:
            # unattached / force-other level
            o.superior_label = "(force-level: %s)" % (o.force or "?")
            force_roots.setdefault(o.superior_label, []).append(o)
        elif m:
            o.superior_label = "Force %s" % m.group(1)
            force_roots.setdefault(o.superior_label, []).append(o)
        elif ref in by_uuid and by_uuid[ref] is not o:
            parent = by_uuid[ref]
            parent.children.append(o)
            o.superior_label = parent.marking or ref
        else:
            # dangling reference - parent not in this file
            o.superior_label = "(unresolved: %s)" % ref
            force_roots.setdefault(o.superior_label, []).append(o)

    # Completeness guard: any object not reachable by walking children down from
    # a root (e.g. a malformed parent->child->parent cycle) would otherwise
    # vanish from the tree view. Surface such objects under an explicit root.
    reachable = set()

    def mark(o):
        if id(o) in reachable:
            return
        reachable.add(id(o))
        for c in o.children:
            mark(c)

    for roots in force_roots.values():
        for o in roots:
            mark(o)
    orphans = [o for o in objs if id(o) not in reachable]
    if orphans:
        force_roots["(orphaned / cyclic references)"] = orphans
    return force_roots


# ---------- controller / task / embedded extraction ----------

CTRL_RE = re.compile(r"(controller|process-state-repository-default|-psr-default)")


def controllers_of(o):
    tags = set()

    def walk(n):
        if isinstance(n, list):
            h = head(n)
            if h and CTRL_RE.search(h):
                tags.add(h)
            for c in n:
                if isinstance(c, list):
                    walk(c)
    walk(o.node)
    return sorted(tags)


def subsystems_of(o):
    """Direct child tags of every system-psr-manager (the wired subsystems)."""
    subs = []
    for mgr in find_deep(o.node, "system-psr-manager"):
        for c in mgr[1:]:
            h = head(c)
            if h and h not in ("is-enabled",):
                subs.append(h)
    # de-dup preserving order
    seen = set()
    out = []
    for s in subs:
        if s not in seen:
            seen.add(s)
            out.append(s)
    return out


def tasks_of(o):
    """List (controller-name, task-type) from a non-empty task-status-list."""
    out = []
    tsl = find(find(o.node, "state-repository") or o.node, "task-status-list")
    if tsl is None:
        return out
    for ts in find_all(tsl, "task-status"):
        cn = sval(ts, "controller-name")
        tmsg = find(ts, "task-message")
        tt = None
        if tmsg is not None:
            tnode = find(tmsg, "task")
            if tnode is not None:
                tt = sval(tnode, "task-type")
        out.append((cn, tt))
    return out


def embedded_of(o):
    names = []
    for ee in find_deep(o.node, "embedded-entity"):
        nm = sval(ee, "entity-name")
        if nm is not None:
            names.append(nm)
    return names


def has_plan(o):
    """True if the object carries a non-trivial individual plan / suspended tasks."""
    sr = find(o.node, "state-repository") or o.node
    stl = find(sr, "suspended-task-list")
    nsus = len(find_all(stl, "task-status")) if stl is not None else 0
    return nsus


# ---------------------------------------------------------------------------
# scenario params (.scn) and forces (.xtr)
# ---------------------------------------------------------------------------

SCN_FIELDS = [
    "scenario-name", "Terrain-Database", "Simulation-Model-Set-Files",
    "time-multiplier", "frame-mode", "frame-time", "auto-reorganize",
    "random-number-seed", "run-duration-time", "version",
]


def scn_summary(text):
    nodes = parse_sexpr(text)
    scn = None
    for n in nodes:
        if head(n) == "Scenario":
            scn = n
            break
    if scn is None:
        return []
    out = []
    for f in SCN_FIELDS:
        v = sval(scn, f)
        if v is not None:
            out.append((f, v))
    return out


def xtr_forces(text):
    nodes = parse_sexpr(text)
    forces = []
    for extras in nodes:
        fh = find(extras, "force-hostility")
        if fh is None:
            continue
        for f in find_all(fh, "force"):
            fid = sval(f, "force-id")
            fname = sval(f, "force-name")
            hostile = [unquote(x) for x in scalars_after_head(find(f, "hostile-to") or [])]
            forces.append((fid, fname, hostile))
    return forces


# ---------------------------------------------------------------------------
# scenario loading
# ---------------------------------------------------------------------------

class Scenario(object):
    def __init__(self, path):
        self.path = path
        self.members, self.info = read_container(path)
        self.objs = []
        self.force_roots = OrderedDict()
        oob_txt = self._member_by_ext(".oob")
        if oob_txt is not None:
            self.objs = extract_objects(parse_sexpr(oob_txt))
            self.force_roots = build_tree(self.objs)
        self.scn = self._member_by_ext(".scn")
        self.xtr = self._member_by_ext(".xtr")

    def _member_by_ext(self, ext):
        best = None
        for name, txt in self.members.items():
            if name.lower().endswith(ext):
                # concatenate if several (robustness); usually exactly one
                best = txt if best is None else best + "\n" + txt
        return best


# ---------------------------------------------------------------------------
# DUMP
# ---------------------------------------------------------------------------

def fmt_dis(cls, dis):
    cls_lbl = {"1": "entity", "3": "aggregate"}.get(cls, cls)
    dis_s = " ".join(dis) if dis else "?"
    return "class=%s(%s) enum=(%s)" % (cls, cls_lbl, dis_s)


def cmd_dump(args):
    sc = Scenario(args.scnx)
    w = sys.stdout.write

    w("=" * 78 + "\n")
    w("SCNX DUMP: %s\n" % os.path.abspath(args.scnx))
    w("=" * 78 + "\n\n")

    w("CONTAINER MEMBERS (%d):\n" % len(sc.info))
    for name, size in sc.info:
        txt = sc.members.get(name, "")
        w("  %-34s %8d bytes  [%s]\n" % (name, size, classify(txt)))
    w("\n")

    if sc.scn:
        w("SCENARIO PARAMETERS (.scn):\n")
        for k, v in scn_summary(sc.scn):
            w("  %-28s %s\n" % (k, v))
        w("\n")

    if sc.xtr:
        forces = xtr_forces(sc.xtr)
        if forces:
            w("FORCES (.xtr force-hostility):\n")
            for fid, fname, hostile in forces:
                h = (" hostile-to=[%s]" % " ".join(hostile)) if hostile else ""
                w("  force %-3s %-14s%s\n" % (fid, repr(fname), h))
            w("\n")

    # counts
    n_agg = sum(1 for o in sc.objs if o.cls == "3")
    n_ent = sum(1 for o in sc.objs if o.cls == "1")
    w("OBJECT COUNT: %d total  (%d aggregate/unit, %d entity, %d other)\n\n"
      % (len(sc.objs), n_agg, n_ent, len(sc.objs) - n_agg - n_ent))

    # org tree
    w("ORGANIZATION TREE (subordinate order = document order; first = leader;\n")
    w("echelon IDs are runtime-only and not stored in .scnx):\n")

    _seen_tree = set()

    def emit_tree(o, depth, is_leader):
        indent = "  " + "    " * depth
        lead = "*" if is_leader else " "
        tag = "[AGG:%s]" % o.agg_state if o.agg_state else "[ent]"
        cyc = ""
        if id(o) in _seen_tree:
            cyc = "  <CYCLE - already shown, not recursing>"
        w("%s%s %-26s %s  %s%s\n" % (indent, lead, repr(o.marking), tag,
                                     fmt_dis(o.cls, o.dis), cyc))
        if id(o) in _seen_tree:
            return
        _seen_tree.add(id(o))
        for i, ch in enumerate(o.children):
            emit_tree(ch, depth + 1, i == 0)

    # "*" marks the leader = first subordinate of its parent.
    for label, roots in sc.force_roots.items():
        w("  %s:\n" % label)
        for o in roots:
            emit_tree(o, 1, False)
    w("\n")

    if args.brief:
        return

    # per-object detail
    w("PER-OBJECT DETAIL:\n")
    w("-" * 78 + "\n")
    for o in sc.objs:
        w("NAME       : %s\n" % repr(o.marking))
        w("  uuid     : %s\n" % o.uuid)
        w("  oid      : %s\n" % o.oid)
        w("  type     : %s\n" % fmt_dis(o.cls, o.dis))
        w("  force    : %s\n" % o.force)
        if o.agg_state:
            w("  agg-state: %s\n" % o.agg_state)
        w("  superior : %s\n" % o.superior_label)
        if o.children:
            kids = ["%s%s" % (repr(c.marking), " (leader)" if i == 0 else "")
                    for i, c in enumerate(o.children)]
            w("  subord.  : %d ordered -> %s\n" % (len(o.children), ", ".join(kids)))
        subs = subsystems_of(o)
        if subs:
            w("  subsystems: %s\n" % ", ".join(subs))
        ctrls = controllers_of(o)
        if ctrls:
            shown = ctrls if args.full else ctrls[:12]
            more = "" if len(shown) == len(ctrls) else "  (+%d more; use --full)" % (len(ctrls) - len(shown))
            w("  controllers: %s%s\n" % (", ".join(shown), more))
        tks = tasks_of(o)
        if tks:
            w("  tasks    : %s\n" % ", ".join("%s:%s" % (c, t) for c, t in tks))
        emb = embedded_of(o)
        if emb:
            w("  embedded : %s\n" % ", ".join(repr(e) for e in emb))
        flags = []
        if o.destroyed and o.destroyed != "False":
            flags.append("destroyed=%s" % o.destroyed)
        if o.indep_tasked and o.indep_tasked != "False":
            flags.append("independently-tasked=%s" % o.indep_tasked)
        if o.request_task and o.request_task != "False":
            flags.append("request-task-needed=%s" % o.request_task)
        nsus = has_plan(o)
        if nsus:
            flags.append("suspended-tasks=%d" % nsus)
        if o.appearance and o.appearance not in ("0", "0X0"):
            flags.append("appearance=%s" % o.appearance)
        if flags:
            w("  flags    : %s\n" % ", ".join(flags))
        w("\n")


# ---------------------------------------------------------------------------
# canonicalize + DIFF
# ---------------------------------------------------------------------------

FLOAT_RE = re.compile(r"^-?\d+\.\d+$")

# Volatile / identity tags dropped from the field-level diff by default.
DEFAULT_IGNORE = {
    "uuid",                 # object identity - regenerated every save/creation
    "object-identifier",    # sim-address - regenerated
    "parent-name",          # raw superior uuid - replaced by synthetic #superior
    "next-suspend-id",      # runtime counter
    "time-of-day",          # scenario clock snapshot
    "next-supply-check-time",
    "set-scenario-start-time-at-local-on-first-object-creation-time",
    "set-scenario-start-time-at-local-on-first-object-creation",
}


def norm_scalar(tok):
    s = unquote(tok)
    if FLOAT_RE.match(s):
        v = round(float(s), 4)
        if v == 0.0:
            v = 0.0  # collapse -0.0
        return "%.4f" % v
    return s


def flatten(node, path, out, ignore):
    if isinstance(node, list) and node and is_tag(node[0]):
        rest = node[1:]
    else:
        rest = node if isinstance(node, list) else [node]
    scal = [x for x in rest if not isinstance(x, list)]
    kids = [x for x in rest if isinstance(x, list)]
    if scal or not kids:
        out[path + "/="] = " ".join(norm_scalar(s) for s in scal)
    tagcount = Counter()
    for k in kids:
        t = k[0] if (k and is_tag(k[0])) else "#list"
        tagcount[t] += 1
    seen = Counter()
    for k in kids:
        t = k[0] if (k and is_tag(k[0])) else "#list"
        if t in ignore:
            continue
        if tagcount[t] > 1:
            i = seen[t]
            seen[t] += 1
            kp = "%s/%s[%d]" % (path, t, i)
        else:
            kp = "%s/%s" % (path, t)
        flatten(k, kp, out, ignore)


def object_leaves(o, ignore):
    """Canonical flattened leaf dict for one SimObject, plus synthetic org fields."""
    out = {}
    flatten(o.node, "", out, ignore)
    # synthetic structural fields (name-based, survive re-creation)
    out["#superior/="] = o.superior_label or ""
    out["#subordinates/="] = ",".join(c.marking or "?" for c in o.children)
    return out


def match_key(o, mode):
    if mode == "uuid":
        return o.uuid or o.marking or o.oid
    return o.marking or o.uuid or o.oid


def cmd_diff(args):
    a = Scenario(args.a)
    b = Scenario(args.b)
    w = sys.stdout.write
    ignore = set(DEFAULT_IGNORE)
    for extra in (args.ignore or []):
        ignore.add(extra)

    w("=" * 78 + "\n")
    w("SCNX DIFF\n")
    w("  A: %s\n" % os.path.abspath(args.a))
    w("  B: %s\n" % os.path.abspath(args.b))
    w("  match-by: %s   ignored-tags: %d\n" % (args.match_by, len(ignore)))
    w("=" * 78 + "\n\n")

    # --- container member set ---
    amem = set(a.members)
    bmem = set(b.members)
    if amem != bmem:
        w("CONTAINER MEMBER SET DIFFERS:\n")
        for m in sorted(amem - bmem):
            w("  only in A: %s\n" % m)
        for m in sorted(bmem - amem):
            w("  only in B: %s\n" % m)
        w("\n")

    # --- scenario params ---
    if a.scn and b.scn:
        sa = dict(scn_summary(a.scn))
        sb = dict(scn_summary(b.scn))
        pk = [k for k in sa if k not in ("scenario-name",)]
        diffs = [(k, sa.get(k), sb.get(k)) for k in sorted(set(sa) | set(sb))
                 if sa.get(k) != sb.get(k) and k != "scenario-name"]
        if diffs:
            w("SCENARIO PARAMETER DIFFERENCES:\n")
            for k, va, vb in diffs:
                w("  %-24s A=%s  B=%s\n" % (k, va, vb))
            w("\n")

    # --- object matching ---
    amap = index_objects(a.objs, args.match_by)
    bmap = index_objects(b.objs, args.match_by)
    akeys = set(amap)
    bkeys = set(bmap)

    only_a = sorted(akeys - bkeys, key=str)
    only_b = sorted(bkeys - akeys, key=str)
    common = sorted(akeys & bkeys, key=str)

    total_field_diffs = 0
    changed_objs = 0

    if only_a:
        w("UNITS ONLY IN A (%d):\n" % len(only_a))
        for k in only_a:
            o = amap[k]
            w("  %s  [%s]\n" % (repr(o.marking), fmt_dis(o.cls, o.dis)))
        w("\n")
    if only_b:
        w("UNITS ONLY IN B (%d):\n" % len(only_b))
        for k in only_b:
            o = bmap[k]
            w("  %s  [%s]\n" % (repr(o.marking), fmt_dis(o.cls, o.dis)))
        w("\n")

    w("MATCHED UNITS: %d\n\n" % len(common))
    for k in common:
        oa = amap[k]
        ob = bmap[k]
        la = object_leaves(oa, ignore)
        lb = object_leaves(ob, ignore)
        keys = set(la) | set(lb)
        rows = []
        for kk in sorted(keys):
            va = la.get(kk)
            vb = lb.get(kk)
            if va != vb:
                rows.append((kk, va, vb))
        if rows:
            changed_objs += 1
            total_field_diffs += len(rows)
            w("UNIT %s  (%d field diff%s):\n"
              % (repr(oa.marking), len(rows), "" if len(rows) == 1 else "s"))
            for kk, va, vb in rows:
                field = kk[:-2] if kk.endswith("/=") else kk
                if va is None:
                    w("    + %s : (absent in A) -> B=%s\n" % (field, vb))
                elif vb is None:
                    w("    - %s : A=%s -> (absent in B)\n" % (field, va))
                else:
                    w("    ~ %s : A=%s  B=%s\n" % (field, va, vb))
            w("\n")

    w("-" * 78 + "\n")
    w("SUMMARY: %d only-in-A, %d only-in-B, %d matched; "
      "%d units changed, %d total field differences.\n"
      % (len(only_a), len(only_b), len(common), changed_objs, total_field_diffs))
    identical = (not only_a and not only_b and total_field_diffs == 0
                 and amem == bmem)
    w("RESULT: %s\n" % ("IDENTICAL (zero differences)" if identical
                        else "DIFFERENCES FOUND"))
    return 0


def index_objects(objs, mode):
    """key -> SimObject, disambiguating duplicate keys positionally."""
    counts = Counter(match_key(o, mode) for o in objs)
    seen = Counter()
    out = OrderedDict()
    for o in objs:
        k = match_key(o, mode)
        if counts[k] > 1:
            idx = seen[k]
            seen[k] += 1
            k = "%s#%d" % (k, idx)
        out[k] = o
    return out


# ---------------------------------------------------------------------------
# cli
# ---------------------------------------------------------------------------

def main(argv=None):
    p = argparse.ArgumentParser(
        prog="scnx_diff",
        description="Offline DUMP / DIFF for VR-Forces .scnx scenario containers.")
    sub = p.add_subparsers(dest="cmd")

    pd = sub.add_parser("dump", help="structure dump of one .scnx")
    pd.add_argument("scnx")
    pd.add_argument("--brief", action="store_true",
                    help="org tree + container only; skip per-object detail")
    pd.add_argument("--full", action="store_true",
                    help="do not truncate controller lists")
    pd.set_defaults(func=cmd_dump)

    px = sub.add_parser("diff", help="semantic unit-by-unit diff of two .scnx")
    px.add_argument("a")
    px.add_argument("b")
    px.add_argument("--match-by", choices=["marking", "uuid"], default="marking",
                    help="unit match key (default: marking = the unit name)")
    px.add_argument("--ignore", action="append", metavar="TAG",
                    help="additional S-expr tag to ignore (repeatable)")
    px.set_defaults(func=cmd_diff)

    # Scenario content may contain non-ASCII (e.g. accented unit names). The
    # console default on Windows is cp1252, which would raise on such output.
    # Emit UTF-8 and backslash-escape anything unencodable rather than crash.
    try:
        sys.stdout.reconfigure(encoding="utf-8", errors="backslashreplace")
    except (AttributeError, ValueError):
        pass

    args = p.parse_args(argv)
    if not getattr(args, "cmd", None):
        p.print_help()
        return 2
    return args.func(args) or 0


if __name__ == "__main__":
    sys.exit(main())
