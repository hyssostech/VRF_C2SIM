// Phase 2 spike: prove a /clr:netcore C++/CLI assembly can link against MAK's native
// VR-Link libraries and be called from a net6.0 process.
//
// It wraps exactly one native computation the port needs - the geodetic->geocentric
// coordinate conversion that C2SIMinterface::geodeticToGeocentric performs via
// DtGeodeticCoord - so this is both a toolchain proof and a correctness proof of that math.
//
// No license, no network, no exercise connection: the failure modes here are purely
// "does /clr:netcore compile, link against matrix.lib, and load under net6".

#include <matrix/geodeticCoord.h>   // DtGeodeticCoord
#include <matrix/vlVector.h>        // DtVector64 / DtVector

using namespace System;

namespace VrfBridge {

    // Managed value type carrying a geocentric (ECEF) coordinate back to .NET.
    public value struct Geocentric
    {
        double X;
        double Y;
        double Z;
    };

    // Managed value type for a geodetic coordinate.
    public value struct Geodetic
    {
        double LatDeg;
        double LonDeg;
        double AltMeters;
    };

    public ref class VrfMath abstract sealed
    {
    public:
        // Mirror of C2SIMinterface::geodeticToGeocentric: degrees in, ECEF metres out,
        // computed by the native DtGeodeticCoord using MAK's ellipsoid model.
        static Geocentric GeodeticToGeocentric(double latDeg, double lonDeg, double altMeters)
        {
            const double deg2rad = 0.017453292519943295; // matches DtDeg2Rad
            DtGeodeticCoord geod(latDeg * deg2rad, lonDeg * deg2rad, altMeters);
            DtVector geoc = geod.geocentric();

            Geocentric result;
            result.X = geoc.x();
            result.Y = geoc.y();
            result.Z = geoc.z();
            return result;
        }

        // Inverse, so the runner can round-trip and prove the native call actually ran
        // (as opposed to returning zeros or garbage marshalled across the boundary).
        static Geodetic GeocentricToGeodetic(double x, double y, double z)
        {
            const double rad2deg = 57.29577951308232;
            DtGeodeticCoord geod;
            geod.setGeocentric(DtVector(x, y, z));

            Geodetic result;
            result.LatDeg = geod.lat() * rad2deg;
            result.LonDeg = geod.lon() * rad2deg;
            result.AltMeters = geod.alt();
            return result;
        }

        // Distance from Earth centre - a coarse sanity value the runner can check
        // against the expected ~6.36e6 m without trusting the round-trip.
        static double GeocentricMagnitude(double x, double y, double z)
        {
            return System::Math::Sqrt(DtVector(x, y, z).magnitudeSquared());
        }
    };
}
