<#
  TheSword 빌드(Build\Windows, 데모는 Build\WindowsDemo)를 SteamPipe 로 올린다. 처음이면 Steam\scripts\README.md 대로 AppID·DepotID 부터 채운다.

    powershell -ExecutionPolicy Bypass -File Steam\scripts\upload.ps1            # 본편 -> beta 브랜치
    powershell -ExecutionPolicy Bypass -File Steam\scripts\upload.ps1 -Preview   # 올리지 않고 파일 목록만 (Steam\output)
    powershell -ExecutionPolicy Bypass -File Steam\scripts\upload.ps1 -Demo      # 데모 앱 <- Build\WindowsDemo (GameBuild.WindowsDemo)

  steamcmd 는 $env:STEAMCMD -> Steam\steamcmd\steamcmd.exe -> PATH 순서로 찾는다.
  계정은 $env:STEAM_USER, 없으면 묻는다. 처음 한 번은 steamcmd 가 비밀번호와 Steam Guard 코드를 묻고 그 뒤로는 기억한다.
  Windows PowerShell 5.1 용. 한글 때문에 이 파일은 BOM 있는 UTF-8 로 둔다.
#>
param(
    [switch]$Demo,
    [switch]$Preview,
    [string]$Script     # 다른 app_build 파일을 쓸 때 (기본: app_build_APPID.vdf / -Demo 면 app_build_DEMO_APPID.vdf)
)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path

function Stop-Upload([string]$Message) {
    Write-Host "그만둔다: $Message" -ForegroundColor Red
    exit 1
}

# 1. 빌드 — aa 가 비면 실행 파일은 만들어져도 타이틀에서 한 발짝도 못 나간다 (CLAUDE.md "사람이 해 볼 빌드 만들기")
#    데모는 따로 구운 폴더다. 본편을 데모 앱에 올리면 본편 전체가 무료로 풀리는데, Build\WindowsDemo 는 DEMO 정의로 굽는
#    GameBuild.WindowsDemo 만 쓴다 (app_build_DEMO_APPID.vdf 의 ContentRoot 와 같다 — check_steam.py 가 본다).
$folder = if ($Demo) { 'Build\WindowsDemo' } else { 'Build\Windows' }
$step = if ($Demo) { 'GameBuild.WindowsDemo' } else { 'GameBuild.Windows' }
$build = Join-Path $root $folder
if (-not (Test-Path (Join-Path $build 'TheSword.exe'))) {
    Stop-Upload "$folder\TheSword.exe 가 없다. 에디터를 닫고 GameBuild.Prepare -> $step 을 먼저 돌린다."
}
$aa = Join-Path $build 'TheSword_Data\StreamingAssets\aa'
if (-not (Test-Path $aa) -or -not (Get-ChildItem $aa -Recurse -File | Select-Object -First 1)) {
    Stop-Upload "$folder\TheSword_Data\StreamingAssets\aa 가 없거나 비었다 - 어드레서블 콘텐츠 없이 구운 빌드다."
}

# 2. 스크립트 — 자리표시(<APPID> 따위)가 하나라도 남으면 올리지 않는다
if (-not $Script) {
    $Script = Join-Path $PSScriptRoot $(if ($Demo) { 'app_build_DEMO_APPID.vdf' } else { 'app_build_APPID.vdf' })
}
if (-not (Test-Path $Script)) { Stop-Upload "$Script 가 없다." }
$Script = (Resolve-Path $Script).Path
$text = Get-Content -Raw -Encoding UTF8 $Script
$depots = @([regex]::Matches($text, '"(depot_build_[^"]+\.vdf)"') | ForEach-Object { Join-Path (Split-Path $Script) $_.Groups[1].Value })
if ($depots.Count -eq 0) { Stop-Upload "$(Split-Path -Leaf $Script) 가 depot 스크립트를 가리키지 않는다." }
foreach ($file in @($Script) + $depots) {
    if (-not (Test-Path $file)) { Stop-Upload "$file 가 없다." }
    if ((Get-Content -Raw -Encoding UTF8 $file) -cmatch '<[A-Z_]+>') {
        Stop-Upload "$(Split-Path -Leaf $file) 에 자리표시 $($Matches[0]) 가 남았다. Steam\scripts\README.md 대로 채운다."
    }
}
if ($text -notmatch '"AppID"\s+"(\d+)"') { Stop-Upload 'AppID 를 숫자로 읽지 못했다.' }
$appId = $Matches[1]
$outDir = if ($text -match '"BuildOutput"\s+"([^"]+)"') { Join-Path (Split-Path $Script) $Matches[1] } else { Join-Path $root 'Steam\output' }

