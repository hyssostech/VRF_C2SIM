#!/usr/bin/env python
"""make_nav_terrain.py - register navigation areas on a COPY of a VR-Forces terrain.

VR-Forces 5.2 discovers navigation areas ONLY from `<Type>navData</Type>` generic records
in the terrain configuration (.mtf) - each record's `path` parameter names the area's
.navRuntimeConfig (UG52 66.5 "Import Navigation Area" = add the record + save the terrain;
the shipped MAK Earth (online).mtf carries 9 such records under <myGenericRecords>).
Every real file path in that .mtf is a $(SHARED_DATA_DIR) macro (<myFilename>), so a copy
of the file works from ANY folder: this tool never writes under C:\\MAK.

    python make_nav_terrain.py --runtime-config "<dir>\\NavArea-ground-platform X.navRuntimeConfig" \
        [--runtime-config ...] [--terrain "<original .mtf>"] [--out "<copy .mtf>"] [--name NAME]

The copy's default location is tools/navdata/out/<NAME>.mtf (gitignored). Point a fixture's
Terrain-Database / Gui-Terrain-Database at the copy (build_fixture.py --profile 5.2 --empty
--terrain "<copy .mtf>") and reload the scenario (UG52 66.3.3).

Self-test: python make_nav_terrain.py --selftest   (no MAK install needed)
"""
from __future__ import print_function
import argparse
import io
import os
import re
import sys

DEFAULT_TERRAIN = (r"C:\MAK\SharedData\19\latest\TerrainData\TerrainConfiguration"
                   r"\MAK Earth (online).mtf")
OUT_DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)), "out")

# The record list: <myGenericRecords ...><count>N</count><item_version>1</item_version><item ...>
RECORDS_RE = re.compile(
    r"(<myGenericRecords\b[^>]*>\s*<count>)(\d+)(</count>\s*<item_version>\d+</item_version>)",
    re.S)
CLOSE_RE = re.compile(r"</myGenericRecords>")

# A follow-on item (boost serialization: class ids appear on the FIRST item only; later
# items of the same vector repeat the bare element names).
ITEM_TEMPLATE = (
    "\t\t<item>\n"
    "\t\t\t<Type>navData</Type>\n"
    "\t\t\t<Parameters>\n"
    "\t\t\t\t<ParameterMap>\n"
    "\t\t\t\t\t<count>1</count>\n"
    "\t\t\t\t\t<item_version>0</item_version>\n"
    "\t\t\t\t\t<item>\n"
    "\t\t\t\t\t\t<first>path</first>\n"
    "\t\t\t\t\t\t<second>{path}</second>\n"
    "\t\t\t\t\t</item>\n"
    "\t\t\t\t</ParameterMap>\n"
    "\t\t\t</Parameters>\n"
    "\t\t</item>\n")


def add_nav_records(mtf_text, runtime_config_paths):
    """Return (new_text, n_before, n_after). Pure function; raises on a malformed file."""
    m = RECORDS_RE.search(mtf_text)
    if not m:
        raise ValueError("no <myGenericRecords> list with a <count> in the terrain file")
    if len(RECORDS_RE.findall(mtf_text)) != 1:
        raise ValueError("more than one <myGenericRecords> list - refusing to guess")
    n_before = int(m.group(2))
    if n_before < 1:
        # An empty vector has no first item to carry the class ids; the shipped online
        # terrain has 9, so this is a guard, not a supported case.
        raise ValueError("the record list is empty; a first item with class ids is needed")
    already = set(re.findall(r"<second>([^<]*\.navRuntimeConfig)</second>", mtf_text))
    new = [p for p in runtime_config_paths if p not in already]
    for p in new:
        if "<" in p or "&" in p:
            raise ValueError("path needs XML escaping, refusing: %s" % p)
    n_after = n_before + len(new)
    text = mtf_text[:m.start(2)] + str(n_after) + mtf_text[m.end(2):]
    # Insert the new items right before the list's closing tag (after the last item).
    close = CLOSE_RE.search(text, m.end())
    if not close:
        raise ValueError("no </myGenericRecords> after the list")
    # Detect the file's line ending and indentation style from the closing tag's line.
    eol = "\r\n" if "\r\n" in text[:5000] else "\n"
    block = "".join(ITEM_TEMPLATE.format(path=p) for p in new).replace("\n", eol)
    # The closing tag is preceded by a tab; put the items before that indentation.
    line_start = text.rfind(eol, 0, close.start())
    insert_at = line_start + len(eol) if line_start >= 0 else close.start()
    text = text[:insert_at] + block + text[insert_at:]
    return text, n_before, n_after


def build(terrain, out_path, runtime_configs):
    with io.open(terrain, "r", encoding="utf-8", newline="") as f:
        src = f.read()
    for rc in runtime_configs:
        if not os.path.isfile(rc):
            raise SystemExit("runtime config not found: %s" % rc)
    text, before, after = add_nav_records(src, [os.path.abspath(rc) for rc in runtime_configs])
    out_dir = os.path.dirname(out_path)
    if out_dir and not os.path.isdir(out_dir):
        os.makedirs(out_dir)
    with io.open(out_path, "w", encoding="utf-8", newline="") as f:
        f.write(text)
    return before, after


