<#
.SYNOPSIS
  Chạy benchmark GET /api/products, có kiểm tra điều kiện trước khi đo.

.DESCRIPTION
  Kết quả ghi ra perf/out/<Label>__<scenario>__r<N>.json và được đọc lại bởi
  perf/compare.ps1. Đừng đặt tên file bằng tay - quy ước đặt tên chính là thứ
  compare.ps1 dựa vào để nhóm các lần chạy.

.EXAMPLE
  .\perf\run.ps1 -Check              # chỉ kiểm tra môi trường
  .\perf\run.ps1 -Label C            # warm-up + hot + tail, 1 lần mỗi kịch bản
  .\perf\run.ps1 -Label C -Repeat 3  # lặp 3 lần để đo được độ lệch
  .\perf\run.ps1 -Label C -Pages 1   # chỉ kịch bản hot
#>
[CmdletBinding()]
param(
    # Tên mốc: A, B, C... Dùng làm tiền tố tên file kết quả.
    [string]$Label,

    # 0 = chạy cả hai kịch bản (hot=1 trang, tail=50 trang).
    # Truyền số cụ thể = chỉ chạy kịch bản đó.
    [int]$Pages = 0,

    [int]$Vus = 50,

    # Số lần lặp mỗi kịch bản. >1 cho biết sàn nhiễu của phép đo - nếu độ lệch
    # giữa các lần lặp lớn hơn chênh lệch giữa hai mốc thì kết luận vô nghĩa.
    [int]$Repeat = 1,

    [string]$BaseUrl = 'http://localhost:5204',

    [switch]$SkipWarmup,

    # Chỉ kiểm tra điều kiện rồi thoát.
    [switch]$Check
)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
Set-Location $root

# --- tim k6 -----------------------------------------------------------------
$k6 = (Get-Command k6 -ErrorAction SilentlyContinue).Source
if (-not $k6) { $k6 = "$env:LOCALAPPDATA\k6\k6.exe" }
if (-not (Test-Path $k6)) {
    throw "Khong tim thay k6 (da thu PATH va $env:LOCALAPPDATA\k6\k6.exe)"
}

# --- kiem tra API -----------------------------------------------------------
# API chua chay -> 100% request fail -> bang ket qua toan so 0, trong y het mot
# ket qua cuc tot. Chan tu dau con hon phat hien sau khi da chep vao bao cao.
$uri = [Uri]$BaseUrl
$listening = Test-NetConnection -ComputerName $uri.Host -Port $uri.Port `
    -InformationLevel Quiet -WarningAction SilentlyContinue
if (-not $listening) {
    Write-Host ""
    Write-Host "API chua chay tai $BaseUrl" -ForegroundColor Red
    Write-Host "  cd Backend_May_Ecommerce"
    Write-Host "  dotnet run -c Release --project src/Shop.API --launch-profile http"
    Write-Host ""
    exit 1
}

try {
    $probe = (Invoke-WebRequest "$BaseUrl/api/products?page=1&pageSize=20" `
        -UseBasicParsing -TimeoutSec 30).Content | ConvertFrom-Json
} catch {
    Write-Host "API tra loi khi goi /api/products: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Duoi nguong nay thi bug in-memory paging khong lo ra va moc A ~ moc B.
if ($probe.totalCount -lt 10000) {
    Write-Host ""
    Write-Host "Chi co $($probe.totalCount) san pham - qua it de do." -ForegroundColor Yellow
    Write-Host "Seed truoc: xem muc 'Chuan bi' trong perf/README.md" -ForegroundColor Yellow
    Write-Host ""
    exit 1
}

Write-Host "k6       : $k6" -ForegroundColor DarkGray
Write-Host "API      : $BaseUrl (totalCount = $($probe.totalCount))" -ForegroundColor DarkGray

if ($Check) {
    Write-Host "Moi truong san sang." -ForegroundColor Green
    exit 0
}
if (-not $Label) { throw "Thieu -Label. Vi du: .\perf\run.ps1 -Label C" }
if ($Label -match '__') { throw "Label khong duoc chua '__' (day la dau phan cach ten file)." }

$outDir = Join-Path $root 'perf\out'
New-Item -ItemType Directory -Force $outDir | Out-Null

# --- warm-up ----------------------------------------------------------------
# JIT cua .NET va buffer pool cua Postgres deu can thoi gian am len; lan chay
# dau luon xau hon thuc te nen bi bo di.
if (-not $SkipWarmup) {
    Write-Host ""
    Write-Host "=== WARM-UP (ket qua bi bo) ===" -ForegroundColor Cyan
    & $k6 run --quiet --env VUS=10 --env "BASE_URL=$BaseUrl" perf/products-list.js | Out-Null
}

# hot  = moi VU deu goi page 1 -> cache hit ratio ~100%, kich ban lac quan nhat
# tail = VU rai deu 50 trang   -> hit ratio thuc te hon
if ($Pages -gt 0) {
    $scenarios = @(@{ Pages = $Pages; Name = "p$Pages" })
} else {
    $scenarios = @(
        @{ Pages = 1;  Name = 'hot' },
        @{ Pages = 50; Name = 'tail' }
    )
}

foreach ($s in $scenarios) {
    for ($r = 1; $r -le $Repeat; $r++) {
        $tag = "$Label__$($s.Name)__r$r"
        $out = "perf/out/$tag.json"

        Write-Host ""
        Write-Host "=== $tag (PAGES=$($s.Pages), VUS=$Vus) ===" -ForegroundColor Cyan

        & $k6 run `
            --env "LABEL=$tag" `
            --env "PAGES=$($s.Pages)" `
            --env "VUS=$Vus" `
            --env "BASE_URL=$BaseUrl" `
            --summary-export $out `
            perf/products-list.js

        # 99 = threshold bi vuot. O moc A dieu do dung va co y nghia, nen day
        # khong phai loi cua phep do.
        if ($LASTEXITCODE -eq 99) {
            Write-Host "  (threshold bi vuot - ket qua van hop le)" -ForegroundColor Yellow
        } elseif ($LASTEXITCODE -ne 0) {
            Write-Host "  k6 loi, exit code $LASTEXITCODE" -ForegroundColor Red
        }
    }
}

Write-Host ""
Write-Host "Xong. Xem bang tong hop:  .\perf\compare.ps1" -ForegroundColor Green
