#!/usr/bin/env python
"""Read the built .oob bytes; back-convert every object's kinematics positions to
lat/lon. Confirms geocentric convention (3 identical ECEF/object) + correct site."""
import math
import re
import zipfile

A = 6378137.0
F = 1.0 / 298.257223563
E2 = F * (2 - F)


def ecef_to_geodetic(x, y, z):
    lon = math.atan2(y, x)
    p = math.hypot(x, y)
    b = A * (1 - F)
    ep2 = (A * A - b * b) / (b * b)
    th = math.atan2(A * z, b * p)
    lat = math.atan2(z + ep2 * b * math.sin(th) ** 3, p - E2 * A * math.cos(th) ** 3)
    sl = math.sin(lat)
    N = A / math.sqrt(1 - E2 * sl * sl)
    return math.degrees(lat), math.degrees(lon), p / math.cos(lat) - N


def blocks(text):
    out = []
    for m in re.finditer(r"\(local-vrf-object", text):
        s = m.start(); d = 0; i = s; n = len(text); q = False
        while i < n:
            c = text[i]
            if q:
                if c == '"':
                    q = False
            elif c == '"':
                q = True
            elif c == "(":
                d += 1
            elif c == ")":
                d -= 1
                if d == 0:
                    out.append(text[s:i + 1]); break
            i += 1
    return out


POS = re.compile(r"\((?:parent-kinematics-state|kinematics-state|local-kinematics-state)\s*\n\s*\(position\s+(-?\d[\d.eE+-]*)\s+(-?\d[\d.eE+-]*)\s+(-?\d[\d.eE+-]*)\)")

for path, expect in [
        (r"C:\MAK\vrforces5.0.2\userData\scenarios\TankPltFixture_Sweden.scnx", (58.702956, 16.499229)),
        (r"C:\MAK\vrforces5.0.2\userData\scenarios\TankPltFixture_Mojave.scnx", (34.612956, -116.600487))]:
    print("=" * 74)
    print(path, "expect lat/lon ~", expect)
    with zipfile.ZipFile(path) as z:
        oob = next(z.read(n).decode("utf-8", "replace") for n in z.namelist() if n.endswith(".oob"))
    for b in blocks(oob):
        mk = re.search(r'marking-text "([^"]*)"', b).group(1)
        ot = re.search(r"\(object-type\s+(\d+)", b).group(1)
        if mk in ("GlblTerrDmg 1", "GlobalEnv 1", "Blocking Terrain Page-In Area 1"):
            continue
        pts = [(float(x), float(y), float(z)) for x, y, z in POS.findall(b)]
        uniq = set((round(x, 1), round(y, 1), round(z, 1)) for x, y, z in pts)
        lat, lon, h = ecef_to_geodetic(*pts[0])
        dlat = lat - expect[0]
        dlon = lon - expect[1]
        flag = "OK" if (abs(dlat) < 0.01 and abs(dlon) < 0.02) else "CHECK"
        print("  %-14s cls=%s  n_pos=%d identical=%s  lat=%.5f lon=%.5f h=%.1f  d=(%.4f,%.4f) [%s]"
              % (mk, ot, len(pts), len(uniq) == 1, lat, lon, h, dlat, dlon, flag))
