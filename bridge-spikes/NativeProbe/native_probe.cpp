// Isolation test: construct DtAutomaticBackendSelector in a PURE NATIVE exe (no /clr).
// If this also access-violates, the object simply cannot be constructed standalone -
// the /clr spike-#2 crash is not a C++/CLI problem and my probe target was wrong.
// If this succeeds, the crash is specific to the managed-host environment.

#include <vrfcontrol/automaticBackendSelector.h>
#include <cstdio>

int main()
{
    printf("native probe: about to construct DtAutomaticBackendSelector\n");
    fflush(stdout);

    DtAutomaticBackendSelector* sel = new DtAutomaticBackendSelector();
    printf("native probe: constructed at %p\n", (void*)sel);
    fflush(stdout);

    delete sel;
    printf("native probe: destroyed OK\n");
    fflush(stdout);
    return 0;
}