# 3. steamcmd 와 계정
$steamcmd = $env:STEAMCMD
if (-not $steamcmd) {
    $local = Join-Path $root 'Steam\steamcmd\steamcmd.exe'
    if (Test-Path $local) { $steamcmd = $local }
}
if (-not $steamcmd) {
    $found = Get-Command steamcmd -ErrorAction SilentlyContinue
    if ($found) { $steamcmd = $found.Source }
}
if (-not $steamcmd -or -not (Test-Path $steamcmd)) {
    Stop-Upload 'steamcmd 를 못 찾았다. Steam\steamcmd\steamcmd.exe 에 두거나 $env:STEAMCMD 에 경로를 넣는다 (Steam\README.md 2절).'
}
$user = $env:STEAM_USER
if (-not $user) { $user = Read-Host 'Steam 빌드 계정 이름' }
if (-not $user) { Stop-Upload '계정 이름이 없다.' }

# 4. 빌드 설명 = 버전 + 올리는 순간의 커밋 (App Admin 의 Builds 목록에만 보인다)
$ver = '?'
$line = Select-String -Path (Join-Path $root 'ProjectSettings\ProjectSettings.asset') -Pattern '^\s*bundleVersion:\s*(\S+)' | Select-Object -First 1
if ($line) { $ver = $line.Matches[0].Groups[1].Value }
$hash = ''
try { $hash = (& git -C $root rev-parse --short HEAD 2>$null) } catch { $hash = '' }
$desc = "TheSword-$ver" + $(if ($Demo) { '-demo' } else { '' }) + $(if ($hash) { "-$hash" } else { '' })

# 5. 올린다
$steamArgs = @('+login', $user, '+run_app_build')
if ($Preview) { $steamArgs += '-preview' }
$steamArgs += @('-desc', $desc, $Script, '+quit')
Write-Host "AppID $appId  $desc  $(if ($Preview) { '(미리보기: 올리지 않는다)' } else { '-> beta' })"
$started = Get-Date
& $steamcmd @steamArgs
$code = $LASTEXITCODE
if ($code -ne 0) {
    Write-Host "steamcmd 가 종료 코드 $code 로 끝났다. 위 출력과 $outDir 의 *.log 를 본다." -ForegroundColor Red
    exit $code
}
if ($Preview) {
    Write-Host "미리보기 끝 - 올라간 것은 없다. $outDir 의 파일 목록(manifest)에 .pdb 와 steam_appid.txt 가 없는지 본다."
    exit 0
}

# 6. BuildID — 로그에서 찾고, 못 찾으면 어디서 보는지 알려 준다
$id = $null
$logs = Get-ChildItem $outDir -Filter *.log -File -ErrorAction SilentlyContinue |
    Where-Object { $_.LastWriteTime -ge $started } | Sort-Object LastWriteTime -Descending
foreach ($log in $logs) {
    $hit = Select-String -Path $log.FullName -Pattern 'BuildID\D{0,3}(\d+)' | Select-Object -Last 1
    if ($hit) { $id = $hit.Matches[0].Groups[1].Value; break }
}
if ($id) { Write-Host "BuildID $id" -ForegroundColor Green }
else { Write-Host '로그에서 BuildID 를 못 찾았다 - 위 출력의 "BuildID" 줄이나 아래 페이지 맨 위 빌드를 본다.' }
Write-Host "https://partner.steamgames.com/apps/builds/$appId  - beta 에 올라갔는지 보고, 스모크 테스트 뒤 default 로 올린다 (Steam\README.md 2절)."
