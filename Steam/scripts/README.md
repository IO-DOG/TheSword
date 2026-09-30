# SteamPipe 스크립트

`upload.ps1` 이 이 폴더의 VDF 로 `Build\Windows` 를 올린다. 처음 한 번 꺾쇠 자리표시를 숫자로 바꾼다.
자리표시가 하나라도 남아 있으면 `upload.ps1` 은 올리지 않고 멈춘다.

| 파일 | 바꿀 것 | 무엇 |
|---|---|---|
| `app_build_APPID.vdf` | `<APPID>`, `<DEPOTID>` | 본편 AppID, 본편 depot ID |
| `depot_build_DEPOTID.vdf` | `<DEPOTID>` | 위와 같은 depot ID |
| `app_build_DEMO_APPID.vdf` | `<DEMO_APPID>`, `<DEMO_DEPOTID>` | 데모 AppID, 데모 depot ID |
| `depot_build_DEMO_DEPOTID.vdf` | `<DEMO_DEPOTID>` | 위와 같은 데모 depot ID |

## 숫자는 어디서

- **AppID**: Steamworks 에서 앱을 고르면 주소가 `partner.steamgames.com/apps/landing/<AppID>` 다. 데모는 본편 랜딩 페이지 → *All associated packages, DLC, demos and tools* → *Add Demo* 로 만든 앱의 번호.
- **Depot ID**: App Admin → **SteamPipe → Depots**. 새 앱에는 depot 이 하나 만들어져 있다(보통 AppID+1). 없으면 *Add New Depot*.
  depot 은 OS **Windows**, 언어는 기본값(모든 언어)으로 둔다. 바꿨으면 App Admin 에서 *Publish* 해야 반영된다.

## 채우는 법

꺾쇠까지 지우고 숫자만 남긴다.

```
"AppID"	"1234560"
...
"Depots"
{
	"1234561"	"depot_build_DEPOTID.vdf"
}
```

- depot ID 는 두 곳(app_build 의 `Depots` 안 키, depot_build 의 `DepotID`)이 같아야 한다.
- **파일 이름은 그대로 둔다.** `upload.ps1` 이 이 이름으로 찾는다. 이름을 바꾸면 app_build 의 `Depots` 값도 바꾸고 `-Script <파일>` 로 넘긴다.
- AppID·depot ID 는 비밀이 아니다. 채운 채로 커밋해도 된다. **계정 이름·비밀번호는 어디에도 적지 않는다** — `upload.ps1` 은 `STEAM_USER` 환경 변수나 입력으로 받고, 로그인 토큰은 `Steam\steamcmd\` 안에만 남는다(.gitignore).

## 스크립트가 하는 일

| 키 | 값 | 까닭 |
|---|---|---|
| `ContentRoot` | `..\..\Build\Windows\` | 경로는 **이 VDF 파일 기준**이다. 저장소 루트의 `Build\Windows` |
| `BuildOutput` | `..\output\` | steamcmd 로그·캐시 → `Steam\output` (.gitignore). 지워도 되지만 다음 업로드가 느려진다 |
| `SetLive` | `beta` | **default 브랜치는 스크립트로 못 켠다** — App Admin 에서 올린다. `beta` 브랜치를 먼저 만들어 둔다 |
| `FileExclusion` | `*.pdb`, `steam_appid.txt`, `*_DoNotShip*`, `*_ButDontShipItWithYourGame*` | 디버그 기호와 개발용 파일은 내보내지 않는다. 기호 폴더는 `GameBuild` 가 이미 `Build\Symbols` 로 옮기지만 한 겹 더 막는다 |

depot 스크립트에는 `ContentRoot` 가 없다 — app_build 의 것을 그대로 쓴다(depot 쪽 `ContentRoot` 는 덮어쓰기용이다).

## 확인

```powershell
powershell -ExecutionPolicy Bypass -File Steam\scripts\upload.ps1 -Preview        # 올리지 않는다
```

`-Preview` 는 steamcmd `run_app_build -preview` 다. 파일 목록과 로그만 `Steam\output` 에 쓴다.
그 목록에 `.pdb`·`steam_appid.txt`·`*_DoNotShip` 폴더가 없는지 본다. 제외 규칙이 폴더에 안 먹으면 여기서 드러난다.

데모는 `-Demo` 를 붙인다. **데모 앱에는 DEMO 정의로 구운 빌드만** 올린다(MASTER_PLAN D1). 본편 빌드를 데모 앱에 올리면
본편 전체가 무료로 풀린다 — 파일로는 가릴 수 없어서 `upload.ps1 -Demo` 는 올리기 전에 `demo` 를 쳐서 확인하게 한다.
