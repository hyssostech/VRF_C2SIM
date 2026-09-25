# scripts/hooks/PreToolUse-AskUserQuestion.ps1
#
# Claude Code PreToolUse hook for the AskUserQuestion tool. It enforces handoff
# sec 4 rule 2: "A question to the owner contains 'RULING ON FILE:' + a quote that
# exists in RULINGS.md or RULINGS_ARCHIVE.md, or 'NO RULING FOUND - searched: <files>'."
# Motivating case: the 2026-09-21 question (transcript line 44946) presupposed a
# ruling that did not exist. It could not have quoted one, so it would be blocked.
#
# NOT INSTALLED by this file. See docs/SESSION_HOOKS.md for the settings.json block.
#
# VENDOR CONTRACT (retrieved 2026-09-21):
#   https://code.claude.com/docs/en/hooks-guide sec "Hook input":
#     "tool_name: the tool Claude is about to use"; "tool_input: the arguments Claude
#     passed to the tool." The JSON arrives on stdin.
#   https://code.claude.com/docs/en/hooks-guide sec "Hook output":
#     "Exit 2: Claude Code blocks the action. Write a reason to stderr."
#     "Exit 0: your hook reports no objection through its exit code. For a PreToolUse
#     hook this doesn't approve the tool call: the normal permission flow still
#     applies." Any other exit code does not block.
#   https://code.claude.com/docs/en/hooks-guide sec "Structured JSON output":
#     "Use exit 2 to block with a stderr message, or exit 0 with JSON for structured
#     control. Choose one approach per hook."
#   THIS HOOK USES EXIT 2 + STDERR, and prints nothing at all on a pass. Emitting a
#   JSON permissionDecision "allow" would silently bypass the owner's own permission
#   prompt; exit 0 with no output leaves the normal permission flow untouched.
#
# FAIL-OPEN / FAIL-CLOSED (decided; see docs/SESSION_HOOKS.md):
#   fail CLOSED (exit 2) when the input parsed and the citation is missing or
#   unverifiable - that is the whole point of the guard;
#   fail OPEN (exit 0) when stdin is empty, is not JSON, carries another tool_name,
#   or the script itself throws. A hook that wedges every question on a parse error
#   is its own hazard, and such a block is unactionable: no citation the model can
#   add would clear it.
#
# Usage (stdin is the hook JSON):
#   pwsh -NoProfile -File scripts\hooks\PreToolUse-AskUserQuestion.ps1 [-RepoRoot <path>]
#
# -RepoRoot defaults to the repo this script lives in, NOT the session cwd: the
# motivating sessions were rooted in a different repo (the frozen C++ oracle).

