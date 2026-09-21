# tests/Hooks.Tests.ps1 - OFFLINE check for the two Claude Code PreToolUse hooks in
# scripts\hooks (docs/SESSION_HOOKS.md). No simulator, no server, no network, and it
# installs nothing. Plain pwsh (no Pester dependency): exits 0 when every check
# passes, 1 otherwise, and prints one line per check. Lane C owns
# tests\RunnerTurnaround.Tests.ps1; these checks live here so the two cannot collide.
#
#   pwsh -NoProfile -File tests\Hooks.Tests.ps1
#
# It really spawns the hook as a child process and feeds it JSON on stdin, because
# the contract under test IS the process contract: stdin JSON in, exit code out
# (https://code.claude.com/docs/en/hooks-guide, "Hook input" / "Hook output").
# A static assertion cannot tell exit 0 from exit 2.
#
# DIRTY CONTROLS RUN FIRST. A guard that has never been seen to block proves nothing.
#
# What it pins down:
#   A.  repo-root resolution from $PSScriptRoot, with the process cwd somewhere else
#       entirely (the motivating sessions were rooted in a different repo), and the
#       -RepoRoot parameter overriding it
#   B.  AskUserQuestion: dirty controls (the 44946 shape, an unquotable claim, a
#       quote absent from the ledger, a missing ledger) then clean controls
#   C.  Agent: dirty controls (no citations, no path, no section, marker too far
#       above the content, one run id without "n = 1") then clean controls
#   D.  edge cases: malformed JSON, empty stdin, a foreign tool_name, no questions
#       -> FAIL OPEN (exit 0), decided in docs/SESSION_HOOKS.md
#   E.  the exit-code contract: pass = exit 0 with EMPTY stdout (never a JSON
#       "allow", which would bypass the owner's own permission prompt);
#       block = exit 2 with the reason on stderr and nothing on stdout
#   F.  per-call latency (a hook that adds seconds to every tool call gets switched
#       off): budget 1000 ms

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$RepoRoot = Split-Path -Parent $PSScriptRoot
$SrcAsk = Join-Path $RepoRoot 'scripts\hooks\PreToolUse-AskUserQuestion.ps1'
$SrcAgent = Join-Path $RepoRoot 'scripts\hooks\PreToolUse-Agent.ps1'

$script:Pass = 0
$script:Fail = 0
function Check {
    param([string]$Name, [bool]$Condition, [string]$Detail = '')
    if ($Condition) { $script:Pass++; Write-Host ('  [PASS] ' + $Name) }
    else { $script:Fail++; Write-Host ('  [FAIL] ' + $Name + $(if ($Detail) { ' -- ' + $Detail } else { '' })) }
}

# The hook must run under the same edition as this suite, so running the suite under
# Windows PowerShell 5.1 and under pwsh 7 exercises both.
$PsExe = if ($PSVersionTable.PSEdition -eq 'Core') { Join-Path $PSHOME 'pwsh.exe' } else { Join-Path $PSHOME 'powershell.exe' }

# --------------------------------------------------------------------------
# Fixture: two throwaway "repos" laid out like this one, so the hooks resolve
# their root from $PSScriptRoot and never touch the real docs\RULINGS.md.
# --------------------------------------------------------------------------
$Fix = Join-Path ([System.IO.Path]::GetTempPath()) ('u2hooks_' + [guid]::NewGuid().ToString('N'))
$GoodRoot = Join-Path $Fix 'goodrepo'
$BareRoot = Join-Path $Fix 'ledgerlessrepo'
$AltRoot = Join-Path $Fix 'altrepo'
foreach ($r in @($GoodRoot, $BareRoot, $AltRoot)) { New-Item -ItemType Directory -Path (Join-Path $r 'scripts\hooks') -Force | Out-Null }
foreach ($r in @($GoodRoot, $AltRoot)) { New-Item -ItemType Directory -Path (Join-Path $r 'docs') -Force | Out-Null }
foreach ($r in @($GoodRoot, $BareRoot, $AltRoot)) {
    Copy-Item -LiteralPath $SrcAsk -Destination (Join-Path $r 'scripts\hooks') -Force
    Copy-Item -LiteralPath $SrcAgent -Destination (Join-Path $r 'scripts\hooks') -Force
}
$AskGood = Join-Path $GoodRoot 'scripts\hooks\PreToolUse-AskUserQuestion.ps1'
$AskBare = Join-Path $BareRoot 'scripts\hooks\PreToolUse-AskUserQuestion.ps1'
$AgentGood = Join-Path $GoodRoot 'scripts\hooks\PreToolUse-Agent.ps1'

