# Runs the batch checks (run_checks.py beside this file) on this PC, with no Claude session: the requests go to the
# Claude API with your API key and are paid from the API credit (the Max plan's $100 a month), never from the plan's
# usage limits, so this works when the five-hour limit is used up. README.md beside this file has the details.
#
# First time, once (asks for the key, hidden, keeps it as your Windows user variable RW_BATCH_API_KEY, sends nothing):
#   powershell -ExecutionPolicy Bypass -File "<this folder>\Run-Batch.ps1" -SaveKey
# Then, any time (Haiku 5.5, only what changed since each check's last full run, at most $3):
#   powershell -ExecutionPolicy Bypass -File "<this folder>\Run-Batch.ps1"
# Other forms:
#   ... Run-Batch.ps1 -Plan                     # the cost estimate only; sends nothing, costs nothing
#   ... Run-Batch.ps1 -Full                     # every check whole, not only what changed
#   ... Run-Batch.ps1 -Model sonnet -MaxCost 12 # Sonnet 5.5 (a full run about $5.48, at most $10.52)
#   ... Run-Batch.ps1 -Check scope,design       # some checks only (text, scope, design, docs, art)
#   ... Run-Batch.ps1 -Collect msgbatch_...     # finish a batch still running when the two-hour wait ended
#
# What it does: pulls the newest base into this clone (git pull --ff-only), installs the Python packages it needs the
# first time, runs run_checks.py, waits for the batch (up to two hours; most end within minutes), and moves the
# report from docs\reports to Desktop\RogueWave\batch, where a Claude thread can read it later (that folder is
# approved for the project's threads): say "read the batch report on my Desktop" in the project. Nothing else on the
# PC is touched, the game is never started, and the report is not committed: a thread does that in its own PR. The
# report is moved, not copied, so the clone keeps no untracked report that would stop the next git pull.
param(
    [ValidateSet("haiku", "sonnet")][string]$Model = "haiku",
    [string]$Check = "all",
    [switch]$Full,
    [double]$MaxCost = 3,
    [switch]$Plan,
    [switch]$SaveKey,
    [switch]$NoPull,
    [string]$Collect = "",
    [string]$CopyTo = (Join-Path ([Environment]::GetFolderPath("Desktop")) "RogueWave\batch")
)
# Exit codes of git, Python and pip are checked one by one, so a line on stderr is not treated as a failure.
$ErrorActionPreference = "Continue"
$repo = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..")).Path
$script = Join-Path $repo "TDS_Mod\Tools\batch\run_checks.py"
$reports = Join-Path $repo "docs\reports"
$inv = [Globalization.CultureInfo]::InvariantCulture

# The key: this window, else your saved user variable, else -SaveKey asks for it.
if ($SaveKey) {
    $secure = Read-Host "API key from the Console (hidden; kept as your Windows user variable RW_BATCH_API_KEY)" -AsSecureString
    $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
    try { $plain = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr).Trim() }
    finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr) }
    if (-not $plain) { Write-Host "No key typed; nothing saved."; exit 3 }
    [Environment]::SetEnvironmentVariable("RW_BATCH_API_KEY", $plain, "User")
    Remove-Variable plain
    Write-Host "Saved. Claude Code never reads this name, so no Claude session is offered the key."
    Write-Host "Nothing was sent. Run the same command without -SaveKey to run the checks."
    exit 0
}
if (-not $env:RW_BATCH_API_KEY) {
    $env:RW_BATCH_API_KEY = [Environment]::GetEnvironmentVariable("RW_BATCH_API_KEY", "User")
}
if (-not $env:RW_BATCH_API_KEY -and -not $Plan) {
    Write-Host "No key yet. Run once with -SaveKey (it asks for the key, hidden)."
    exit 3
}

