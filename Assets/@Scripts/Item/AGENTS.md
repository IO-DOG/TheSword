<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-08-20 | Updated: 2026-08-20 -->

# Item

## Purpose
맵 타일 위에 놓이는 **상호작용 오브젝트**. 플레이어가 밀거나 닿으면 반응하고, 결과를
활성화 딕셔너리(세이브)에 기록한다. 콜라이더가 루트가 아니라 자식에 붙어 있는 경우가 많다.

## Key Files
| File | Description |
|------|-------------|
| `Door.cs` | 색 문(초록/노랑/빨강). 대응 열쇠를 쓰면 열린다. 어떤 콜라이더에서든 그 문의 `Door`를 찾아낸다. 문짝 위에 색 표지(`Utils/KeyMark` ●▲◆)를 단다 |
| `BossDoor.cs` | 보스방 입구. `PortalController` 상속 — 문이 아니라 포탈 계열이다 |
| `ConsumableItem.cs` | 소비 아이템. 회복 물약과 **룬**(공격/방어/체력 영구 증가, `ConsumableItemData` 9·10·11) |
| `Equip.cs` | 떨군 장비. `EquipItem_{id}` 애니메이션 상태를 재생한다 |
| `Lever.cs` | 레버. 당기면 기둥(`Pillar`)이 내려가 길이 열린다 |
| `Pillar.cs` | 오르내리는 기둥. 보스방 통로 차단에 쓰인다 |
| `PostPointLight.cs` | 기둥/장식용 포인트 라이트 |

## For AI Agents

### Working In This Directory
- **이펙트가 없어도 로직은 끝까지 굴러가야 한다.** `Managers.Resource.Instantiate`는
  어드레서블에 없는 키나 데이터의 `-`에 대해 null을 준다. 그대로 `transform`을 만지면
  `PickUp`이 중간에 끊겨 **아이템이 안 꺼지고, 줍지도 못하는데 길만 막는다**.
  파티클/이펙트는 반드시 null을 확인하고 없으면 조용히 건너뛴다 (`ConsumableItem.PlayParticle` 참고).
  문 자물쇠(`Door.SpawnLock`)도 그렇다 — 거기서 죽으면 `OnInteract` 가 영영 남아 플레이어가 멈춘다.
  `Tools/rules_check.py` 는 글자 그대로 쓴 키("DoorLock")가 PreLoad 에 있는지, 데이터에서 온 키로 만든 것은 null 을 보는지를 잰다
- 색 표지(`KeyMark`)는 문짝(`Door` 가 붙은 오브젝트)·열쇠 아이템 **아래에** 단다 — 문이 열리거나 열쇠를 주워 그 오브젝트가
  꺼지면 같이 사라진다. 문짝 부모(`Tilemap_N`)의 자식 순서는 건드리지 않는다(`Door.Start` 가 `GetChild(1)` 을 자물쇠 자리로 쓴다)
- 조회는 `Util.FindInTile<T>` — 문·레버·포탈은 **타일에 하나뿐인 것**이다.
  `Util.Find<T>`(좁은 탐색)는 몬스터·아이템처럼 여럿인 것에 쓴다
- 획득/개방 결과는 반드시 활성화 딕셔너리(`_itemIndex_forActive` 등)에 반영해야 세이브에 남는다.
  **인덱스가 다른 오브젝트와 겹치면 하나를 먹을 때 다른 하나가 사라진다** (3층 열쇠 ↔ 2층 포션 사고)
- 룬은 층마다 하나, **계단 앞 구역**에 놓인다. F층의 룬 효과는 F+1층부터다(`rune_bonus`) —
  배치를 옮기면 `Tools/generate_content.py`의 완주 계산이 어긋난다
- 지나갈 수 없는 포탈은 막아도 바닥이 갈리지 않는 칸에, 통로 입구 둘레를 비우고 놓는다(`layout_gen._portal_cells`).
  문은 통로가 방에 닿는 문턱에만 선다(`check_structure`)

## Dependencies

### Internal
- `Managers.Game`(플레이어 스탯·현재 아이템), `Managers.Data`(테이블·세이브),
  `Managers.Resource`(이펙트), `Utils/Util.FindInTile`, `Controllers/PortalController`

### External
- DOTween

<!-- MANUAL: Any manually added notes below this line are preserved on regeneration -->
