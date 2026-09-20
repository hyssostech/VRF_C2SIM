# PREREG STP-825 BUNDLING - CREATE CENSUS (moved from the main prereg for length)

This is section 1.1 of docs/experiments/PREREG_STP825_BUNDLING_2026-09-20.md, moved to this
sibling file only because the main prereg exceeded the ~400-line guidance; nothing here was
altered from the original draft (scratchpad/validation/PREREG_STP825_BUNDLING_DRAFT.md sec 1.1).
Read the main file first for the surrounding analysis (sec 1.2 interprets this table).

---------------------------------------------------------------------------------------------------
### 1.1 The create table (35 rows; full file: create_table.txt)

| #  | local clock | log line | result  | rejected module                       | payload B | gap s |
|----|-------------|----------|---------|---------------------------------------|-----------|-------|
| 1  | 11:50:35    | 261      | Error   | MAK-DynamicTerrain-2_evolved.xml      | 9,699     | -     |
| 2  | 11:54:01    | 1873     | Error   | RPR-MAK_Experimental_IFF-4.xml        | 65,000    | 205.6 |
| 3  | 11:54:12    | 3485     | Error   | MAK-VRFAggregate-7_evolved.xml        | 65,000    | 11.0  |
| 4  | 11:54:23    | 5097     | Success | -                                     | -         | 11.0  |
| 5  | 11:59:04    | 10004    | Success | -                                     | -         | 281.4 |
| 6  | 11:59:21    | 14911    | Success | -                                     | -         | 16.6  |
| 7  | 11:59:37    | 19818    | Error   | MAK-DER-1_evolved.xml                 | 9,114     | 15.9  |
| 8  | 11:59:48    | 21430    | Error   | RPR_FOM_v2.0_1516-2010.xml            | 65,000    | 11.1  |
| 9  | 11:59:59    | 23042    | Success | -                                     | -         | 11.0  |
| 10-19 | 12:00:14 .. 12:32:10 | 27949..99292 | Success x10 | -            | -         | 8-1619|
| 20 | 12:32:39    | 104199   | Error   | MAK-METOC-3_evolved.xml               | 30,178    | 28.5  |
| 21 | 12:39:07    | 105811   | Error   | RPR-Enumerations_Experimental_IFF.xml | 65,000    | 388.8 |
| 22 | 12:39:17    | 107457   | Error   | MAK-Aerodrome-1_evolved.xml           | 34,197    | 9.7   |
| 23 | 12:39:27    | 109103   | Error   | MAK-Aerodrome-1_evolved.xml           | 34,197    | 9.6   |
| 24 | 12:39:36    | 110749   | Success | -                                     | -         | 9.7   |
| 25 | 12:41:41    | 115656   | Error   | RPR-Enumerations_Experimental_IFF.xml | 65,000    | 124.8 |
| 26 | 12:41:50    | 117302   | Success | -                                     | -         | 8.7   |
| 27-29 | 13:11:58 .. 13:21:50 | 122209..172801 | Success x3 | -          | -         | long  |
| 30 | 16:22:10 (20:22:11Z, appNo 4630) | 245688 | Error | MAK-Aerodrome-1_evolved.xml | 34,197 | 10820 |
| 31 | 16:22:18 (20:22:19Z, appNo 4631) | 247334 | Error | MAK-VRLExt-3_evolved.xml    | 5,692  | 8.7   |
| 32 | 16:22:27 (20:22:28Z, appNo 4632) | 248980 | Error | MAK-VRFExt-12_evolved.xml   | 52,829 | 8.2   |
| 33 | 16:22:34 (20:22:35Z, appNo 4633) | 250592 | Error | RPR-Enumerations_Experimental_IFF.xml | 65,000 | 7.1 |
| 34 | 16:25:12 (20:25:13Z, appNo 4634) | 252238 | Error | MAK-VRLExt-3_evolved.xml    | 5,692  | 158.0 |
| 35 | 16:25:23 (20:25:24Z, appNo 4635) | 253850 | Success | -  (the 8 h holder, pid 68304)  | -      | 11.7  |

appNo attribution for rows 30-35 is the coordinator's D3 harvest (d3_harvest_report.md); rows 1-29
carry no appNo in the rtiexec log and are not attributed here.