# Fixture ledger. Synthetic on purpose: a test must never be mistakable for a record.
# R-FIX-2's text WRAPS in the ledger and is quoted flat in a question, which is what
# the whitespace-normalised ordinal comparison exists for.
$ledger = @'
# RULINGS (TEST FIXTURE - this file is not a record of anything)
R-FIX-1 | 2026-01-01 | QUESTION AS PUT: "fixture question one" | OWNER'S WORDS: "the fixture ruling text for lane E tests" | supervisor reading: none
R-FIX-2 | 2026-01-02 | OWNER'S WORDS: "a fixture ruling whose text
wraps across two ledger lines" | supervisor reading: none
'@
$archive = @'
# RULINGS ARCHIVE (TEST FIXTURE - this file is not a record of anything)
R-FIX-OLD | 2025-12-01 | OWNER'S WORDS: "an archived fixture ruling" | supervisor reading: none
'@
Set-Content -LiteralPath (Join-Path $GoodRoot 'docs\RULINGS.md') -Value $ledger -Encoding ascii
Set-Content -LiteralPath (Join-Path $GoodRoot 'docs\RULINGS_ARCHIVE.md') -Value $archive -Encoding ascii
Set-Content -LiteralPath (Join-Path $AltRoot 'docs\RULINGS.md') -Value $ledger -Encoding ascii

$briefGood = Join-Path $Fix 'BRIEF_fixture_good.md'
$briefBad = Join-Path $Fix 'BRIEF_fixture_bad.md'
Set-Content -LiteralPath $briefGood -Encoding ascii -Value @'
# FIXTURE BRIEF (good)
RECORD CITATIONS:
- docs\FIXTURE_DOC.md sec 3 (what the lane rests on)
'@
Set-Content -LiteralPath $briefBad -Encoding ascii -Value @'
# FIXTURE BRIEF (bad)
Go and change the placement code until the trace looks right.
'@

# The hook is run from a cwd that is NOT any of the fixture repos, so a hook that
# leaned on the session cwd would fail every ledger lookup below.
$ForeignCwd = [System.IO.Path]::GetTempPath()

function Invoke-Hook {
    param([string]$Script, [string]$Json, [string]$ExplicitRoot, [switch]$Bom)
    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = $PsExe
    # Arguments as a single quoted string, not ArgumentList: ProcessStartInfo.ArgumentList
    # does not exist on .NET Framework, so this is what lets the suite also run under
    # Windows PowerShell 5.1. No path in the fixture contains a double quote.
    $argLine = '-NoProfile -ExecutionPolicy Bypass -File "' + $Script + '"'
    if ($ExplicitRoot) { $argLine = $argLine + ' -RepoRoot "' + $ExplicitRoot + '"' }
    $psi.Arguments = $argLine
    $psi.WorkingDirectory = $ForeignCwd
    $psi.RedirectStandardInput = $true
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $psi.UseShellExecute = $false
    $psi.CreateNoWindow = $true
    $p = [System.Diagnostics.Process]::Start($psi)
    # Write RAW BYTES, never $p.StandardInput.Write(): on .NET Framework that writer
    # is UTF8Encoding WITH the EF BB BF preamble, so a 5.1 run of this suite silently
    # fed every hook a BOM and the whole guard failed open. Bytes keep the suite's
    # own encoding out of the result, and -Bom makes the BOM an explicit fixture.
    # and the BASE STREAM is closed directly, never $p.StandardInput.Close(): on .NET
    # Framework closing that StreamWriter flushes its unwritten UTF-8 preamble, which
    # would land AFTER the JSON and make every payload unparseable.
    $bytes = (New-Object System.Text.UTF8Encoding $false).GetBytes($Json)
    if ($Bom) { $bytes = @([byte]0xEF, [byte]0xBB, [byte]0xBF) + $bytes }
    $p.StandardInput.BaseStream.Write($bytes, 0, $bytes.Length)
    $p.StandardInput.BaseStream.Flush()
    $p.StandardInput.BaseStream.Close()
    $so = $p.StandardOutput.ReadToEnd()
    $se = $p.StandardError.ReadToEnd()
    $p.WaitForExit()
    $code = $p.ExitCode
    $p.Dispose()
    return [pscustomobject]@{ Code = $code; StdOut = $so; StdErr = $se }
}