# Python 3.10 or later, through the py launcher when there is one.
$py = @()
if (Get-Command py -ErrorAction SilentlyContinue) { $py = @("py", "-3") }
elseif (Get-Command python -ErrorAction SilentlyContinue) { $py = @("python") }
else { Write-Host "Python is missing: install Python 3.12 (winget install Python.Python.3.12), then run this again."; exit 1 }
function Invoke-Py { param([string[]]$PyArgs) $exe = $py[0]; $rest = @($py | Select-Object -Skip 1) + $PyArgs; & $exe @rest }
Invoke-Py @("-c", "import sys; sys.exit(0 if sys.version_info >= (3, 10) else 1)")
if ($LASTEXITCODE -ne 0) { Write-Host "Python 3.10 or later is needed (winget install Python.Python.3.12)."; exit 1 }
Invoke-Py @("-c", "import importlib.util as u, sys; sys.exit(0 if u.find_spec('anthropic') and u.find_spec('PIL') else 1)")
if ($LASTEXITCODE -ne 0) {
    Write-Host "Installing the Python packages (anthropic, pillow), once..."
    Invoke-Py @("-m", "pip", "install", "--quiet", "--user", "anthropic", "pillow")
    if ($LASTEXITCODE -ne 0) { Write-Host "pip could not install anthropic and pillow."; exit 1 }
}

$git = [bool](Get-Command git -ErrorAction SilentlyContinue)
if (-not $git -and -not $Full -and -not $Collect) {
    Write-Host "Git is missing, so 'only what changed' can't work: running everything (-Full). Install Git for Windows (winget install Git.Git) to fix."
    $Full = $true
}

# Batch reports this script left in docs\reports go to the Desktop folder. With git: every untracked one (also one
# left by a run that was stopped, which would otherwise stop the pull once a thread commits a report of that name).
# Without git: only those written since $After.
function Move-Reports {
    param([datetime]$After)
    if ($git) {
        $names = @(& git -C $repo ls-files --others --exclude-standard -- docs/reports |
            ForEach-Object { Split-Path $_ -Leaf } | Where-Object { $_ -like "BATCH_CHECKS_*.md" })
        $left = @($names | ForEach-Object { Get-Item (Join-Path $reports $_) })
    } else {
        $left = @(Get-ChildItem $reports -Filter "BATCH_CHECKS_*.md" | Where-Object { $_.LastWriteTime -ge $After })
    }
    if (-not $left) { return @() }
    New-Item -ItemType Directory -Force -Path $CopyTo | Out-Null
    foreach ($f in $left) { Move-Item $f.FullName -Destination (Join-Path $CopyTo $f.Name) -Force }
    return @($left | ForEach-Object { Join-Path $CopyTo $_.Name })
}
foreach ($m in (Move-Reports -After ([datetime]::MaxValue))) { Write-Host "Moved an earlier report to: $m" }

# The newest base, so the checks read what the threads built.
if ($git -and -not $NoPull) {
    & git -C $repo pull --ff-only --quiet origin claude/phase-1-6lfnar
    if ($LASTEXITCODE -ne 0) { Write-Host "git pull failed; checking the clone as it is." }
}

if ($Collect) {
    $a = @($script, "collect", $Collect, "--max-wait", "120")
} else {
    $cmd = if ($Plan) { "plan" } else { "submit" }
    $a = @($script, $cmd, "--model", $Model, "--check", $Check)
    if (-not $Full) { $a += @("--since", "last") }
    if (-not $Plan) { $a += @("--max-cost", $MaxCost.ToString($inv), "--max-wait", "120") }
}
$start = Get-Date
Push-Location $repo
try { Invoke-Py $a; $code = $LASTEXITCODE } finally { Pop-Location }

switch ($code) {
    0 { }
    2 { Write-Host "Bad arguments (exit 2): see the line above." }
    3 { Write-Host "No key (exit 3): run once with -SaveKey." }
    4 { Write-Host "Over -MaxCost (exit 4): nothing was sent. Raise -MaxCost, or narrow it with -Check or -Model haiku." }
    default { Write-Host "run_checks.py stopped with exit $code (see above)." }
}
if ($Plan -or $code -ne 0) { exit $code }

$moved = @(Move-Reports -After $start)
if (-not $moved) {
    Write-Host "No new report: nothing changed since the last run, or the batch is still running. To finish it, run"
    Write-Host "this script again with -Collect and the batch id printed above (msgbatch_...)."
    exit 0
}
foreach ($m in $moved) {
    Write-Host ""
    Write-Host "Report: $m"
    Write-Host "To hand it back, tell the project: read the batch report on my Desktop ($(Split-Path $m -Leaf))."
}
