# tools/scenario - scenario files derived from STP exports

- `derive_ironstorm_cuta.py` - derives cut A (`data/IRONSTORM_CUTA_*`) from the Iron Storm narrative-1 export.
  `--check` proves the committed files still match the derivation. Change list: `data/IRONSTORM_CUTA_CHANGES.md`.
  `--without i` writes the one registered variant, `data/IRONSTORM_CUTA_E1_Order.xml` (change (i) left out, for
  run E1); every other letter is refused, and `--check` verifies both order files.
  `--variant full` writes `data/IRONSTORM_FULL_Order.xml` (every export task but T12, the same changes; no init of its
  own - the export's is used). `--check` verifies it too. Change list and per-task prediction:
  `data/IRONSTORM_FULL_CHANGES.md`.

## Where the Iron Storm narrative-1 export came from

`data/STP-IRON-STORM-SYNTHETIC_Initialization.xml` (sha256 2000e856...3eec) and `data/STP-IRON-STORM-SYNTHETIC_Order.xml`
(sha256 33dc899c...a5b4) are STP's C2SIM export of the Iron Storm scenario, narrative 1. They were characterised on
2026-09-20 in the Iron Storm session's scratch report `validation\ironstorm_export_report.md` (line 1: "Iron Storm
(narrative 1) - C2SIM export characterised against the VRF interface") and committed in e8d6df9 (2026-09-20).
What the record holds about how they were produced:

- The export's own stamps: ScenarioSetting DateTime 2026-09-20T20:00:27Z, order IssuedTime 2026-09-20T20:00:45Z,
  SystemName "Not Set" (STP-847; run with `--client-id "Not Set"`), 40 units, 23 tasks (2 of STP's 25 dropped by
  the connector, report sec 7.1), Durations as `PT20M` (STP-848; cut A rewrites them).
- "SYNTHETIC" = the 2026-08-25 CoaRenderer render on the IRONSTORM_plus_missing baseline (Iron Storm demo plan).
- The STP operation sources are two `.op` files: `STP-IRON-STORM-SYNTHETIC_Initialization.op` (123,817 B, sha256
  36ef3ae5...e459) and `STP-IRON-STORM-SYNTHETIC_Narrative1.op` (222,720 B, sha256 55f3fd0e...cfc).
- The STP UI steps of the export itself are NOT recorded anywhere. Re-exporting means redoing them by hand and
  checking the result against the two hashes above.

The `.op` files are NOT in the repo. The originals live only in the 09-20 session's scratch folder
(`...\a7f6a276-7ebc-4507-ac9d-c6bd361bd64e\scratchpad\ironstorm\`). A byte-identical copy was made on 2026-09-26 to
`D:\C2SIM-preserve\ironstorm-narrative1\` so they survive a scratch cleanup or a machine rebuild.