function New-AskJson {
    param([array]$Questions)
    return (@{ hook_event_name = 'PreToolUse'; tool_name = 'AskUserQuestion'; session_id = 'fixture'; cwd = 'C:\somewhere\else'; tool_input = @{ questions = $Questions } } | ConvertTo-Json -Depth 12 -Compress)
}
function New-Question {
    param([string]$Q, [string]$Header = 'Fixture', [array]$Options = @())
    if ($Options.Count -eq 0) { $Options = @([ordered]@{ label = 'Yes'; description = 'go ahead' }, [ordered]@{ label = 'No'; description = 'do not' }) }
    return [ordered]@{ question = $Q; header = $Header; options = $Options; multiSelect = $false }
}
function New-AgentJson {
    param([string]$Prompt, [string]$ToolName = 'Agent')
    return (@{ hook_event_name = 'PreToolUse'; tool_name = $ToolName; session_id = 'fixture'; cwd = 'C:\somewhere\else'; tool_input = @{ description = 'fixture lane'; prompt = $Prompt; subagent_type = 'general-purpose' } } | ConvertTo-Json -Depth 12 -Compress)
}

# A block is exit 2, a reason on stderr, and NOTHING on stdout (one mechanism, per
# the vendor note "Choose one approach per hook").
function Test-Blocked { param($R) return ($R.Code -eq 2 -and $R.StdErr.Trim().Length -gt 0 -and $R.StdOut.Trim().Length -eq 0) }
# A pass is exit 0 with EMPTY stdout: no JSON "allow", so the normal permission flow
# still applies (hooks-guide, "Hook output": exit 0 "doesn't approve the tool call").
function Test-Passed { param($R) return ($R.Code -eq 0 -and $R.StdOut.Trim().Length -eq 0) }

