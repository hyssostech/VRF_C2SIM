// ScriptVarSerializeProbe - lane M3b (2026-09-28). OFFLINE: no controller, no federation, nothing sent.
//
// *** THE HYPOTHESIS THIS PROBE WAS BUILT FOR IS REFUTED - read this before reusing it. ***
// It asked whether navigate-to-location's `destination` failed to BIND (the planned move completed at once with 0 m
// moved in G1-3). The G1-3 Result refutes that with the vendor's own parameter echo at console level 3 (run
// 20260928T190047Z, vrfc2simapp.log L9681-L9713, every member, both vertices): "...Task 0 name and parameters:
// navigate-to-location: destination={3448198.452440, 1485421.208769, 5138688.441927}" - the exact vector this probe
// computes for (54.029734,23.305499,0) - and section C below agrees offline: the vendor's location-reference
// upgrade turns the DtRwVector the interface sends into the right absolute reference. What failed was the EXECUTOR:
// the move-along subtask on the script-created route answered "DtAggregatedMoveAlongController::setupRoute -- %1
// route does not exist. | <member> Pathr" (L9811-L9875) - the route-name budget; RUNBOOK sec 11o. The facade was
// NOT changed; this probe is kept as the record of the serialization (sections A-C) and of the refutation.
//
// QUESTION (as built): in what FORM does the planned move's navigate-to-location `destination` leave this interface,
// and what form does VR-Forces' own front end give a variable the script declares `locationreference`
// (SMS\base\scripts\navigate-to-location.xml :34-35)?
//
// It builds a real DtScriptedTaskTask and writes it with the vendor's own reader/writer text serialization
// (DtReaderWriter::putSelf, readerWriter.h :133 - the form a saved .oob/.pln holds), then round-trips it through the
// vendor's network encode/decode (DtBufferSerialize::getSize/encode/decode on a DtScriptedTask, scriptedTaskTask.h
// :331-336) and writes the decoded copy the same way.
//
//   A. THE FOUR API VARIANTS for `destination`, on the vendor API directly:
//        V1  setValue(name, DtVector)                          - default type DtScriptedTaskLocationVariable (:97)
//        V2  setValue(name, DtVector, "locationreference")      - the type argument overridden (:97)
//        V3  setLocation(name, DtVector)                        - the untyped-looking helper (:109)
//        V4  setValue(name, DtRwLocationReference, "locationreference") - the reader/writer overload (:121) with an
//            ABSOLUTE DtRwLocationReference (vrfutil/rwLocationReference.h :84 setAbsoluteLocation)
//        V5  variables().addVariable(DtRwVector) only           - the Lua-issued subtask shape (no data type)
//   B. THE FACADE'S SHIPPING PATH: this file #includes the facade translation unit (FacadeDir, default
//      src\VrfFacade) so the file-static addScriptVar RunScriptedTask uses is called directly; every existing caller's
//      variable list is written (point this at another commit's facade copy with /p:FacadeDir to diff the two).
//   C. THE VENDOR'S OWN CONVERTERS on our values (vrfutil/scriptedTaskVariableConverters.h, scriptedTaskVariable.h):
//      the location-reference converter's upgradeFromVariable on a DtRwVector, toRwPtr of the script's declared
//      variable, and the tag-keyed stringRep for each (tag, value class) pair a variant produces.
//
// Run with --vrfutil-factory to install DtVrfutilReaderWriterFactory (vrfutil/vrfutilReaderWriterFactory.h) before
// any reader/writer is created - the factory the network decode uses to re-create a variable by its type string
// (readerWriter/rwVariableBindings.h :20-26).

#include "VrfFacade.cpp"   // the facade TU itself (anonymous-namespace addScriptVar / toGeocentric / toGeodetic)

#include <vrftasks/scriptedTaskTask.h>
#include <vrfutil/rwLocationReference.h>
#include <vrfutil/scriptedTaskVariable.h>
#include <vrfutil/scriptedTaskVariableConverters.h>
#include <vrfutil/vrfutilReaderWriterFactory.h>
#include <readerWriter/readerWriterFactory.h>
#include <readerWriter/rwVector.h>
#include <readerWriter/rwString.h>
#include <readerWriter/rwReal.h>
#include <readerWriter/rwBoolean.h>

