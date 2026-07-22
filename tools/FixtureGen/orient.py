#!/usr/bin/env python
"""Reverse-engineer VR-Forces orientation-tait-bryan convention (DIS Euler in ECEF)
and derive the East-heading, level attitude at a target lat/lon. ASCII only."""
import math

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
    h = p / math.cos(lat) - N
    return math.degrees(lat), math.degrees(lon), h


def Rz(a):
    c, s = math.cos(a), math.sin(a)
    return [[c, -s, 0], [s, c, 0], [0, 0, 1]]

def Ry(a):
    c, s = math.cos(a), math.sin(a)
    return [[c, 0, s], [0, 1, 0], [-s, 0, c]]

def Rx(a):
    c, s = math.cos(a), math.sin(a)
    return [[1, 0, 0], [0, c, -s], [0, s, c]]

def matmul(P, Q):
    return [[sum(P[i][k] * Q[k][j] for k in range(3)) for j in range(3)] for i in range(3)]


def ecef_to_ned_dcm(lat, lon):
    """Rotation ECEF->NED (rows = N,E,D in ECEF components)."""
    sl, cl = math.sin(lat), math.cos(lat)
    so, co = math.sin(lon), math.cos(lon)
    # N,E,D unit vectors expressed in ECEF
    N = [-sl * co, -sl * so, cl]
    E = [-so, co, 0.0]
    D = [-cl * co, -cl * so, -sl]
    return [N, E, D]  # rows


def dcm_to_taitbryan_zyx(Rb2w):
    """Extract DIS psi,theta,phi from a body->world DCM using ZYX (psi about Z,
    theta about Y, phi about X). DIS: world=ECEF, orientation is world->body via
    Rz(psi)Ry(theta)Rx(phi) applied as body->world = transpose. Here we take Rb2w
    and solve for psi,theta,phi such that Rb2w = Rz(psi)Ry(theta)Rx(phi)."""
    theta = math.asin(max(-1.0, min(1.0, -Rb2w[2][0])))
    if abs(math.cos(theta)) > 1e-9:
        psi = math.atan2(Rb2w[1][0], Rb2w[0][0])
        phi = math.atan2(Rb2w[2][1], Rb2w[2][2])
    else:
        psi = math.atan2(-Rb2w[0][1], Rb2w[1][1])
        phi = 0.0
    return psi, theta, phi


def dis_euler_for_local(lat, lon, heading_deg, pitch_deg=0.0, roll_deg=0.0):
    """DIS Euler (psi,theta,phi) in ECEF for a body with given local NED heading/pitch/roll."""
    h, p, r = map(math.radians, (heading_deg, pitch_deg, roll_deg))
    # body->NED
    Rb2ned = matmul(matmul(Rz(h), Ry(p)), Rx(r))
    Rned2ecef = transpose(ecef_to_ned_dcm(lat, lon))  # NED->ECEF
    Rb2ecef = matmul(Rned2ecef, Rb2ned)
    return dcm_to_taitbryan_zyx(Rb2ecef)


def transpose(M):
    return [[M[j][i] for j in range(3)] for i in range(3)]


def euler_to_local(lat, lon, psi, theta, phi):
    """Inverse: given DIS Euler in ECEF, recover local NED heading/pitch/roll."""
    Rb2ecef = matmul(matmul(Rz(psi), Ry(theta)), Rx(phi))
    Recef2ned = ecef_to_ned_dcm(lat, lon)
    Rb2ned = matmul(Recef2ned, Rb2ecef)
    # extract heading/pitch/roll (ZYX) from body->NED
    hp, tp, rp = dcm_to_taitbryan_zyx(Rb2ned)
    return math.degrees(hp), math.degrees(tp), math.degrees(rp)


def rad(v):
    return "(%.6f %.6f %.6f)" % v


print("=== Locate the cloned objects ===")
for tag, (x, y, z) in [
    ("testFindTankPlt agg", (-2812790.097501, -4332801.127899, 3728565.598318)),
    ("BGABF AR Plt 1 agg ", (-2248455.027581, -4689305.196829, 3682604.905386)),
    ("Makland route anchor", (3138938.035081, 5441852.289068, 1101676.315700)),
]:
    lat, lon, h = ecef_to_geodetic(x, y, z)
    print("  %s lat=%.5f lon=%.5f h=%.1f" % (tag, lat, lon, h))

print()
print("=== Test convention: recover local heading/pitch/roll from stored orientations ===")
# BGABF AR Plt 1 stored orientation-tait-bryan
la, lo, _ = ecef_to_geodetic(-2248455.027581, -4689305.196829, 3682604.905386)
la, lo = math.radians(la), math.radians(lo)
hpr = euler_to_local(la, lo, -1.722977, 0.930465, -2.902643)
print("  BGABF AR Plt 1: stored (-1.722977 0.930465 -2.902643) -> local hdg=%.2f pitch=%.2f roll=%.2f deg" % hpr)
# Makland route anchor stored orientation
la2, lo2, _ = ecef_to_geodetic(3138938.035081, 5441852.289068, 1101676.315700)
la2, lo2 = math.radians(la2), math.radians(lo2)
hpr2 = euler_to_local(la2, lo2, -2.093990, -1.396050, -3.141590)
print("  Makland route   : stored (-2.093990 -1.396050 -3.141590) -> local hdg=%.2f pitch=%.2f roll=%.2f deg" % hpr2)

print()
print("=== FORWARD: DIS Euler for EAST-heading, level, at fixture sites ===")
for tag, lat, lon in [("SWEDEN 58.702956,16.499229", 58.702956, 16.499229),
                       ("MOJAVE 34.612956,-116.600487", 34.612956, -116.600487)]:
    e = dis_euler_for_local(math.radians(lat), math.radians(lon), 90.0)  # heading East
    print("  %s : orientation-tait-bryan %s" % (tag, rad(e)))
    # round-trip check
    back = euler_to_local(math.radians(lat), math.radians(lon), *e)
    print("      round-trip local hdg=%.2f pitch=%.2f roll=%.2f (expect 90,0,0)" % back)
