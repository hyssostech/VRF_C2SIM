VR-Forces 5.2 vendor PDFs (git-ignored, ~335 MB). Re-fetch from the PUBLIC MAK library:
  https://docs.mak.com/support/   (open directory listing; every product version)

  VR-Forces_5.2_Users_Guide.pdf              1806 pp
  VR-Forces_5.2_First_Experience_Guide.pdf     90 pp
  VR-Forces_5.2_Migration_Guide.pdf            90 pp
  VR-Forces_5.2_Quick_Reference_Card.pdf       26 pp
  VR-Forces_5.2_Release_Notes.pdf              84 pp
  VR-Forces_5.1.1_Entity_Catalog.pdf         1326 pp (no VR-Forces 5.2 entity catalog online)
  MAK_ONE_2025_Model_Catalog.pdf              680 pp (added 2026-09-25; MAK ONE 2025 catalog)
  MAK_ONE_2025_Interoperability_Guide.pdf      96 pp (added 2026-09-25)

Page index, sha256 and revisions: mak-5.2/INDEX.md. Text extracts for rg: mak-5.2/txt/
(git-ignored; regenerate with pypdf, one "=== PDFPAGE n LABEL n ===" marker per page).
Developer Guide + class reference (HTML): https://docs.mak.com/api/vrforces5.2/classref/index.html

State of the local install, re-checked 2026-09-25: C:\MAK\vrforces5.2d\doc holds 120,690-byte
placeholders ("Documentation Has Not Been Installed") for every PDF except VRF5.2ReleaseNotes.pdf,
and help\ and classdoc\ are stubs. (A 2026-09-02 note here said the documentation installer had
filled that tree and left the old stubs in vrforces5.2d\doc0; neither holds any more - there is no
doc0.) This folder is therefore the working copy of the PDFs. Control: the public Release Notes
here is byte-identical to the installed VRF5.2ReleaseNotes.pdf (sha256 81b402e4...72800).
