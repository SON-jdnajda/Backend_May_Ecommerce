<#
.SYNOPSIS
  Đọc mọi kết quả trong perf/out và in bảng so sánh giữa các mốc.

.DESCRIPTION
  Thay cho việc chép số bằng tay vào README - chép tay là cách nhanh nhất để
  gán nhầm số của mốc này sang mốc khác.

  Khi một mốc có nhiều lần lặp (-Repeat), bảng lấy MEDIAN và in kèm độ lệch
  giữa lần cao nhất và thấp nhất. Độ lệch đó là SÀN NHIỄU: chênh lệch giữa hai
  mốc mà nhỏ hơn sàn nhiễu thì không kết luận được gì.

.EXAMPLE
  .\perf\compare.ps1
  .\perf\compare.ps1 -Baseline A        # thêm cột so với mốc A
  .\perf\compare.ps1 -Scenario hot
#>
[CmdletBinding()]
param(
    # Mốc dùng làm gốc để tính phần trăm thay đổi.
    [string]$Baseline,

    # Lọc theo kịch bản: hot, tail... Bỏ trống = tất cả.
    [string]$Scenario
)

$ErrorActionPreference = 'Stop'
$outDir = Join-Path $PSScriptRoot 'out'

if (-not (Test-Path $outDir)) { throw "Chua co thu muc $outDir" }

$files = Get-ChildItem $outDir -Filter '*.json' -ErrorAction SilentlyContinue
if (-not $files) {
    Write-Host "Chua co ket qua nao trong perf/out. Chay: .\perf\run.ps1 -Label A" -ForegroundColor Yellow
    exit 0
}

# --- doc tung file ----------------------------------------------------------
$runs = foreach ($f in $files) {
    # Quy uoc ten: <Label>__<scenario>__r<N>.json
    $parts = $f.BaseName -split '__'
    if ($parts.Count -ne 3) {
        Write-Host "Bo qua '$($f.Name)' - khong dung quy uoc <Label>__<scenario>__r<N>.json" -ForegroundColor DarkYellow
        continue
    }

    try { $j = Get-Content $f.FullName -Raw | ConvertFrom-Json } catch {
        Write-Host "Bo qua '$($f.Name)' - JSON hong" -ForegroundColor DarkYellow
        continue
    }

    $d = $j.metrics.http_req_duration
    $rq = $j.metrics.http_reqs
    $fl = $j.metrics.http_req_failed
    if (-not $d -or -not $rq) {
        Write-Host "Bo qua '$($f.Name)' - khong co metric (lan chay do 100% fail?)" -ForegroundColor DarkYellow
        continue
    }

    [pscustomobject]@{
        Label    = $parts[0]
        Scenario = $parts[1]
        Run      = $parts[2]
        RPS      = [double]$rq.rate
        P95      = [double]$d.'p(95)'
        P99      = [double]$d.'p(99)'
        Median   = [double]$d.med
        ErrRate  = [double]$fl.rate
        When     = $f.LastWriteTime
    }
}

if (-not $runs) { Write-Host "Khong doc duoc ket qua nao." -ForegroundColor Yellow; exit 0 }

function Get-Median([double[]]$values) {
    $s = $values | Sort-Object
    $n = $s.Count
    if ($n -eq 0) { return 0 }
    if ($n % 2 -eq 1) { return $s[[int](($n - 1) / 2)] }
    return ($s[$n / 2 - 1] + $s[$n / 2]) / 2
}

$scenarios = $runs | Select-Object -ExpandProperty Scenario -Unique | Sort-Object
if ($Scenario) { $scenarios = $scenarios | Where-Object { $_ -eq $Scenario } }

foreach ($sc in $scenarios) {
    $inScenario = $runs | Where-Object { $_.Scenario -eq $sc }

    # Sap xep theo lan chay dau tien -> thu tu do, khong phai thu tu alphabet.
    $labels = $inScenario | Group-Object Label | Sort-Object { ($_.Group | Sort-Object When | Select-Object -First 1).When }

    Write-Host ""
    Write-Host "=== kich ban: $sc ===" -ForegroundColor Cyan

    $rows = foreach ($g in $labels) {
        $rpsAll = @($g.Group.RPS)
        $spread = 0.0
        if ($rpsAll.Count -gt 1 -and (Get-Median $rpsAll) -gt 0) {
            $spread = (($rpsAll | Measure-Object -Maximum).Maximum - ($rpsAll | Measure-Object -Minimum).Minimum) `
                      / (Get-Median $rpsAll) * 100
        }

        [pscustomobject]@{
            'Moc'       = $g.Name
            'Lan'       = $g.Count
            'RPS'       = [math]::Round((Get-Median $rpsAll), 1)
            'p95 (ms)'  = [math]::Round((Get-Median @($g.Group.P95)), 1)
            'p99 (ms)'  = [math]::Round((Get-Median @($g.Group.P99)), 1)
            'med (ms)'  = [math]::Round((Get-Median @($g.Group.Median)), 1)
            'loi %'     = [math]::Round((Get-Median @($g.Group.ErrRate)) * 100, 2)
            'lech RPS %'= if ($rpsAll.Count -gt 1) { [math]::Round($spread, 1) } else { $null }
        }
    }

    if ($Baseline) {
        $base = $rows | Where-Object { $_.Moc -eq $Baseline } | Select-Object -First 1
        if ($base) {
            $rows = foreach ($r in $rows) {
                $dRps = $null; $dP95 = $null
                if ($base.RPS -gt 0)      { $dRps = [math]::Round(($r.RPS / $base.RPS), 2) }
                if ($base.'p95 (ms)' -gt 0) { $dP95 = [math]::Round((($r.'p95 (ms)' - $base.'p95 (ms)') / $base.'p95 (ms)') * 100, 1) }
                $r | Add-Member -NotePropertyName "RPS vs $Baseline" -NotePropertyValue "x$dRps" -PassThru |
                     Add-Member -NotePropertyName "p95 vs $Baseline" -NotePropertyValue "$dP95%" -PassThru
            }
        } else {
            Write-Host "Khong co moc '$Baseline' trong kich ban nay." -ForegroundColor DarkYellow
        }
    }

    $rows | Format-Table -AutoSize
}

# --- canh bao san nhieu -----------------------------------------------------
$noisy = $runs | Group-Object Label, Scenario | Where-Object { $_.Count -gt 1 } | ForEach-Object {
    $r = @($_.Group.RPS)
    $m = Get-Median $r
    if ($m -gt 0) {
        $sp = (($r | Measure-Object -Maximum).Maximum - ($r | Measure-Object -Minimum).Minimum) / $m * 100
        if ($sp -gt 10) { "$($_.Name): lech $([math]::Round($sp,1))% giua cac lan lap" }
    }
}

Write-Host ""
if ($noisy) {
    Write-Host "CANH BAO - san nhieu cao:" -ForegroundColor Yellow
    $noisy | ForEach-Object { Write-Host "  $_" -ForegroundColor Yellow }
    Write-Host "  Chenh lech giua hai moc phai LON HON con so nay moi ket luan duoc." -ForegroundColor Yellow
} else {
    $single = $runs | Group-Object Label, Scenario | Where-Object { $_.Count -eq 1 }
    if ($single) {
        Write-Host "Luu y: co moc chi chay 1 lan nen chua biet san nhieu." -ForegroundColor DarkYellow
        Write-Host "  Chay lai voi -Repeat 3 de do do lech." -ForegroundColor DarkYellow
    }
}
Write-Host ""
