# scripts/hooks/PreToolUse-Agent.ps1
#
# Claude Code PreToolUse hook for the agent-spawning tool. It enforces handoff
# sec 4 rule 3: "A brief for a code or harvest lane carries a 'RECORD CITATIONS:'
# block (file + section) for its domain, and 'n = 1' if it was born from one run."
# Audit S7: a lane brief born from a single run must carry "n = 1" in its first lines.
#
# NOT INSTALLED by this file. See docs/SESSION_HOOKS.md for the settings.json block.
#
# TOOL NAME: the agent-spawning tool is named "Agent" in current Claude Code
# (https://code.claude.com/docs/en/tools, tool table: "Agent - Spawns a subagent with
# its own context window to handle a task"). There is no "Task" tool on that list; the
# name is historical. This script accepts BOTH tool_name values so it keeps working
# either way, and the recommended matcher is the exact alternation "Agent|Task".
# Do NOT use a regex matcher like "Task.*" - it would also catch TaskCreate,
# TaskGet, TaskList, TaskOutput, TaskStop and TaskUpdate, which are unrelated.
#
# VENDOR CONTRACT (retrieved 2026-09-21):
#   https://code.claude.com/docs/en/hooks-guide sec "Hook input":
#     "tool_name: the tool Claude is about to use"; "tool_input: the arguments Claude
#     passed to the tool." The JSON arrives on stdin.
#   https://code.claude.com/docs/en/hooks-guide sec "Hook output":
#     "Exit 2: Claude Code blocks the action. Write a reason to stderr."
#     "Exit 0: your hook reports no objection through its exit code. For a PreToolUse
#     hook this doesn't approve the tool call: the normal permission flow still
#     applies."
#   https://code.claude.com/docs/en/hooks-guide sec "Structured JSON output":
#     "Use exit 2 to block with a stderr message, or exit 0 with JSON for structured
#     control. Choose one approach per hook."
#   THIS HOOK USES EXIT 2 + STDERR, and prints nothing at all on a pass.
#
# FAIL-OPEN / FAIL-CLOSED (decided; see docs/SESSION_HOOKS.md):
#   fail CLOSED (exit 2) when the input parsed, a prompt is present and the citations
#   are missing; fail OPEN (exit 0) when stdin is empty, is not JSON, carries another
#   tool_name, has no prompt, or the script itself throws.
#
# Usage (stdin is the hook JSON):
#   pwsh -NoProfile -File scripts\hooks\PreToolUse-Agent.ps1 [-RepoRoot <path>]
#
# -RepoRoot defaults to the repo this script lives in, NOT the session cwd: the
# motivating sessions were rooted in a different repo (the frozen C++ oracle). It is
# used only to resolve a RELATIVE brief path named in the prompt.

