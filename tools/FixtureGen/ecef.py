#!/usr/bin/env python
"""WGS84 geodetic <-> ECEF, and back-check. ASCII only."""
import math

A = 6378137.0                      # WGS84 semi-major
F = 1.0 / 298.257223563
E2 = F * (2 - F)                   # first eccentricity squared


def geodetic_to_ecef(lat_deg, lon_deg, h):
    lat = math.radians(lat_deg)
    lon = math.radians(lon_deg)
    sl = math.sin(lat)
    N = A / math.sqrt(1 - E2 * sl * sl)
    x = (N + h) * math.cos(lat) * math.cos(lon)
    y = (N + h) * math.cos(lat) * math.sin(lon)
    z = (N * (1 - E2) + h) * sl
    return x, y, z


def ecef_to_geodetic(x, y, z):
    # Bowring's method
    lon = math.atan2(y, x)
    p = math.hypot(x, y)
    b = A * (1 - F)
    ep2 = (A * A - b * b) / (b * b)
    th = math.atan2(A * z, b * p)
    lat = math.atan2(z + ep2 * b * math.sin(th) ** 3,
                     p - E2 * A * math.cos(th) ** 3)
    sl = math.sin(lat)
    N = A / math.sqrt(1 - E2 * sl * sl)
    h = p / math.cos(lat) - N
    return math.degrees(lat), math.degrees(lon), h


def show(tag, lat, lon, h):
    x, y, z = geodetic_to_ecef(lat, lon, h)
    blat, blon, bh = ecef_to_geodetic(x, y, z)
    print("%-22s lat=%.6f lon=%.6f h=%.1f" % (tag, lat, lon, h))
    print("    ECEF  X=%.3f  Y=%.3f  Z=%.3f" % (x, y, z))
    print("    back  lat=%.6f lon=%.6f h=%.3f  (roundtrip ok=%s)"
          % (blat, blon, bh, abs(blat-lat) < 1e-9 and abs(blon-lon) < 1e-9))


print("=== VALIDATE handoff Mojave number: (34.61,-116.60,h=0) expect X=-2353028.662 Y=-4698889.659 Z=3602341.757 ===")
x, y, z = geodetic_to_ecef(34.61, -116.60, 0.0)
print("    computed X=%.3f Y=%.3f Z=%.3f" % (x, y, z))
print("    delta    dX=%.3f dY=%.3f dZ=%.3f"
      % (x - (-2353028.662), y - (-4698889.659), z - 3602341.757))

print()
print("=== FIXTURE POINTS (platoon start + short 3-pt eastward route, above-terrain) ===")
# Sweden: R9 1222.MechPlt start, terrain ~51 m; waypoints h=81 (=+30)
print("-- SWEDEN (Bogaland) --")
show("SWE start", 58.702956, 16.499229, 51.0)
show("SWE wp1",   58.702956, 16.499229, 81.0)
show("SWE wp2",   58.702956, 16.501823, 81.0)
show("SWE wp3",   58.702956, 16.504417, 81.0)
print("-- MOJAVE --")
show("MOJ start", 34.612956, -116.600487, 1041.0)
show("MOJ wp1",   34.612956, -116.600487, 1071.0)
show("MOJ wp2",   34.612956, -116.598849, 1071.0)
show("MOJ wp3",   34.612956, -116.597211, 1071.0)
