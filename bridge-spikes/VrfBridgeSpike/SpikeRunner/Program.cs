using VrfBridge;

// Phase 2 spike runner: a plain net6.0 process calling the /clr:netcore bridge,
// which in turn calls native MAK matrix.lib. Proves the full chain loads and runs.
//
// Test point: one of the STP scenario's coordinates (58.6753 N, 16.4829 E, 100 m),
// the east end of the move order 14.MechBn actually drove.

Console.WriteLine("=== Phase 2 spike: net6 -> C++/CLI(netcore) -> native MAK matrix.lib ===\n");

double lat = 58.6752856770139, lon = 16.4828633358011, alt = 100.0;
Console.WriteLine($"input geodetic : lat={lat}  lon={lon}  alt={alt} m");

// Call 1: native DtGeodeticCoord geodetic -> geocentric (ECEF)
Geocentric ecef = VrfMath.GeodeticToGeocentric(lat, lon, alt);
Console.WriteLine($"native ECEF    : X={ecef.X:F3}  Y={ecef.Y:F3}  Z={ecef.Z:F3}");

// Sanity: |ECEF| must be ~Earth radius (~6.36e6 m at this latitude), not zero/garbage.
double mag = VrfMath.GeocentricMagnitude(ecef.X, ecef.Y, ecef.Z);
Console.WriteLine($"|ECEF|         : {mag:F1} m   (expect ~6.36e6)");

// Call 2: native inverse, to round-trip
Geodetic back = VrfMath.GeocentricToGeodetic(ecef.X, ecef.Y, ecef.Z);
Console.WriteLine($"round-trip     : lat={back.LatDeg:F9}  lon={back.LonDeg:F9}  alt={back.AltMeters:F3} m");

// Verdicts
double dLat = Math.Abs(back.LatDeg - lat);
double dLon = Math.Abs(back.LonDeg - lon);
double dAlt = Math.Abs(back.AltMeters - alt);
bool magOk = mag > 6.3e6 && mag < 6.4e6;
bool rtOk = dLat < 1e-6 && dLon < 1e-6 && dAlt < 1e-3;

Console.WriteLine();
Console.WriteLine($"[{(magOk ? "PASS" : "FAIL")}] ECEF magnitude is a plausible Earth radius");
Console.WriteLine($"[{(rtOk ? "PASS" : "FAIL")}] geodetic->geocentric->geodetic round-trips (dLat={dLat:E2} dLon={dLon:E2} dAlt={dAlt:E2})");

int rc = (magOk && rtOk) ? 0 : 1;
Console.WriteLine($"\n{(rc == 0 ? "SPIKE PASSED - the C++/CLI netcore bridge over native MAK libs works." : "SPIKE FAILED")}");
return rc;