def selftest():
    sample = (
        "<?xml version=\"1.0\"?>\r\n<mtf>\r\n\t<myGenericRecords class_id=\"23\" "
        "tracking_level=\"0\" version=\"0\">\r\n\t\t<count>2</count>\r\n\t\t<item_version>1"
        "</item_version>\r\n\t\t<item class_id=\"24\" tracking_level=\"0\" version=\"1\">\r\n"
        "\t\t\t<Type>navData</Type>\r\n\t\t\t<Parameters class_id=\"25\" tracking_level=\"0\" "
        "version=\"0\">\r\n\t\t\t\t<ParameterMap class_id=\"26\" tracking_level=\"0\" "
        "version=\"0\">\r\n\t\t\t\t\t<count>1</count>\r\n\t\t\t\t\t<item_version>0</item_version>"
        "\r\n\t\t\t\t\t<item class_id=\"27\" tracking_level=\"0\" version=\"0\">\r\n"
        "\t\t\t\t\t\t<first>path</first>\r\n\t\t\t\t\t\t<second>$(SHARED_DATA_DIR)\\A.navRuntimeConfig"
        "</second>\r\n\t\t\t\t\t</item>\r\n\t\t\t\t</ParameterMap>\r\n\t\t\t</Parameters>\r\n"
        "\t\t</item>\r\n\t\t<item>\r\n\t\t\t<Type>navData</Type>\r\n\t\t\t<Parameters>\r\n"
        "\t\t\t\t<ParameterMap>\r\n\t\t\t\t\t<count>1</count>\r\n\t\t\t\t\t<item_version>0"
        "</item_version>\r\n\t\t\t\t\t<item>\r\n\t\t\t\t\t\t<first>path</first>\r\n"
        "\t\t\t\t\t\t<second>$(SHARED_DATA_DIR)\\B.navRuntimeConfig</second>\r\n\t\t\t\t\t</item>"
        "\r\n\t\t\t\t</ParameterMap>\r\n\t\t\t</Parameters>\r\n\t\t</item>\r\n"
        "\t</myGenericRecords>\r\n\t<myOther>x</myOther>\r\n</mtf>\r\n")
    fails = []

    def check(name, cond):
        if not cond:
            fails.append(name)

    out, b, a = add_nav_records(sample, [r"C:\x\NavArea-ground-platform Z.navRuntimeConfig"])
    check("count 2->3", (b, a) == (2, 3) and "<count>3</count>" in out)
    check("record inserted before the close", out.index("Z.navRuntimeConfig") < out.index("</myGenericRecords>"))
    check("record after the last shipped one", out.index("Z.navRuntimeConfig") > out.index("B.navRuntimeConfig"))
    check("three navData records", out.count("<Type>navData</Type>") == 3)
    check("crlf preserved", "\n" not in out.replace("\r\n", ""))
    check("rest untouched", out.endswith("\t<myOther>x</myOther>\r\n</mtf>\r\n"))
    check("class ids not duplicated", out.count('class_id="24"') == 1)
    # idempotent: adding the same path again changes nothing
    out2, b2, a2 = add_nav_records(out, [r"C:\x\NavArea-ground-platform Z.navRuntimeConfig"])
    check("idempotent", out2 == out and (b2, a2) == (3, 3))
    # two at once, one duplicate
    out3, b3, a3 = add_nav_records(sample, [r"C:\x\P.navRuntimeConfig", r"C:\x\Q.navRuntimeConfig",
                                            r"$(SHARED_DATA_DIR)\A.navRuntimeConfig"])
    check("two new + one existing -> 4", (b3, a3) == (2, 4) and out3.count("<Type>navData</Type>") == 4)
    # well-formed XML
    try:
        import xml.etree.ElementTree as ET
        root = ET.fromstring(out3)
        recs = root.find("myGenericRecords")
        check("xml count matches items", recs.find("count").text == "4"
              and len(recs.findall("item")) == 4)
    except Exception as e:  # noqa: BLE001
        fails.append("xml parse: %s" % e)
    # guards
    try:
        add_nav_records("<mtf></mtf>", ["C:\\x\\a.navRuntimeConfig"]); fails.append("no-list guard")
    except ValueError:
        pass
    try:
        add_nav_records(sample, ["C:\\bad<path>.navRuntimeConfig"]); fails.append("escape guard")
    except ValueError:
        pass
    n = 11
    if fails:
        print("make_nav_terrain selftest: FAIL %d/%d: %s" % (len(fails), n, ", ".join(fails)))
        return 1
    print("make_nav_terrain selftest: PASS %d/%d" % (n, n))
    return 0


def main(argv=None):
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--runtime-config", action="append", default=[], metavar="FILE",
                    help="a .navRuntimeConfig to register (repeatable)")
    ap.add_argument("--terrain", default=DEFAULT_TERRAIN, help="the original .mtf (read only)")
    ap.add_argument("--name", default="MAK Earth (online) + nav",
                    help="name of the copy (file stem under --out-dir)")
    ap.add_argument("--out", default=None, help="explicit output .mtf path (overrides --name)")
    ap.add_argument("--out-dir", default=OUT_DIR)
    ap.add_argument("--selftest", action="store_true")
    a = ap.parse_args(argv)
    if a.selftest:
        return selftest()
    if not a.runtime_config:
        ap.error("at least one --runtime-config is required")
    if not os.path.isfile(a.terrain):
        raise SystemExit("terrain not found: %s" % a.terrain)
    out = a.out or os.path.join(a.out_dir, a.name + ".mtf")
    if os.path.abspath(out).lower() == os.path.abspath(a.terrain).lower():
        raise SystemExit("refusing to overwrite the original terrain")
    if os.path.abspath(out).lower().startswith("c:\\mak\\"):
        raise SystemExit("refusing to write under C:\\MAK (pass an explicit --out elsewhere)")
    before, after = build(a.terrain, out, a.runtime_config)
    print("WROTE %s" % out)
    print("  original      = %s (read only)" % a.terrain)
    print("  navData records: %d -> %d" % (before, after))
    for rc in a.runtime_config:
        print("  + %s" % os.path.abspath(rc))
    return 0


if __name__ == "__main__":
    sys.exit(main())