[CmdletBinding()]
param(
    [string]$RepoRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$script:Marker = 'PreToolUse-Agent'
$script:RuleQuote = "A brief for a code or harvest lane carries a 'RECORD CITATIONS:' block (file + section) for its domain, and 'n = 1' if it was born from one run."

# The 15-line window after the RECORD CITATIONS: marker (S7: "in its first lines").
$script:WindowLines = 15

function Get-Prop {
    param($Object, [string]$Name)
    if ($null -eq $Object) { return $null }
    if ($Object -is [System.Collections.IDictionary]) {
        if ($Object.Contains($Name)) { return $Object[$Name] }
        return $null
    }
    $p = $Object.PSObject.Properties[$Name]
    if ($null -eq $p) { return $null }
    return $p.Value
}

# A path-like token: an optional drive letter, optional directory segments, and a
# final name WITH an extension. The extension is required on purpose - without it
# "and/or" and "read-only" satisfy a bare separator test.
$script:PathToken = '(?:[A-Za-z]:[\\/])?(?:[A-Za-z0-9_.\-]+[\\/])*[A-Za-z0-9_\-]+\.[A-Za-z0-9]{1,6}(?![A-Za-z0-9])'
$script:SectionToken = '(?:\bsec\b|\bsecs\b|\bsection\b|\bsections\b|:\d+)'

# True when $Text carries a RECORD CITATIONS: block whose first 15 lines name at
# least one path-like token AND at least one section marker.
function Test-RecordCitations {
    param([string]$Text)
    if ([string]::IsNullOrWhiteSpace($Text)) { return $false }
    $m = [regex]::Match($Text, 'RECORD\s+CITATIONS\s*:', 'IgnoreCase')
    if (-not $m.Success) { return $false }

    $tail = $Text.Substring($m.Index)
    $lines = [regex]::Split($tail, '\r?\n')
    $last = [Math]::Min($script:WindowLines, $lines.Count - 1)
    $window = ($lines[0..$last]) -join "`n"

    $hasPath = [regex]::IsMatch($window, $script:PathToken)
    $hasSection = [regex]::IsMatch($window, $script:SectionToken, 'IgnoreCase')
    return ($hasPath -and $hasSection)
}

# Any .md path named in the prompt that actually exists on disk, resolved absolute
# first and then relative to -RepoRoot. At most 5, and nothing larger than 1 MB, so
# the hook cannot be turned into a slow file crawl.
function Get-ExistingBriefFile {
    param([string]$Text, [string]$Root)
    $found = New-Object System.Collections.Generic.List[string]
    foreach ($m in [regex]::Matches($Text, '(?:[A-Za-z]:[\\/])?(?:[A-Za-z0-9_.\-]+[\\/])*[A-Za-z0-9_\-]+\.md(?![A-Za-z0-9])', 'IgnoreCase')) {
        if ($found.Count -ge 5) { break }
        foreach ($cand in @($m.Value, (Join-Path $Root $m.Value))) {
            try {
                if (Test-Path -LiteralPath $cand -PathType Leaf) {
                    $fi = Get-Item -LiteralPath $cand
                    if ($fi.Length -le 1MB -and -not $found.Contains($fi.FullName)) { $found.Add($fi.FullName) }
                    break
                }
            }
            catch { }
        }
    }
    return $found
}

function Write-HookDeny {
    param([string]$Reason)
    [Console]::Error.Write($Reason)
    exit 2
}

# ----------------------------------------------------------------------------
$raw = ''
try {
    if ([Console]::IsInputRedirected) { $raw = [Console]::In.ReadToEnd() }
    # Trim to the outermost { ... }. The hook input is documented to be a JSON
    # OBJECT, and bytes on either side of it must not disable the guard. On .NET
    # Framework the redirected StandardInput writer is UTF8Encoding WITH the EF BB BF
    # preamble (pwsh 7's is not), so a producer on that path emits a BOM, and it is
    # NOT enough to strip U+FEFF: [Console]::In decodes those three bytes through the
    # console input codepage, so they arrive as whatever cp437/cp1252 makes of them.
    # Without this the hook fails open on EVERY call - a silent false green, the
    # worst outcome available to a guard. Measured 2026-09-21; tests check 5j-5l.
    if (-not [string]::IsNullOrEmpty($raw)) {
        $b = $raw.IndexOf('{')
        $e = $raw.LastIndexOf('}')
        if ($b -ge 0 -and $e -gt $b) { $raw = $raw.Substring($b, $e - $b + 1) }
    }
}
catch {
    $raw = ''
}

try {
    if ([string]::IsNullOrWhiteSpace($raw)) { exit 0 }               # fail open

    $payload = $null
    try { $payload = $raw | ConvertFrom-Json } catch { $payload = $null }
    if ($null -eq $payload) {
        [Console]::Error.Write("$($script:Marker): stdin was not JSON; failing open.")
        exit 0                                                        # fail open
    }

    $toolName = [string](Get-Prop $payload 'tool_name')
    if ($toolName -ne 'Agent' -and $toolName -ne 'Task') { exit 0 }   # not ours

    if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
        $RepoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
    }

    $toolInput = Get-Prop $payload 'tool_input'
    $prompt = [string](Get-Prop $toolInput 'prompt')
    if ([string]::IsNullOrWhiteSpace($prompt)) { exit 0 }             # nothing to judge

    # Handoff rule 3 names "code or harvest" lanes. A declared read-only lookup is
    # outside that scope and is exempt outright, including from the n = 1 rule.
    if ([regex]::IsMatch($prompt, 'LANE\s+KIND\s*:\s*read-only\s+lookup', 'IgnoreCase')) { exit 0 }

    $reasons = New-Object System.Collections.Generic.List[string]

    $citedInPrompt = Test-RecordCitations -Text $prompt
    $citedInBrief = $false
    $briefsSeen = New-Object System.Collections.Generic.List[string]
    if (-not $citedInPrompt) {
        foreach ($bf in (Get-ExistingBriefFile -Text $prompt -Root $RepoRoot)) {
            $briefsSeen.Add($bf)
            $body = ''
            try { $body = Get-Content -LiteralPath $bf -Raw -ErrorAction Stop } catch { $body = '' }
            if (Test-RecordCitations -Text $body) { $citedInBrief = $true; break }
        }
    }

    if (-not $citedInPrompt -and -not $citedInBrief) {
        if ([regex]::IsMatch($prompt, 'RECORD\s+CITATIONS\s*:', 'IgnoreCase')) {
            $reasons.Add("'RECORD CITATIONS:' is present but its first $($script:WindowLines) lines do not name both a file path (a token with an extension, e.g. docs\HANDOFF_2026-09-01_R9_COMPLETE.md) and a section (sec / section / :<line>)")
        }
        elseif ($briefsSeen.Count -gt 0) {
            $reasons.Add("the prompt names brief file(s) " + ($briefsSeen -join ', ') + " but neither the prompt nor those files carry a 'RECORD CITATIONS:' block with a file path and a section")
        }
        else {
            $reasons.Add("the prompt carries no 'RECORD CITATIONS:' block and names no existing brief .md file that carries one")
        }
    }

    # Audit S7: one run is one observation. If exactly one run id is named as the
    # origin, the brief must say so in the literal the record uses.
    $runIds = @([regex]::Matches($prompt, '\b\d{8}T\d{6}Z\b') | ForEach-Object { $_.Value } | Sort-Object -Unique)
    if ($runIds.Count -eq 1 -and -not [regex]::IsMatch($prompt, '\bn\s*=\s*1\b', 'IgnoreCase')) {
        $reasons.Add("the prompt names exactly one run ($($runIds[0])) as its origin but does not carry the literal 'n = 1'. One run is one observation, never a cause")
    }

    if ($reasons.Count -gt 0) {
        $msg = "BLOCKED by $($script:Marker). Handoff sec 4 rule 3: " + '"' + $script:RuleQuote + '"' +
        " A lane that edits code or harvests a run inherits whatever frame its brief carries, so the brief has to name the record it rests on before the executor can rebuild it from a guess. Failing: " +
        ($reasons -join ' | ') +
        ". Fix: add a 'RECORD CITATIONS:' block to the prompt (or to the brief .md it points at) naming file + section for this lane's domain - e.g. 'RECORD CITATIONS: docs\VRF_ALTITUDE_FRAMES.md sec 5 and sec 7' - add 'n = 1' if the lane was born from a single run, or declare 'LANE KIND: read-only lookup' if this is a search rather than a code or harvest lane."
        Write-HookDeny -Reason $msg
    }

    exit 0
}
catch {
    [Console]::Error.Write("$($script:Marker): internal error, failing open: " + $_.Exception.Message)
    exit 0                                                            # fail open
}
