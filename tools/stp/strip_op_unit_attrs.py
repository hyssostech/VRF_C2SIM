"""Drop named attributes from every UNIT object of an STP .op object_set, so an older scenario loads in a current STP.

Why: STP-1104 (2026-10-13) - STP main rejects a unit carrying `role: sof_support` ("filling out FS failed"); the
attribute is derived again from the SIDC on load, so dropping it loses nothing (CAPSTONE_STP883_2026-10-13.md,
the load isolation). Remove this step once STP-1104 is fixed.

Usage: strip_op_unit_attrs.py <in.op> <out.op> attr [attr ...]
Prints one line: units=<n> dropped=<attr:value:count,...>. Exit 0, or 2 on a usage error. ASCII only.
"""
import sys


def split_top(body):
    """Split 'k: v,k: v' at top-level commas (outside [] () and quotes)."""
    parts, depth, quote, cur = [], 0, None, []
    for ch in body:
        if quote:
            cur.append(ch)
            if ch == quote:
                quote = None
            continue
        if ch in "'\"":
            quote = ch
        elif ch in '[(':
            depth += 1
        elif ch in '])':
            depth -= 1
        if ch == ',' and depth == 0:
            parts.append(''.join(cur))
            cur = []
            continue
        cur.append(ch)
    if cur:
        parts.append(''.join(cur))
    return parts


def main(argv):
    if len(argv) < 4:
        print('usage: strip_op_unit_attrs.py <in.op> <out.op> attr [attr ...]')
        return 2
    src, dst, drop = argv[1], argv[2], set(argv[3:])
    with open(src, encoding='utf-8') as f:
        lines = [line.rstrip('\r') for line in f.read().split('\n')]
    out, units, seen = [], 0, {}
    for line in lines:
        stripped = line.strip()
        if stripped.startswith('[fsTYPE: unit,') and stripped.endswith(']'):
            keep = []
            for part in split_top(stripped[1:-1]):
                key = part.split(':', 1)[0].strip()
                if key in drop:
                    value = part.split(':', 1)[1].strip() if ':' in part else ''
                    seen[(key, value)] = seen.get((key, value), 0) + 1
                    continue
                keep.append(part)
            out.append('[' + ','.join(keep) + ']')
            units += 1
        else:
            out.append(line)
    with open(dst, 'w', encoding='utf-8', newline='\n') as f:
        f.write('\n'.join(out))
    print('units=%d dropped=%s' % (units, ','.join('%s:%s:%d' % (k, v, n) for (k, v), n in sorted(seen.items()))))
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv))