[CmdletBinding()]
param(
    [string]$RepoRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$script:Marker = 'PreToolUse-AskUserQuestion'
$script:RuleQuote = "A question to the owner contains 'RULING ON FILE:' + a quote that exists in RULINGS.md or RULINGS_ARCHIVE.md, or 'NO RULING FOUND - searched: <files>'."

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

function ConvertTo-NormalizedText {
    param([string]$Text)
    if ([string]::IsNullOrEmpty($Text)) { return '' }
    return ([regex]::Replace($Text, '\s+', ' ')).Trim()
}

# Block per the vendor contract: exit 2, reason on stderr, nothing on stdout.
function Write-HookDeny {
    param([string]$Reason)
    [Console]::Error.Write($Reason)
    exit 2
}

# Returns $true when this question carries an acceptable citation; otherwise $false
# with one or more human-readable reasons appended to $Reasons.
function Test-QuestionCitation {
    param(
        [string]$Text,
        [string[]]$LedgerPaths,
        [System.Collections.Generic.List[string]]$Reasons
    )
    if ([string]::IsNullOrWhiteSpace($Text)) {
        $Reasons.Add('the question carries no text at all')
        return $false
    }

    # Form 1: RULING ON FILE: "<verbatim ledger text>"
    $mOn = [regex]::Match($Text, 'RULING\s+ON\s+FILE\s*:', 'IgnoreCase')
    if ($mOn.Success) {
        $tail = $Text.Substring($mOn.Index + $mOn.Length)
        $mQ = [regex]::Match($tail, '"([^"]{4,})"')
        if (-not $mQ.Success) { $mQ = [regex]::Match($tail, "'([^']{4,})'") }
        if (-not $mQ.Success) {
            $Reasons.Add("'RULING ON FILE:' is present but no quoted string of 4 or more characters follows it (use straight ASCII quotes)")
        }
        else {
            $quote = ConvertTo-NormalizedText $mQ.Groups[1].Value
            $present = @($LedgerPaths | Where-Object { Test-Path -LiteralPath $_ -PathType Leaf })
            if ($present.Count -eq 0) {
                $Reasons.Add("'RULING ON FILE:' cannot be verified: no ruling ledger exists yet (looked for " + ($LedgerPaths -join ' and ') + "). Nothing can be quoted from a ledger that does not exist")
            }
            else {
                foreach ($lp in $present) {
                    $ledger = ConvertTo-NormalizedText (Get-Content -LiteralPath $lp -Raw -ErrorAction SilentlyContinue)
                    if ($ledger.IndexOf($quote, [System.StringComparison]::Ordinal) -ge 0) { return $true }
                }
                $short = $quote
                if ($short.Length -gt 90) { $short = $short.Substring(0, 90) + '...' }
                $Reasons.Add("the quoted string '" + $short + "' does not occur in " + ($present -join ' or ') + " (compared ordinal, whitespace-normalised)")
            }
        }
    }

    # Form 2: NO RULING FOUND - searched: <files>
    $mNo = [regex]::Match($Text, 'NO\s+RULING\s+FOUND\s*-\s*SEARCHED\s*:', 'IgnoreCase')
    if ($mNo.Success) {
        $tail = $Text.Substring($mNo.Index + $mNo.Length)
        if ([regex]::IsMatch($tail, '(?:[A-Za-z]:)?[A-Za-z0-9_.\-\\/]*[A-Za-z0-9_\-]\.[A-Za-z0-9]{1,6}\b')) { return $true }
        $Reasons.Add("'NO RULING FOUND - searched:' is present but names no file (give at least one file name with an extension, e.g. docs\RULINGS.md)")
    }

    if (-not $mOn.Success -and -not $mNo.Success) {
        $Reasons.Add("neither 'RULING ON FILE:' nor 'NO RULING FOUND - searched:' appears in the question text or in any option label or description")
    }
    return $false
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
    if ([string]::IsNullOrWhiteSpace($raw)) { exit 0 }              # fail open

    $payload = $null
    try { $payload = $raw | ConvertFrom-Json } catch { $payload = $null }
    if ($null -eq $payload) {
        [Console]::Error.Write("$($script:Marker): stdin was not JSON; failing open.")
        exit 0                                                       # fail open
    }

    $toolName = [string](Get-Prop $payload 'tool_name')
    if ($toolName -ne 'AskUserQuestion') { exit 0 }                  # not ours

    if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
        $RepoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
    }
    $ledgerPaths = @(
        (Join-Path $RepoRoot 'docs\RULINGS.md'),
        (Join-Path $RepoRoot 'docs\RULINGS_ARCHIVE.md')
    )

    $toolInput = Get-Prop $payload 'tool_input'
    # @($null).Count is 1 in PowerShell, not 0, so the null test has to come first:
    # without it a tool_input carrying no questions at all reached the blocking path
    # and denied the call on malformed input (caught by check 5d in tests\Hooks.Tests.ps1).
    $questionsRaw = Get-Prop $toolInput 'questions'
    if ($null -eq $questionsRaw) { exit 0 }                          # fail open
    $questions = @($questionsRaw)
    if ($questions.Count -eq 0) { exit 0 }                           # nothing to judge

    $failures = New-Object System.Collections.Generic.List[string]
    for ($i = 0; $i -lt $questions.Count; $i++) {
        $q = $questions[$i]
        if ($null -eq $q) { continue }                               # malformed entry
        $parts = New-Object System.Collections.Generic.List[string]
        foreach ($f in @('question', 'header')) {
            $v = Get-Prop $q $f
            if ($null -ne $v) { $parts.Add([string]$v) }
        }
        foreach ($opt in @(Get-Prop $q 'options')) {
            if ($opt -is [string]) { $parts.Add($opt); continue }
            foreach ($f in @('label', 'description')) {
                $v = Get-Prop $opt $f
                if ($null -ne $v) { $parts.Add([string]$v) }
            }
        }
        $text = ($parts -join "`n")

        $reasons = New-Object System.Collections.Generic.List[string]
        if (Test-QuestionCitation -Text $text -LedgerPaths $ledgerPaths -Reasons $reasons) { continue }

        $label = ConvertTo-NormalizedText ([string](Get-Prop $q 'question'))
        if ([string]::IsNullOrEmpty($label)) { $label = ConvertTo-NormalizedText ([string](Get-Prop $q 'header')) }
        if ($label.Length -gt 70) { $label = $label.Substring(0, 70) + '...' }
        $failures.Add("question " + ($i + 1) + " ('" + $label + "'): " + ($reasons -join '; '))
    }

    if ($failures.Count -gt 0) {
        $msg = "BLOCKED by $($script:Marker). Handoff sec 4 rule 2: " + '"' + $script:RuleQuote + '"' +
        " Every question you put to the owner must show its record state before it presupposes one, because a question that assumes a ruling nobody made is how a paraphrase becomes a rule. Failing: " +
        ($failures -join ' | ') +
        ". Fix: search " + ($ledgerPaths -join ' and ') +
        " first, then either add RULING ON FILE: " + '"' + "<text copied verbatim from the ledger>" + '"' +
        " to the question or to an option description, or add NO RULING FOUND - searched: <the files you actually searched> and say plainly in the question that no ruling exists. Do not paraphrase a ruling into existence."
        Write-HookDeny -Reason $msg
    }

    exit 0
}
catch {
    [Console]::Error.Write("$($script:Marker): internal error, failing open: " + $_.Exception.Message)
    exit 0                                                           # fail open
}
