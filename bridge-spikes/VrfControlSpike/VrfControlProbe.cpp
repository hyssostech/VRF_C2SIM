// Phase 2 spike #2 (the decisive one): does the BOOST-HEAVY vrfcontrol/vl stack -
// where DtVrfRemoteController lives - link and load under /clr:netcore?
//
// spike #1 proved matrix.lib (light). This links the full DIS dependency set the real
// interface uses and:
//   1. constructs a DtAutomaticBackendSelector - a vrfcontrol object with a plain ctor,
//      forcing vrfcontrol.lib to link and vrfcontrol.dll + every boost/vl dependency to
//      load in the managed process. It needs no exercise connection and no license
//      (network only happens in init(), which we never call).
//   2. calls DtControlTypeString - a pure vrfmsgs free function - as a second proof point.
//
// If this loads and runs, the in-process C++/CLI bridge over DtVrfRemoteController is viable.

#include <vrfcontrol/automaticBackendSelector.h>   // DtAutomaticBackendSelector (vrfcontrol)
#include <vrfmsgs/messageTypes.h>                    // DtControlTypeString (vrfmsgs)

using namespace System;

// Do the native construction in an explicitly UNMANAGED function, so operator new/delete
// resolve to the native CRT that vrfcontrol.dll uses, not the managed module's. Mixing the
// two is a classic /clr access-violation source; this isolates it.
#pragma managed(push, off)
static bool NativeConstructBackendSelector()
{
    // Construct only. We deliberately do NOT delete: the isolation test proved (in a pure
    // native exe) that this object's DESTRUCTOR access-violates standalone - it references
    // global VR-Forces state that only a full app init sets up. That teardown crash is
    // native-reproducible and unrelated to /clr, so leaking here keeps the spike measuring
    // the thing under test: can vrfcontrol construct in the managed-hosted process.
    DtAutomaticBackendSelector* sel = new DtAutomaticBackendSelector();
    return sel != nullptr;
}
#pragma managed(pop)

namespace VrfBridge {

    public ref class VrfControlProbe abstract sealed
    {
    public:
        // Construct + destruct a real vrfcontrol object. Returns true if it survived,
        // which requires vrfcontrol.dll and all its boost-heavy deps to have loaded.
        static bool ConstructBackendSelector()
        {
            return NativeConstructBackendSelector();
        }

        // Pure vrfmsgs free function: int control-type enum -> its name.
        static String^ ControlTypeName(int type)
        {
            const char* s = DtControlTypeString(type);
            return s ? gcnew String(s) : gcnew String("(null)");
        }
    };
}
