# SESSION_HOOKS.md - the two PreToolUse guards in scripts\hooks (BUILT AND TESTED, NOT INSTALLED)

INSTALLATION IS THE OWNER'S DECISION. Nothing here installs them; no settings.json was created or edited. Test:
`pwsh -NoProfile -File tests\Hooks.Tests.ps1` (78 checks, dirty controls first; green under pwsh 7 and Windows PowerShell 5.1).
Vendor contract retrieved 2026-09-21: https://code.claude.com/docs/en/hooks-guide secs "Hook input", "Hook output",
"Structured JSON output", "Configure hook location"; also /docs/en/hooks; tool names from /docs/en/tools.

## What they enforce
1. `PreToolUse-AskUserQuestion.ps1` - handoff sec 4 rule 2. A question put to the owner must carry
   `RULING ON FILE: "<text occurring verbatim in docs\RULINGS.md or docs\RULINGS_ARCHIVE.md>"` or
   `NO RULING FOUND - searched: <at least one file name>`, in the question text or in an option label or description.
   Compared ordinal, whitespace-normalised. BOTH LEDGER FILES ARE SEARCHED - the ledger is two files and an archived
   id is as real as a live one (test 2c pins a quote that exists ONLY in docs\RULINGS_ARCHIVE.md). With no ledger on
   disk only the first form is blocked: nothing can be quoted from a ledger that does not exist.
2. `PreToolUse-Agent.ps1` - handoff sec 4 rule 3 and AUDIT S7. A brief must carry a `RECORD CITATIONS:` block naming a
   file path AND a section (`sec` / `section` / `:<line>`) within 15 lines, or point at an existing `.md` brief that
   does; and if it names exactly one run id as its origin, the literal `n = 1`. `LANE KIND: read-only lookup` exempts
   a search lane.

Both resolve the guarded repo from `$PSScriptRoot`, never the session cwd, so they still fire when a session is rooted
in another repo - which is how the motivating failures happened. `-RepoRoot` overrides. Neither hook reads the
transcript, the network, or anything under C:\MAK.

## Fail-open vs fail-closed (decided)
BLOCK - exit 2, reason on stderr - when the input parsed and the citation is missing or unverifiable. FAIL OPEN -
exit 0, nothing on stdout - on empty or non-JSON stdin, a foreign tool_name, a missing questions/prompt field, or any
internal error. A hook that wedges every question on a parse error is its own hazard, and that block would be
unactionable: no citation the model could add would clear it. A pass prints NOTHING; a JSON `permissionDecision:
"allow"` would silently bypass the owner's own permission prompt.

## Install (owner only). User level is the one that matters here
A session rooted in another repo does not load THIS repo's `.claude\settings.json`, so put the block in
`C:\Users\<you>\.claude\settings.json` under `"hooks"`, in the same invocation style as the `UserPromptSubmit` hook
already on this box:

    "PreToolUse": [
      { "matcher": "AskUserQuestion",
        "hooks": [ { "type": "command", "command": "C:\\Program Files\\PowerShell\\7\\pwsh.exe",
          "args": ["-NoProfile", "-ExecutionPolicy", "Bypass", "-File",
            "<REPO>\\scripts\\hooks\\PreToolUse-AskUserQuestion.ps1"], "timeout": 10 } ] },
      { "matcher": "Agent|Task",
        "hooks": [ { "type": "command", "command": "C:\\Program Files\\PowerShell\\7\\pwsh.exe",
          "args": ["-NoProfile", "-ExecutionPolicy", "Bypass", "-File",
            "<REPO>\\scripts\\hooks\\PreToolUse-Agent.ps1"], "timeout": 10 } ] }
    ]

Project level is the same block in `<REPO>\.claude\settings.json` with `${CLAUDE_PROJECT_DIR}` in place of `<REPO>`, but
it fires only for sessions STARTED in this repo, so it misses the cross-repo case above. It is the weaker of the two.

EDITION: give the FULL path to the x64 pwsh 7, `C:\Program Files\PowerShell\7\pwsh.exe`. A bare `pwsh` resolves to the
32-bit build on this box and nearly doubles the cost. Measured 2026-09-21, 10 calls per figure: pwsh 7 x64 448-462
ms/call, Windows PowerShell 5.1 x64 432-491, 32-bit 5.1 855-905. Both guarded tools are low-frequency, so this is
per question or per agent spawn, never per Read or Bash. `"Agent|Task"` is an EXACT-name alternation (`Agent` is the
current name, `Task` the historical one). Never use a regex such as `"Task.*"`: it would also catch TaskCreate,
TaskGet, TaskList, TaskOutput, TaskStop and TaskUpdate.

## Bypass and uninstall
Bypass honestly, never silently: add `NO RULING FOUND - searched: <files>` to the question, or
`LANE KIND: read-only lookup` to a search brief. A hard bypass means removing the block. `/hooks` is a read-only
browser - "to add, modify, or remove hooks, edit your settings JSON directly". Uninstall = delete the PreToolUse
entries you added; the scripts do nothing at all until they are registered.