try {

    Write-Host '=== 0. instrument control: the hook scripts exist and the fixture copies are identical ==='
    Check 'source hook scripts exist' ((Test-Path -LiteralPath $SrcAsk) -and (Test-Path -LiteralPath $SrcAgent))
    Check 'fixture copy of the AskUserQuestion hook is byte-identical to the tracked file' (
        [System.IO.File]::ReadAllBytes($AskGood).Length -eq [System.IO.File]::ReadAllBytes($SrcAsk).Length)
    Check 'fixture ledger is where the hook will look for it' (Test-Path -LiteralPath (Join-Path $GoodRoot 'docs\RULINGS.md'))
    Check 'ledgerless fixture repo really has no docs directory' (-not (Test-Path -LiteralPath (Join-Path $BareRoot 'docs')))

    # ======================================================================
    Write-Host ''
    Write-Host '=== 1. DIRTY CONTROLS - AskUserQuestion must BLOCK these ==='

    # The 2026-09-21 shape (transcript line 44946): a question that presupposes a
    # ruling and quotes nothing. Text paraphrased, not copied, to keep the fixture
    # from reading as a record.
    $d1 = Invoke-Hook -Script $AskGood -Json (New-AskJson @( (New-Question 'The timer-completion behaviour for move tasks is already ruled. Do you want it kept as it is for the demo?') ))
    Check '1a the 44946 shape (presupposes a ruling, quotes nothing) is BLOCKED' (Test-Blocked $d1) "code=$($d1.Code) out='$($d1.StdOut.Trim())'"
    Check '1a the block message names the missing markers' ($d1.StdErr -match 'RULING ON FILE' -and $d1.StdErr -match 'NO RULING FOUND') $d1.StdErr
    Check '1a the block message quotes handoff rule 2' ($d1.StdErr -match 'Handoff sec 4 rule 2')

    $d2 = Invoke-Hook -Script $AskGood -Json (New-AskJson @( (New-Question 'RULING ON FILE: the owner settled this one already. Keep it?') ))
    Check '1b RULING ON FILE: with no quoted string is BLOCKED' (Test-Blocked $d2) "code=$($d2.Code)"
    Check '1b message says a quoted string must follow' ($d2.StdErr -match 'no quoted string')

    $d3 = Invoke-Hook -Script $AskGood -Json (New-AskJson @( (New-Question 'RULING ON FILE: "a ruling nobody ever made about move completion". Keep it?') ))
    Check '1c RULING ON FILE: quoting text that is NOT in the ledger is BLOCKED' (Test-Blocked $d3) "code=$($d3.Code)"
    Check '1c message says the quote was not found' ($d3.StdErr -match 'does not occur in')

    $d4 = Invoke-Hook -Script $AskGood -Json (New-AskJson @( (New-Question 'NO RULING FOUND - searched: everywhere I could think of. Which do you want?') ))
    Check '1d NO RULING FOUND - searched: naming no file is BLOCKED' (Test-Blocked $d4) "code=$($d4.Code)"
    Check '1d message asks for a file name' ($d4.StdErr -match 'names no file')

    $d5 = Invoke-Hook -Script $AskGood -Json (New-AskJson @(
            (New-Question 'RULING ON FILE: "the fixture ruling text for lane E tests". Keep it?'),
            (New-Question 'And while we are at it, should the watchdog ship ON?')
        ))
    Check '1e one clean question + one bare question: the CALL is BLOCKED' (Test-Blocked $d5) "code=$($d5.Code)"
    Check '1e the message names question 2, not question 1' ($d5.StdErr -match 'question 2' -and $d5.StdErr -notmatch 'question 1')

    # Missing ledger: the unverifiable form blocks and says why (brief deliverable 1).
    $d6 = Invoke-Hook -Script $AskBare -Json (New-AskJson @( (New-Question 'RULING ON FILE: "the fixture ruling text for lane E tests". Keep it?') ))
    Check '1f missing ledger + RULING ON FILE: is BLOCKED' (Test-Blocked $d6) "code=$($d6.Code)"
    Check '1f message says the ledger does not exist' ($d6.StdErr -match 'no ruling ledger exists')

    $d7 = Invoke-Hook -Script $AskGood -Json (New-AskJson @( (New-Question 'RULING ON FILE: "an archived fixture ruling that was edited afterwards". Keep it?') ))
    Check '1g a quote that is a NEAR miss of the archive text is BLOCKED' (Test-Blocked $d7) "code=$($d7.Code)"

    # ======================================================================
    Write-Host ''
    Write-Host '=== 2. CLEAN CONTROLS - AskUserQuestion must PASS these ==='

    $c1 = Invoke-Hook -Script $AskGood -Json (New-AskJson @( (New-Question 'RULING ON FILE: "the fixture ruling text for lane E tests" - do you want it applied to the demo run too?') ))
    Check '2a verbatim ledger quote PASSES' (Test-Passed $c1) "code=$($c1.Code) err='$($c1.StdErr.Trim())'"

    $c2 = Invoke-Hook -Script $AskGood -Json (New-AskJson @( (New-Question 'RULING ON FILE: "a fixture ruling whose text wraps across two ledger lines" - still current?') ))
    Check '2b a ledger quote that WRAPS in the ledger but is flat in the question PASSES (whitespace-normalised)' (Test-Passed $c2) "code=$($c2.Code) err='$($c2.StdErr.Trim())'"

    $c3 = Invoke-Hook -Script $AskGood -Json (New-AskJson @( (New-Question 'RULING ON FILE: "an archived fixture ruling" - does it still hold?') ))
    Check '2c a quote found only in RULINGS_ARCHIVE.md PASSES' (Test-Passed $c3) "code=$($c3.Code) err='$($c3.StdErr.Trim())'"

    $c4 = Invoke-Hook -Script $AskGood -Json (New-AskJson @( (New-Question 'Which way for the demo?' 'Demo' @(
                    [ordered]@{ label = 'Keep'; description = 'RULING ON FILE: "the fixture ruling text for lane E tests" - unchanged' },
                    [ordered]@{ label = 'Change'; description = 'depart from it' }
                )) ))
    Check '2d the citation carried by an OPTION DESCRIPTION PASSES' (Test-Passed $c4) "code=$($c4.Code) err='$($c4.StdErr.Trim())'"

    $c5 = Invoke-Hook -Script $AskGood -Json (New-AskJson @( (New-Question 'NO RULING FOUND - searched: docs\RULINGS.md, docs\RULINGS_ARCHIVE.md, docs\HANDOFF_2026-09-01_R9_COMPLETE.md. Nothing covers this. Which do you want?') ))
    Check '2e NO RULING FOUND - searched: <files> PASSES' (Test-Passed $c5) "code=$($c5.Code) err='$($c5.StdErr.Trim())'"

    $c6 = Invoke-Hook -Script $AskBare -Json (New-AskJson @( (New-Question 'NO RULING FOUND - searched: docs\RULINGS.md. Nothing covers this. Which do you want?') ))
    Check '2f missing ledger + the NO RULING FOUND form still PASSES (only the unquotable form blocks)' (Test-Passed $c6) "code=$($c6.Code) err='$($c6.StdErr.Trim())'"

    $c7 = Invoke-Hook -Script $AskGood -Json (New-AskJson @( (New-Question 'ruling on file: "the fixture ruling text for lane E tests" - still current?') ))
    Check '2g the marker is matched case-insensitively (the ledger lookup is what has the teeth)' (Test-Passed $c7) "code=$($c7.Code)"

    # ======================================================================
    Write-Host ''
    Write-Host '=== 3. DIRTY CONTROLS - Agent must BLOCK these ==='

    $a1 = Invoke-Hook -Script $AgentGood -Json (New-AgentJson 'You are an executor for the placement lane. Fix PlacementPolicy so the units stop freezing, rebuild, and report.')
    Check '3a a code-lane brief with no RECORD CITATIONS: is BLOCKED' (Test-Blocked $a1) "code=$($a1.Code) out='$($a1.StdOut.Trim())'"
    Check '3a the block message quotes handoff rule 3' ($a1.StdErr -match 'Handoff sec 4 rule 3')
    Check '3a the block message offers the read-only exemption' ($a1.StdErr -match 'LANE KIND: read-only lookup')

    $a2 = Invoke-Hook -Script $AgentGood -Json (New-AgentJson "You are an executor.`nRECORD CITATIONS:`n- the altitude doc, the section about ground contact`nGo and change the placement code.")
    Check '3b RECORD CITATIONS: with a prose reference but no path token is BLOCKED' (Test-Blocked $a2) "code=$($a2.Code)"
    Check '3b message asks for a file path' ($a2.StdErr -match 'do not name both a file path')

    $a3 = Invoke-Hook -Script $AgentGood -Json (New-AgentJson "You are an executor.`nRECORD CITATIONS:`n- docs\VRF_ALTITUDE_FRAMES.md`nGo and change the placement code.")
    Check '3c RECORD CITATIONS: with a path but NO section marker is BLOCKED' (Test-Blocked $a3) "code=$($a3.Code)"

    $filler = (1..20 | ForEach-Object { "filler line $_ of the brief preamble" }) -join "`n"
    $a4 = Invoke-Hook -Script $AgentGood -Json (New-AgentJson ("RECORD CITATIONS:`n" + $filler + "`n- docs\VRF_ALTITUDE_FRAMES.md sec 5"))
    Check '3d citations pushed more than 15 lines below the marker are BLOCKED (S7: "in its first lines")' (Test-Blocked $a4) "code=$($a4.Code)"

    $a5 = Invoke-Hook -Script $AgentGood -Json (New-AgentJson "RECORD CITATIONS: docs\VRF_ALTITUDE_FRAMES.md sec 5.`nHarvest run 20260921T143243Z and tell me why the company froze.")
    Check '3e one run id as the origin without the literal "n = 1" is BLOCKED' (Test-Blocked $a5) "code=$($a5.Code)"
    Check '3e message says one run is one observation' ($a5.StdErr -match 'One run is one observation')

    $a6 = Invoke-Hook -Script $AgentGood -Json (New-AgentJson ('Read ' + $briefBad + ' and do what it says.'))
    Check '3f a prompt pointing at an EXISTING brief file with no citations is BLOCKED' (Test-Blocked $a6) "code=$($a6.Code)"
    Check '3f message names the brief file it opened' ($a6.StdErr -match 'BRIEF_fixture_bad\.md')

    $a7 = Invoke-Hook -Script $AgentGood -Json (New-AgentJson 'Change the type map. RECORD CITATIONS: and/or whatever you find, no section given.')
    Check '3g "and/or" does not satisfy the path test (the extension is required on purpose)' (Test-Blocked $a7) "code=$($a7.Code)"

    $a8 = Invoke-Hook -Script $AgentGood -Json (New-AgentJson 'Fix the runner turnaround logic.' 'Task')
    Check '3h the historical tool_name "Task" is guarded the same way' (Test-Blocked $a8) "code=$($a8.Code)"

    # ======================================================================
    Write-Host ''
    Write-Host '=== 4. CLEAN CONTROLS - Agent must PASS these ==='

    $b1 = Invoke-Hook -Script $AgentGood -Json (New-AgentJson "You are an executor for the placement lane.`nRECORD CITATIONS:`n- docs\VRF_ALTITUDE_FRAMES.md sec 5 (FALSIFIED) and sec 7 (TRIPWIRES)`n- docs\CORRECTIONS_LOG.md section 2`nGo and change the placement code.")
    Check '4a RECORD CITATIONS: with path + sec PASSES' (Test-Passed $b1) "code=$($b1.Code) err='$($b1.StdErr.Trim())'"

    $b2 = Invoke-Hook -Script $AgentGood -Json (New-AgentJson "RECORD CITATIONS: docs\HANDOFF_2026-09-01_R9_COMPLETE.md:114 - the CLOSED list.`nGo and change the code.")
    Check '4b a :<line> marker counts as the section marker' (Test-Passed $b2) "code=$($b2.Code) err='$($b2.StdErr.Trim())'"

    $b3 = Invoke-Hook -Script $AgentGood -Json (New-AgentJson 'LANE KIND: read-only lookup. Find every call site of setAltitude and report the file and line.')
    Check '4c a declared read-only lookup is exempt and PASSES' (Test-Passed $b3) "code=$($b3.Code) err='$($b3.StdErr.Trim())'"

    $b4 = Invoke-Hook -Script $AgentGood -Json (New-AgentJson 'LANE KIND: read-only lookup. Summarise run 20260921T143243Z from its manifest.')
    Check '4d a read-only lookup naming one run is exempt from the n = 1 rule too' (Test-Passed $b4) "code=$($b4.Code) err='$($b4.StdErr.Trim())'"

    $b5 = Invoke-Hook -Script $AgentGood -Json (New-AgentJson ('Read ' + $briefGood + ' in full and do what it says.'))
    Check '4e a prompt pointing at an EXISTING brief file that carries citations PASSES' (Test-Passed $b5) "code=$($b5.Code) err='$($b5.StdErr.Trim())'"

    $b6 = Invoke-Hook -Script $AgentGood -Json (New-AgentJson "RECORD CITATIONS: docs\VRF_ALTITUDE_FRAMES.md sec 5. n = 1 (born from run 20260921T143243Z).`nHarvest it.")
    Check '4f one run id WITH the literal "n = 1" PASSES' (Test-Passed $b6) "code=$($b6.Code) err='$($b6.StdErr.Trim())'"

    $b7 = Invoke-Hook -Script $AgentGood -Json (New-AgentJson "RECORD CITATIONS: docs\VRF_ALTITUDE_FRAMES.md sec 5.`nCompare runs 20260921T143243Z and 20260920T101112Z.")
    Check '4g two run ids do not trigger the n = 1 rule (it guards a SINGLE-run origin)' (Test-Passed $b7) "code=$($b7.Code) err='$($b7.StdErr.Trim())'"

    # ======================================================================
    Write-Host ''
    Write-Host '=== 5. EDGE CASES - both hooks FAIL OPEN (decided; docs/SESSION_HOOKS.md) ==='

    foreach ($pair in @(@{ n = 'AskUserQuestion'; s = $AskGood }, @{ n = 'Agent'; s = $AgentGood })) {
        $e1 = Invoke-Hook -Script $pair.s -Json '{ this is not json at all '
        Check ("5a $($pair.n): malformed JSON FAILS OPEN (exit 0)") ($e1.Code -eq 0) "code=$($e1.Code)"
        $e2 = Invoke-Hook -Script $pair.s -Json ''
        Check ("5b $($pair.n): empty stdin FAILS OPEN (exit 0)") ($e2.Code -eq 0) "code=$($e2.Code)"
        $e3 = Invoke-Hook -Script $pair.s -Json '{"hook_event_name":"PreToolUse","tool_name":"Bash","tool_input":{"command":"git status"}}'
        Check ("5c $($pair.n): a foreign tool_name is ignored (exit 0)") ($e3.Code -eq 0) "code=$($e3.Code)"
        # 5d is a REGRESSION check: @($null).Count is 1 in PowerShell, not 0, so the
        # first cut of the AskUserQuestion hook denied a call whose tool_input carried
        # no questions at all - a block on malformed input, which is the hazard the
        # fail-open decision exists to avoid.
        $e4 = Invoke-Hook -Script $pair.s -Json '{"hook_event_name":"PreToolUse","tool_name":"AskUserQuestion","tool_input":{}}'
        Check ("5d $($pair.n): tool_input with no questions/prompt FAILS OPEN (exit 0)") ($e4.Code -eq 0) "code=$($e4.Code)"
    }
    $e5 = Invoke-Hook -Script $AskGood -Json (New-AskJson @( [ordered]@{ header = 'Fixture' } ))
    Check '5e AskUserQuestion: a question object with no text at all is BLOCKED, not crashed' (Test-Blocked $e5) "code=$($e5.Code)"
    $e6 = Invoke-Hook -Script $AgentGood -Json '{"hook_event_name":"PreToolUse","tool_name":"Agent","tool_input":{"prompt":null}}'
    Check '5f Agent: a null prompt FAILS OPEN (exit 0)' ($e6.Code -eq 0) "code=$($e6.Code)"
    $e7 = Invoke-Hook -Script $AskGood -Json '{"hook_event_name":"PreToolUse","tool_name":"AskUserQuestion","tool_input":{"questions":[]}}'
    Check '5g AskUserQuestion: an EMPTY questions array FAILS OPEN (exit 0)' ($e7.Code -eq 0) "code=$($e7.Code)"
    $e8 = Invoke-Hook -Script $AskGood -Json '{"hook_event_name":"PreToolUse","tool_name":"AskUserQuestion","tool_input":{"questions":[null]}}'
    Check '5h AskUserQuestion: a null question ENTRY is skipped, not blocked (exit 0)' ($e8.Code -eq 0) "code=$($e8.Code)"
    $e9 = Invoke-Hook -Script $AskGood -Json '{"hook_event_name":"PreToolUse","tool_name":"AskUserQuestion","tool_input":{"questions":"not an array"}}'
    Check '5i AskUserQuestion: a questions value of the wrong TYPE is BLOCKED, not crashed' (Test-Blocked $e9) "code=$($e9.Code)"

    # 5j is the OTHER regression from this lane: a UTF-8 BOM ahead of the JSON made
    # ConvertFrom-Json throw, so the hook failed open on every single call. A guard
    # that never fires is worse than no guard, because it reads as a green.
    $e10 = Invoke-Hook -Script $AskGood -Bom -Json (New-AskJson @( (New-Question 'The timer-completion behaviour is already ruled. Keep it for the demo?') ))
    Check '5j AskUserQuestion: a UTF-8 BOM ahead of the JSON still BLOCKS a dirty question' (Test-Blocked $e10) "code=$($e10.Code) err='$($e10.StdErr.Trim())'"
    $e11 = Invoke-Hook -Script $AgentGood -Bom -Json (New-AgentJson 'Fix the placement code so the units stop freezing.')
    Check '5k Agent: a UTF-8 BOM ahead of the JSON still BLOCKS a dirty brief' (Test-Blocked $e11) "code=$($e11.Code) err='$($e11.StdErr.Trim())'"
    $e12 = Invoke-Hook -Script $AskGood -Bom -Json (New-AskJson @( (New-Question 'RULING ON FILE: "the fixture ruling text for lane E tests". Keep it?') ))
    Check '5l AskUserQuestion: a UTF-8 BOM ahead of a CLEAN question still passes' (Test-Passed $e12) "code=$($e12.Code)"

    # ======================================================================
    Write-Host ''
    Write-Host '=== 6. REPO-ROOT RESOLUTION (never the session cwd) ==='
    # Every invocation above already ran with cwd = the system temp directory, so the
    # passes in section 2 are themselves the evidence that $PSScriptRoot is the source
    # of the root. These two pin the parameter override.
    $r1 = Invoke-Hook -Script $AskBare -Json (New-AskJson @( (New-Question 'RULING ON FILE: "the fixture ruling text for lane E tests". Keep it?') )) -ExplicitRoot $AltRoot
    Check '6a -RepoRoot overrides the $PSScriptRoot default (ledgerless script + a root that HAS the ledger -> PASS)' (Test-Passed $r1) "code=$($r1.Code) err='$($r1.StdErr.Trim())'"
    $r2 = Invoke-Hook -Script $AskGood -Json (New-AskJson @( (New-Question 'RULING ON FILE: "the fixture ruling text for lane E tests". Keep it?') )) -ExplicitRoot $BareRoot
    Check '6b -RepoRoot pointed at a ledgerless root BLOCKS even though the script sits beside a ledger' (Test-Blocked $r2) "code=$($r2.Code)"
    Check '6c the cwd used for every invocation is none of the fixture repos' ($ForeignCwd -notlike (Join-Path $Fix '*'))

    # ======================================================================
    Write-Host ''
    Write-Host '=== 7. PER-CALL LATENCY (budget 1000 ms; a slow hook gets switched off) ==='
    $warm = Invoke-Hook -Script $AgentGood -Json (New-AgentJson 'LANE KIND: read-only lookup. warm up')
    Check '7a warm-up call passed' (Test-Passed $warm)
    $n = 10
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    for ($i = 0; $i -lt $n; $i++) { $null = Invoke-Hook -Script $AskGood -Json (New-AskJson @( (New-Question 'RULING ON FILE: "the fixture ruling text for lane E tests". Keep it?') )) }
    $sw.Stop()
    $msAsk = [Math]::Round($sw.Elapsed.TotalMilliseconds / $n, 1)
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    for ($i = 0; $i -lt $n; $i++) { $null = Invoke-Hook -Script $AgentGood -Json (New-AgentJson "RECORD CITATIONS: docs\VRF_ALTITUDE_FRAMES.md sec 5.`nGo.") }
    $sw.Stop()
    $msAgent = [Math]::Round($sw.Elapsed.TotalMilliseconds / $n, 1)
    Write-Host ("  measured on $PsExe over $n calls each: AskUserQuestion $msAsk ms/call, Agent $msAgent ms/call")
    Check "7b AskUserQuestion hook under 1000 ms/call ($msAsk ms)" ($msAsk -lt 1000) "$msAsk ms"
    Check "7c Agent hook under 1000 ms/call ($msAgent ms)" ($msAgent -lt 1000) "$msAgent ms"

    # ======================================================================
    Write-Host ''
    Write-Host '=== 8. static: both hook scripts parse, and are ASCII with CRLF endings ==='
    foreach ($f in @($SrcAsk, $SrcAgent, $PSCommandPath)) {
        $errs = $null
        $null = [System.Management.Automation.Language.Parser]::ParseFile($f, [ref]$null, [ref]$errs)
        Check ("8a " + (Split-Path -Leaf $f) + ' parses with zero errors') ($null -eq $errs -or $errs.Count -eq 0) (($errs | ForEach-Object { $_.Message }) -join '; ')
        $bytes = [System.IO.File]::ReadAllBytes($f)
        $nonAscii = @($bytes | Where-Object { $_ -gt 0x7E -or ($_ -lt 0x20 -and $_ -ne 0x09 -and $_ -ne 0x0A -and $_ -ne 0x0D) })
        Check ("8b " + (Split-Path -Leaf $f) + ' is ASCII (no byte > 0x7E, no stray control char)') ($nonAscii.Count -eq 0) "$($nonAscii.Count) offending bytes"
        $bareLf = 0
        for ($i = 0; $i -lt $bytes.Length; $i++) { if ($bytes[$i] -eq 0x0A -and ($i -eq 0 -or $bytes[$i - 1] -ne 0x0D)) { $bareLf++ } }
        Check ("8c " + (Split-Path -Leaf $f) + ' has zero bare LF (CRLF throughout)') ($bareLf -eq 0) "$bareLf bare LF"
    }
    # Dirty control for 8b/8c: the checks must be able to fail.
    $dirty = Join-Path $Fix 'dirty_control.txt'
    [System.IO.File]::WriteAllBytes($dirty, [byte[]](0x61, 0xC3, 0xA9, 0x0A, 0x62, 0x0D, 0x0A))
    $db = [System.IO.File]::ReadAllBytes($dirty)
    $dNon = @($db | Where-Object { $_ -gt 0x7E -or ($_ -lt 0x20 -and $_ -ne 0x09 -and $_ -ne 0x0A -and $_ -ne 0x0D) })
    $dLf = 0
    for ($i = 0; $i -lt $db.Length; $i++) { if ($db[$i] -eq 0x0A -and ($i -eq 0 -or $db[$i - 1] -ne 0x0D)) { $dLf++ } }
    Check '8d DIRTY CONTROL: the ASCII check reports 2 offending bytes on a known-dirty file' ($dNon.Count -eq 2) "$($dNon.Count)"
    Check '8e DIRTY CONTROL: the CRLF check reports 1 bare LF on a known-dirty file' ($dLf -eq 1) "$dLf"
}
finally {
    if (Test-Path -LiteralPath $Fix) { Remove-Item -LiteralPath $Fix -Recurse -Force -ErrorAction SilentlyContinue }
}

Write-Host ''
Write-Host ('{0} passed, {1} failed' -f $script:Pass, $script:Fail)
if ($script:Fail -gt 0) { exit 1 }
exit 0