#include <cstdio>
#include <cstring>
#include <string>
#include <vector>
#include <typeinfo>

namespace {

const char* kScript = "navigate-to-location";
// T10 vertex 1 of G1-3 (runs/20260928T190047Z_run, vrfc2simapp.log L9617): (54.029734,23.305499) at altitude 0.
const vrf::Geodetic kDest = { 54.029734, 23.305499, 0.0 };

// SEH guard for vendor calls that may fault; no C++ object with a destructor lives in the guarded frame (C2712).
int guardedCall(void (*fn)(void*), void* ctx, unsigned long* code)
{
    __try
    {
        fn(ctx);
        return 1;
    }
    __except (EXCEPTION_EXECUTE_HANDLER)
    {
        *code = GetExceptionCode();
        return 0;
    }
}

struct PutCtx { DtReaderWriter* rw; std::string* out; int indent; };
void putFn(void* p)
{
    PutCtx* c = static_cast<PutCtx*>(p);
    c->rw->putSelf(*c->out, c->indent);
}

std::string put(DtReaderWriter& rw)
{
    std::string s;
    PutCtx c = { &rw, &s, 0 };
    unsigned long code = 0;
    if (!guardedCall(&putFn, &c, &code))
    {
        char buf[96];
        std::snprintf(buf, sizeof buf, "<putSelf FAULTED, structured exception 0x%08lx>\n", code);
        s += buf;
    }
    return s;
}

void bisectPut()
{
    DtRwVector vec(DtVector(1.0, 2.0, 3.0), DtString("destination"));
    std::printf("[bisect] DtRwVector alone:\n%s", put(vec).c_str());
    DtRwString str(DtString("pathQuery"), "MAK_ROAD");
    std::printf("[bisect] DtRwString alone:\n%s", put(str).c_str());
    DtScriptedTaskTask t(DtString("task"));
    t.init();
    t.setScriptId(kScript);
    t.setValue(DtString("destination"), DtVector(1.0, 2.0, 3.0));
    std::printf("[bisect] task.variables():\n%s", put(t.variables()).c_str());
    std::printf("[bisect] task.variableDataTypes():\n%s", put(t.variableDataTypes()).c_str());
    std::printf("[bisect] task:\n%s", put(t).c_str());
}

// The task's PAYLOAD in the .oob form: its script id and its two binding sets, each written by its own putSelf.
// (The whole-task putSelf faults in a controller-less process right after "(task " - section 0 shows it - so the
// fixed wrapper lines task-type / subtask / allow-task-visualizations are not written; they carry no variable.)
std::string putTask(DtScriptedTaskTask& t)
{
    std::string s = "(script-id \"";
    s += t.scriptId().c_str();
    s += "\")\n";
    s += put(t.variables());
    s += put(t.variableDataTypes());
    return s;
}

void block(const char* title, const std::string& text)
{
    std::printf("----- %s -----\n%s", title, text.c_str());
    if (text.empty() || text.back() != '\n') std::printf("\n");
    std::printf("----- end %s -----\n", title);
}

// The other five variables exactly as the M3 facade sends them (AggregateMovePlanner.cs NavigateVars; the
// vendor-default setValue overloads: string, string, double, checkbox, string).
void addOtherFive(DtScriptedTask& t)
{
    t.setValue(DtString("obstacleQuery"), std::string("MAK_OBSTACLE"));
    t.setValue(DtString("pathQuery"), std::string("MAK_ROAD"));
    t.setValue(DtString("buffer"), 10.0);
    t.setValue(DtString("displayRoute"), false);
    t.setValue(DtString("query"), std::string(""));
}

// Encode with the vendor's buffer serializer, decode into a fresh task, write it.
std::string roundTrip(const DtScriptedTaskTask& t, int& bytes)
{
    bytes = DtBufferSerialize::getSize(static_cast<const DtScriptedTask&>(t));
    std::vector<char> buf((size_t)bytes + 64, '\0');
    char* end = DtBufferSerialize::encode(static_cast<const DtScriptedTask&>(t), buf.data());
    (void)end;
    DtScriptedTaskTask back(DtString("task"));
    back.init();
    DtBufferSerialize::decode(static_cast<DtScriptedTask&>(back), buf.data());
    return putTask(back);
}

void variant(const char* name, void (*setDest)(DtScriptedTaskTask&, const DtVector&), bool full)
{
    std::printf("[probe] %s: build\n", name);
    DtScriptedTaskTask t(DtString("task"));   // a writeName: putSelf needs one (scriptedTaskTask.h :284-286)
    t.init();
    t.setScriptId(kScript);
    setDest(t, toGeocentric(kDest));
    if (full) addOtherFive(t);
    std::printf("[probe] %s: putSelf\n", name);
    std::string before = putTask(t);
    std::printf("[probe] %s: round trip\n", name);
    int bytes = 0;
    std::string after = roundTrip(t, bytes);
    std::string title = std::string(name) + (full ? " (all six)" : " (destination only)");
    block((title + " putSelf").c_str(), before);
    block((title + " after network encode/decode").c_str(), after);
    std::printf("[%s] wire bytes %d; decoded copy %s the original\n", title.c_str(), bytes,
                before == after ? "IDENTICAL to" : "DIFFERS from");
}

void v1(DtScriptedTaskTask& t, const DtVector& v) { t.setValue(DtString("destination"), v); }
void v2(DtScriptedTaskTask& t, const DtVector& v)
{
    t.setValue(DtString("destination"), v, DtScriptedTaskLocationReferenceVariable);
}
void v3(DtScriptedTaskTask& t, const DtVector& v) { t.setLocation(DtString("destination"), v); }
void v4(DtScriptedTaskTask& t, const DtVector& v)
{
    DtRwLocationReference lr(DtString("destination"));
    lr.setAbsoluteLocation(v);
    t.setValue(DtString("destination"), static_cast<const DtReaderWriter&>(lr), DtScriptedTaskLocationReferenceVariable);
}
void v5(DtScriptedTaskTask& t, const DtVector& v)
{
    t.variables().addVariable(new DtRwVector(v, DtString("destination")));
}

// ---- B: the facade's shipping path ----------------------------------------------------------------------------
void facadeBlock(const char* title, const char* scriptId, const std::vector<vrf::ScriptVar>& vars)
{
    DtScriptedTaskTask t(DtString("task"));   // a writeName: putSelf needs one (scriptedTaskTask.h :284-286)
    t.init();
    t.setScriptId(scriptId);
    for (const vrf::ScriptVar& v : vars) addScriptVar(t, v);
    block((std::string("FACADE ") + title).c_str(), putTask(t));
}

void facadeSection()
{
    using vrf::ScriptVar;
    const vrf::Geodetic direct = { 54.0268, 23.3172, 0.0 };       // --populate-selftest p11's point move
    const vrf::Geodetic v2pt = { 54.040348, 23.324206, 0.0 };     // p11's planner vertex
    const vrf::Geodetic obj = { 34.5, -116.8, 42.0 };             // --scripted-task-selftest's mixed list
    facadeBlock("PA_Move_To_Location_Direct", "PA_Move_To_Location_Direct",
                { ScriptVar::Place("location", direct), ScriptVar::Flag("retrograde", false) });
    facadeBlock("PA_Move_Along_Route", "PA_Move_Along_Route",
                { ScriptVar::Object("route", "VRF_UUID:route"), ScriptVar::Flag("reverseDirection", false),
                  ScriptVar::Flag("startAtClosestVertex", false) });
    facadeBlock("PA_Patrol_Route", "PA_Patrol_Route", { ScriptVar::Object("route", "VRF_UUID:route") });
    facadeBlock("group_movement_simplified", "group_movement_simplified",
                { ScriptVar::Place("destination", v2pt), ScriptVar::Flag("useRoads", true) });
    facadeBlock("navigate-to-location (Place destination, the M3 call)", kScript,
                { ScriptVar::Place("destination", v2pt), ScriptVar::Text("obstacleQuery", "MAK_OBSTACLE"),
                  ScriptVar::Text("pathQuery", "MAK_ROAD"), ScriptVar::Number("buffer", 10.0),
                  ScriptVar::Flag("displayRoute", false), ScriptVar::Text("query", "") });
    facadeBlock("scripted-task-selftest mixed list", "selftest",
                { ScriptVar::Object("route", "VRF_UUID:route"), ScriptVar::Count("duration", 600),
                  ScriptVar::Flag("patrol", true), ScriptVar::Flag("deployDrones", false),
                  ScriptVar::Number("speedMps", 8.25), ScriptVar::Text("posture", "Hasty-Attack"),
                  ScriptVar::Place("objectivePoint", obj) });
}

// ---- C: the vendor's converters ----------------------------------------------------------------------------------
void doStringRep(const char* tag, const DtReaderWriter* rw, char* out, size_t n)
{
    DtString s = DtScriptedTaskVariable::stringRep(DtString(tag), rw);
    std::snprintf(out, n, "%s", s.c_str());
}

// SEH guard: a converter chosen by the TAG may static_cast the value to the tag's class. No C++ object with a
// destructor lives in this frame (C2712).
int guardedStringRep(const char* tag, const DtReaderWriter* rw, char* out, size_t n)
{
    __try
    {
        doStringRep(tag, rw, out, n);
        return 1;
    }
    __except (EXCEPTION_EXECUTE_HANDLER)
    {
        std::snprintf(out, n, "<structured exception 0x%08lx>", GetExceptionCode());
        return 0;
    }
}

void printLocRef(const char* label, const DtRwLocationReference& lr)
{
    const DtVector v = lr.vector();
    vrf::Geodetic g = toGeodetic(v);
    std::printf("%s: locationType %d, vector geocentric (%.6f %.6f %.6f) = geodetic (%.6f,%.6f,%.3f), stringRep '%s'\n",
                label, (int)lr.locationType(), v.x(), v.y(), v.z(), g.latDeg, g.lonDeg, g.altMeters,
                lr.stringRep().c_str());
}

void converterSection()
{
    const DtVector geoc = toGeocentric(kDest);
    DtScriptedTaskVariableConverter* cLoc = DtScriptedTaskVariable::converterFor("location");
    DtScriptedTaskVariableConverter* cRef = DtScriptedTaskVariable::converterFor("locationreference");
    DtScriptedTaskVariableConverter* cNoAlt = DtScriptedTaskVariable::converterFor("locationwithoutaltitude");
    std::printf("CONVERTER for \"location\": %s\n", cLoc ? typeid(*cLoc).name() : "(none)");
    std::printf("CONVERTER for \"locationreference\": %s\n", cRef ? typeid(*cRef).name() : "(none)");
    std::printf("CONVERTER for \"locationwithoutaltitude\": %s\n", cNoAlt ? typeid(*cNoAlt).name() : "(none)");

    // The script's declared variable, as navigate-to-location.xml :33-47 declares it.
    DtScriptedTaskVariable declared;
    declared.setVariableName("destination");
    declared.setType("locationreference");
    declared.setDefaultValue("");
    DtReaderWriter* fromMeta = declared.toRwPtr();
    std::printf("DECLARED destination (type locationreference, default \"\") toRwPtr -> %s\n",
                fromMeta ? fromMeta->readerWriterType() : "null (no default value: scriptedTaskVariable.h :152)");
    if (fromMeta) block("DECLARED toRwPtr putSelf", put(*fromMeta));
    delete fromMeta;

    // The vendor's upgrade hook, applied to what the interface sends (a DtRwVector) and, as a control, to a
    // DtRwLocationReference. (G1-3's level-3 echo shows the sim bound the right vector - consistent with this.)
    {
        DtRwLocationReference target(DtString("destination"));
        DtRwVector orig(geoc, DtString("destination"));
        bool replaced = declared.upgradeFromVariable(&target, &orig);
        std::printf("UPGRADE (variable, locationreference) from DtRwVector: returns %s\n", replaced ? "true" : "false");
        printLocRef("  target after", target);
    }
    if (cRef)
    {
        DtRwLocationReference target(DtString("destination"));
        DtRwVector orig(geoc, DtString("destination"));
        bool replaced = cRef->upgradeFromVariable(&target, &orig);
        std::printf("UPGRADE (converter) from DtRwVector: returns %s\n", replaced ? "true" : "false");
        printLocRef("  target after", target);
        DtRwLocationReference target2(DtString("destination"));
        DtRwLocationReference orig2(DtString("destination"));
        orig2.setAbsoluteLocation(geoc);
        bool replaced2 = cRef->upgradeFromVariable(&target2, &orig2);
        std::printf("UPGRADE (converter) from DtRwLocationReference (control): returns %s\n", replaced2 ? "true" : "false");
        printLocRef("  target after", target2);
    }
    {
        DtRwLocationReference fresh(DtString("destination"));
        printLocRef("DEFAULT-CONSTRUCTED DtRwLocationReference", fresh);
    }

    // The tag-keyed display converter on each (tag, value class) pair a variant produces.
    char out[512];
    DtRwVector vec(geoc, DtString("destination"));
    DtRwLocationReference ref(DtString("destination"));
    ref.setAbsoluteLocation(geoc);
    int ok1 = guardedStringRep("location", &vec, out, sizeof out);
    std::printf("STRINGREP (\"location\", DtRwVector) [V1/V3]: %s%s\n", ok1 ? "" : "FAULT ", out);
    int ok4 = guardedStringRep("locationreference", &ref, out, sizeof out);
    std::printf("STRINGREP (\"locationreference\", DtRwLocationReference) [V4]: %s%s\n", ok4 ? "" : "FAULT ", out);
    int ok2 = guardedStringRep("locationreference", &vec, out, sizeof out);
    std::printf("STRINGREP (\"locationreference\", DtRwVector) [V2]: %s%s\n", ok2 ? "" : "FAULT ", out);
}

// DtVrfutilReaderWriterFactory::create() is declared (vrfutilReaderWriterFactory.h :28) but not exported by
// vrfutil.lib; its (bool) constructor is.
DtReaderWriterFactory* makeVrfutilFactory() { return new DtVrfutilReaderWriterFactory(true); }

}  // namespace

int main(int argc, char** argv)
{
    std::setvbuf(stdout, nullptr, _IONBF, 0);   // a fault must not swallow what was already printed
    bool vrfutilFactory = false;
    bool skipFactoryRead = false;
    for (int i = 1; i < argc; ++i)
    {
        if (std::strcmp(argv[i], "--vrfutil-factory") == 0) vrfutilFactory = true;
        if (std::strcmp(argv[i], "--no-factory-read") == 0) skipFactoryRead = true;
    }
    std::printf("[probe] start\n");
    if (vrfutilFactory) DtReaderWriter::setFactoryCreatorFunction(&makeVrfutilFactory);
    DtReaderWriterFactory* f = skipFactoryRead ? nullptr : DtReaderWriter::factory();
    std::printf("[probe] factory read\n");
    std::printf("=== ScriptVarSerializeProbe (M3b) - reader/writer factory: %s%s ===\n", f ? typeid(*f).name() : "(null)",
                vrfutilFactory ? " (installed by --vrfutil-factory)" : " (the process default)");
    const vrf::Geodetic back = toGeodetic(toGeocentric(kDest));
    std::printf("destination (%.6f,%.6f,%.3f) -> geocentric -> (%.6f,%.6f,%.3f)\n", kDest.latDeg, kDest.lonDeg,
                kDest.altMeters, back.latDeg, back.lonDeg, back.altMeters);

    std::printf("\n=== 0. putSelf bisect ===\n");
    bisectPut();

    std::printf("\n=== A. the API variants for navigate-to-location's destination ===\n");
    variant("V1 setValue(vec)", &v1, false);
    variant("V2 setValue(vec, locationreference)", &v2, false);
    variant("V3 setLocation(vec)", &v3, false);
    variant("V4 setValue(DtRwLocationReference, locationreference)", &v4, false);
    variant("V5 addVariable(DtRwVector), no data type", &v5, false);
    variant("V1 setValue(vec)", &v1, true);
    variant("V4 setValue(DtRwLocationReference, locationreference)", &v4, true);

    std::printf("\n=== B. the facade's shipping path (addScriptVar) ===\n");
    facadeSection();

    std::printf("\n=== C. the vendor's converters on our values ===\n");
    converterSection();
    std::printf("\n=== done ===\n");
    return 0;
}
