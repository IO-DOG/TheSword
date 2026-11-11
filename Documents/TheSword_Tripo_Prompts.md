# TheSword 챕터별 Tripo 3D 프롬프트

`TheSword_Meshy_Prompts.md` 의 Tripo 판이다. **프롬프트 문장과 이름표는 Meshy 판과 똑같다** — 두 생성기에서 뽑은 그림이 한 게임처럼 보이게. 다른 것은 Tripo 설정, 넣는 폴더(`Assets/@Resources/Art/Tripo/`), 크기 규칙(벽은 옛 벽과 같은 크기 — 아래)이다.
챕터마다 벽 4종, 빛 소품 1종, 벽 소품 1종, 바닥 대표 소품 2종이다. [선택]은 여유가 있을 때만 만든다. 회색 상자 안의 문장을 통째로 Tripo 프롬프트 칸에 붙인다. 프롬프트 위의 굵은 이름이 **내보낸 파일의 이름**이다 — 이름이 곧 놓일 자리다.

## 크레딧 — 웹과 CLI 는 지갑이 다르다

- **웹 앱(tripo3d.ai)의 멤버십 크레딧은 API 크레딧이 아니다**(Tripo 도움말). `tripo` CLI 는 API 를 부르므로 웹에서 산 크레딧을 못 쓴다.
- 웹 크레딧으로 하면 웹에서 한 장씩 뽑아 GLB 로 내려받는다(아래 "웹에서").
- API 크레딧이 있으면 CLI 로 이름까지 붙여 한 번에 받을 수 있다(아래 "CLI 로").

## Tripo 설정

| 항목 | 값 | 까닭 |
|---|---|---|
| 모델 | **P 시리즈**(P2, 없으면 P1) — 저폴리 전용. 또는 v3.x + **Smart Low Poly** | 복셀 화풍에 맞는 굵은 면. 면 수를 정할 수 있다 |
| 면 수(face limit) | 벽 **2,800**, 소품 **1,800** | 들일 때 기준이 벽 3,000·소품 2,000 삼각형이다. 넘으면 콘솔이 알린다 |
| Quad | **끈다** | 켜면 FBX 로만 나온다(사각형은 GLB 에 못 담는다) |
| 텍스처 | 표준(standard). 높은 등급은 크레딧만 쓴다 | 도구가 512 이하로 다시 굽고 Point 필터를 건다 |
| PBR | 끈다 | 바탕색(base color)만 쓴다 — 노멀·거칠기는 버려진다 |
| 내보내기 | **GLB** | 앞면 방향이 정확하다. FBX·OBJ 는 앞이 돌아갈 수 있다 |
| 네거티브 프롬프트 | 아래 한 줄 (255자 한도) | 사실풍·글자·바닥판·그림자가 끼는 것을 막는다 |

네거티브 프롬프트(칸이 있으면 모든 그림에 같게):
```
realistic, photorealistic, high detail, smooth organic, text, letters, logo, ground plane, base platform, pedestal, baked shadows, multiple objects, grass block, grass, wooden planks, dirt, hollow, open top, container
```

## 먼저 읽을 것

- 모든 프롬프트 끝에 같은 스타일 꼬리말이 붙어 있다. 지우지 않아야 다섯 챕터가 한 게임처럼 보인다.
- 명암은 Unity의 챕터 해가 넣는다. 텍스처에 그림자가 구워져 나오면 다시 뽑는다.
- **색은 그림 그대로 나온다.** 지금 벽은 챕터 00 그림 한 벌에 챕터 색을 곱해 쓰지만, 새 그림에는 곱하지 않는다.
- **벽은 옛 벽과 같은 크기로 들어간다.** 폭·깊이는 나무 벽(`Tilemap_C00_W01`)처럼 한 칸 0.32 를 꽉 채우고, 몸통 윗면은 돌 벽(`W02·W03`) 높이 0.30 에 맞는다. 굴뚝·가시·떠오른 조각처럼 위로 솟은 것은 그 위로 남는다(전체가 0.30 의 1.5배까지 — 넘으면 몸통째 낮아지고 콘솔이 알린다). 축마다 따로 맞추므로 **정육면체에 가까운 그림일수록 덜 일그러진다** — 프롬프트의 "about as tall as it is wide" 를 지키는 그림을 고른다. 30% 넘게 늘거나 줄면 콘솔이 알린다.
- 몸통 윗면은 도구가 정점으로 찾는다(단면이 전체 폭·깊이의 80% 이상인 가장 높은 곳). 가시가 윗면 네 귀퉁이에 퍼져 솟으면 가시 끝을 윗면으로 잡을 수 있다 — 콘솔의 "몸통 … 전체 높이 …" 줄로 확인한다.
- 소품은 예전처럼 고르게 키우거나 줄이기만 한다(찌그러뜨리지 않는다).
- **Tripo 미리보기의 정면이 게임에서 카메라 쪽을 본다**(GLB 기준). 카메라는 남쪽에서 내려다봐서 벽은 윗면과 앞면만 보인다. 벽감·배수구·창살·화살 구멍·흘러내린 슬래그는 정면에 와야 보인다. 벽에 거는 것은 등이 평평하고 정면이 앞을 보게 뽑는다.
- 글자, 비문, 낙서는 넣지 않는다. 게임의 네 언어(한·영·일·중)로 옮겨지지 않는다.
- 문·바닥 시트·공기 알갱이는 대상이 아니다(문은 다섯 챕터가 같이 쓰고, 나머지는 2D).
- Tripo 로 만든 에셋을 실제로 쓰면 Steam 의 AI 콘텐츠 공개에 Tripo 를 적는다 — `Steam/store/*.md` 의 Meshy 한 줄 자리(`Steam/README.md` AI 공개 항목). 상업 이용 조건은 쓰는 요금제(체험 포함)의 약관을 확인한다.

## 이름표

벽 — `Wall_C{챕터}_{변형}`

| 챕터 | Wall_C?_Base | Wall_C?_A | Wall_C?_B | Wall_C?_C |
|---|---|---|---|---|
| 0 이끼 낀 지하 묘소 | 기본형 | 금 간 벽 | 벽감 벽 | 무너진 벽 |
| 1 무너진 수로 | 기본형 | 배수구 벽 | 창살 벽 | 무너진 벽 |
| 2 잿빛 용광로 | 기본형 | 리벳 벽 | 굴뚝 벽 | 무너진 벽 |
| 3 얼어붙은 심층 | 기본형 | 고드름 벽 | 화살 구멍 벽 | 무너진 벽 |
| 4 왕좌의 균열 | 기본형 | 빛 균열 벽 | 가시 벽 | 부서진 벽 |

소품 — `Prop_C{챕터}_{자리}`

| 챕터 | Prop_C?_Light | Prop_C?_Wall | Prop_C?_Floor | Prop_C?_Big | [선택] |
|---|---|---|---|---|---|
| 0 | 벽 촛대 (벽면) | 이끼 낀 사슬 | 칼이 꽂힌 무덤 표지 | 닫힌 석관 | |
| 1 | 푸른 벽등 (벽면) | 밧줄과 두레박 | 녹슨 바닥 창살 | 멈춘 물레바퀴 조각 | |
| 2 | 화로 (바닥) | 도르래 사슬과 갈고리 | 슬래그 더미 | 가죽 풀무 | `Prop_C2_Extra` 휜 칼 모양 빈 거푸집 |
| 3 | 차가운 푸른 벽등 (벽면) | 얼어붙은 창 걸이 | 얼음에 갇힌 걸상 | 갑옷 걸이 | `Prop_C3_Light2` 마지막 불씨 화로 |
| 4 | 보랏빛 수정 무리 (바닥) | 위로 뜬 쇠사슬 | 떠 있는 돌 조각 | 부서진 계단 조각 | |

자리마다 하는 일 (크기는 지금 그림을 재서 맞춘다 — 소품은 고르게 키우거나 줄이기만 하고 찌그러뜨리지 않는다. 벽만은 옛 벽과 같은 크기가 되게 축마다 맞춘다)

| 자리 | 어디에 | 크기 | 층마다 |
|---|---|---|---|
| Wall_C?_Base·A·B·C | 벽 칸 전부 | 옛 벽과 같은 크기 — 폭·깊이는 나무 벽처럼 한 칸(0.32)을 꽉, 몸통 윗면은 돌 벽 높이(0.30). 굴뚝·가시·떠오른 조각은 그 위로(전체가 0.30의 1.5배까지). 지금 벽 바닥면(y 0.2)에 앉는다 | 기본형 약 80%, 나머지 셋 약 7%씩. 같은 변형은 붙어 서지 않는다 |
| Light (챕터 0·1·3) | 카메라 쪽 벽면 | 지금 횃불(불꽃 포함) 가로·세로 안 | 5·4·3개, 점광원 하나씩 |
| Light (챕터 2·4) | 벽에 붙은 빈 바닥(걷는 칸) | 지금 화로 발자국(0.19) 안, 높이 0.08(발목) 안 | 5·4개, 점광원 하나씩 |
| Wall | 카메라 쪽 벽면 | 지금 사슬의 가장 긴 변(0.2) 안 | 4~8개 |
| Floor · Extra | 벽에 붙은 빈 바닥(걷는 칸, 몬스터·아이템·문·계단 칸은 뺀다) | 화로 발자국 안, 높이 0.08(발목) 안 | 약 8개 · 약 3개 |
| Big | 방 안쪽 벽 한 칸 대신(그 칸은 여전히 막혀 있다) | 벽 한 칸 | 2개 |
| Light2 | 벽에 붙은 빈 바닥(걷는 칸) | 화로 발자국 안, 높이 0.08 안 | 1개, 따뜻한 점광원 |

- 그림이 없는 자리는 지금 모습 그대로다. 벽 변형이 빠지면 그 챕터의 기본형이, 기본형도 없으면 지금 벽이 선다.
- 소품에는 콜라이더가 없고, 새 벽은 지금 벽의 콜라이더를 그대로 갖는다 — 길·충돌·전투 예측이 한 칸도 달라지지 않는다.

## 넣는 법

### 웹에서 (멤버십 크레딧)

1. **Text to 3D** 에 프롬프트를 붙이고, 위 "Tripo 설정" 대로 모델·면 수·네거티브를 맞춘다. 화면에 없는 설정은 건너뛴다.
2. 마음에 드는 결과를 **GLB** 로 내려받는다.
3. **파일 이름을 이름표대로** 바꿔 `Assets/@Resources/Art/Tripo/` 에 넣는다. 예: `Wall_C1_Base.glb`. 대소문자는 도구가 맞춘다. Tripo 가 붙인 긴 이름은 그대로 두면 건너뛴다.

### CLI 로 (API 크레딧)

`tripo login --region ov` 로 로그인해 두고, 이름마다 한 번씩(프로젝트 루트에서):

```bash
tripo make "<프롬프트>" --model tripo-p2 -p face_limit=2800 \
  -p negative_prompt="realistic, photorealistic, high detail, smooth organic, text, letters, logo, ground plane, base platform, pedestal, baked shadows, multiple objects, grass block, grass, wooden planks, dirt" \
  --name Wall_C0_Base -o tripo-out/Wall_C0_Base --json --yes
```

받은 폴더의 `.glb` 를 `Assets/@Resources/Art/Tripo/Wall_C0_Base.glb` 로 옮긴다(소품은 `face_limit=1800`). 뽑기 전에 `tripo balance` 로 API 잔액을 본다 — 웹 크레딧은 여기서 0 으로 보인다. `tripo-out/` 은 커밋하지 않는다.

### 그다음 (둘 다)

1. Unity 메뉴 **TheSword → Import Meshy Art**(이름은 Meshy 지만 `Art/Tripo/` 도 읽는다). 콘솔의 `[MeshyArt]` 줄에 파일마다 맞춘 크기와 삼각형 수가 찍힌다.
2. **소품(Prop_)을 처음 넣었으면** 메뉴 **TheSword → Build Map Decorations**(층 장식 96개를 다시 굽는다). 벽만 넣었으면 할 일이 없다.
3. 플레이해서 그 챕터 층을 본다(아래 "넣은 뒤 볼 것").

같은 이름이 여러 곳에 있으면 **Tripo > Meshy > 복셀 키트(`Art/ChapterKit/`)** 순으로 이긴다 — 하나씩 넣고 다시 Import 하면 그 자리만 바뀐다. 콘솔이 "… 대신 … 를 쓴다" 로 알린다.
넣은 모델 파일은 지우지 않는다 — 메시를 거기서 읽는다.

**처음 한 장은 혼자 넣어 본다.** `Wall_C0_Base` 하나로 크기·앞면을 확인한 뒤 나머지를 뽑는다 — 도구는 Meshy GLB 로 맞춰 왔고, Tripo GLB 의 정면 방향은 첫 장으로 확인한다.

## 넣은 뒤 볼 것

- 콘솔 `[MeshyArt]` 에
  - "기준을 넘는다" — 면 수를 더 줄여 다시 뽑는다.
  - "모양이 …% 늘거나 줄었다" — 길쭉하거나 납작한 그림이다. 어색하면 다시 뽑는다.
  - "몸통 윗면을 못 찾아" / "몸통을 …% 높이로 낮췄다" — 솟은 장식이 너무 크다. 다시 뽑는다.
  - "표에 없는 이름" — 파일 이름을 고친다.
- 그 챕터 층에서
  - 벽이 옛 벽과 같은 크기로 칸을 채우는가, 이웃 벽과 틈 없이 붙는가.
  - 벽감·창살·배수구가 **앞면**(카메라 쪽)에 보이는가 — 안 보이면 정면이 뒤를 향했다.
  - 벽등·사슬이 벽 앞면에 붙어 있는가(뜨거나 묻히지 않는가).
  - 큰 것이 방 안쪽 벽 자리에 서 있고, 그 칸으로 걸어 들어갈 수 없는가.
  - 바닥 소품 위로 걸어 지나갈 수 있는가.
  - 불빛이 어두우면 `MapDecoSetup` 의 챕터 표 `LightRange` 를 올린다.

---

## 챕터 0 이끼 낀 지하 묘소 (5~20층)

> Tripo 시험(2026-11-11)에서 원래 문장은 초록 윗면 + 판자 같은 옆면, 곧 흔한 잔디 블록처럼 나왔다. 그래서 챕터 0 벽 네 줄만 Meshy 판과 다르다 — 줄눈이 보이는 돌 벽돌, 이끼는 가장자리에 조금·어둡게, 칙칙한 색(muted desaturated colors).

한낮의 무덤. 따뜻한 회갈색 벽돌에 이끼가 위에서 아래로 흘러내린다.
지킬 것: 칼 꽂힌 무덤 표지의 칼은 평범한 쇠칼(눈 없음). 벽 윗면에 숲이나 늘어진 뿌리를 올리지 않는다.

### 벽

**Wall_C0_Base** · 기본형
```
a square dungeon wall block, one tile wide and one tile deep, about as tall as it is wide, flat top, all four sides finished, made of worn grey-brown stone grave bricks with clearly visible dark mortar lines, flat stone top with only a few small patches of dark muted green moss near the edges, muted desaturated colors, stylized low-poly dungeon game asset, chunky blocky shapes like voxel art, flat hand-painted colors, matte, no baked shadows, no text or letters, single object, isolated
```
**Wall_C0_A** · 금 간 벽
```
a square dungeon wall block, one tile wide and one tile deep, about as tall as it is wide, flat top, all four sides finished, made of cracked worn grey-brown stone grave bricks with clearly visible dark mortar lines and a few chipped corners, flat stone top, small patches of dark muted green moss only in the cracks, muted desaturated colors, stylized low-poly dungeon game asset, chunky blocky shapes like voxel art, flat hand-painted colors, matte, no baked shadows, no text or letters, single object, isolated
```
**Wall_C0_B** · 벽감 벽
```
a square dungeon wall block, one tile wide and one tile deep, about as tall as it is wide, flat top, all four sides finished, made of worn grey-brown stone grave bricks with clearly visible dark mortar lines, the front side has a small empty arched niche with a melted candle stub, flat stone top with only a few small patches of dark muted green moss near the edges, muted desaturated colors, stylized low-poly dungeon game asset, chunky blocky shapes like voxel art, flat hand-painted colors, matte, no baked shadows, no text or letters, single object, isolated
```
**Wall_C0_C** · 무너진 벽
```
a square dungeon wall block, one tile wide and one tile deep, about as tall as it is wide, flat top, all four sides finished, made of worn grey-brown stone grave bricks with clearly visible dark mortar lines, one top corner crumbled away with loose bricks, small patches of dark muted green moss only on the broken edge, muted desaturated colors, stylized low-poly dungeon game asset, chunky blocky shapes like voxel art, flat hand-painted colors, matte, no baked shadows, no text or letters, single object, isolated
```

### 소품

**Prop_C0_Light** · 빛 · 벽 촛대 (지금 횃불 자리) — 벽면
```
an iron wall sconce holding three thick wax candles with simple flame shapes, mounted flat against a wall, stylized low-poly dungeon game asset, chunky blocky shapes like voxel art, flat hand-painted colors, matte, no baked shadows, no text or letters, single object, isolated
```
**Prop_C0_Wall** · 벽 소품 · 이끼 낀 사슬 (지금 사슬 자리) — 벽면
```
a rusty iron chain with an open shackle hanging from a wall ring, green moss on the links, stylized low-poly dungeon game asset, chunky blocky shapes like voxel art, flat hand-painted colors, matte, no baked shadows, no text or letters, single object, isolated
```
**Prop_C0_Floor** · 바닥 · 칼이 꽂힌 무덤 표지 — 빈 바닥(발목 높이로 줄어든다)
```
a grave marker: a plain straight iron sword planted point-down in a small mound of earth and stones, no ornaments, no eyes, stylized low-poly dungeon game asset, chunky blocky shapes like voxel art, flat hand-painted colors, matte, no baked shadows, no text or letters, single object, isolated
```
**Prop_C0_Big** · 바닥 · 닫힌 석관 — 방 안쪽 벽 한 칸 대신
```
a closed stone sarcophagus with a plain flat lid, weathered warm grey-brown stone, moss along the edges, stylized low-poly dungeon game asset, chunky blocky shapes like voxel art, flat hand-painted colors, matte, no baked shadows, no text or letters, single object, isolated
```

## 챕터 1 무너진 수로 (21~40층)

흐린 오후의 물길. 젖은 청회색 돌, 위에서 흘러내린 물 얼룩, 녹.
지킬 것: 윗면을 번들거리게 만들지 않는다. 밝은 돌색이어야 어두운 바닥과 구별된다.

### 벽

**Wall_C1_Base** · 기본형
```
a square dungeon wall block, one tile wide and one tile deep, about as tall as it is wide, flat top, all four sides finished, made of wet blue-grey stone blocks, dark water stains streaking down from the top edge, pale damp flat top, stylized low-poly dungeon game asset, chunky blocky shapes like voxel art, flat hand-painted colors, matte, no baked shadows, no text or letters, single object, isolated
```
**Wall_C1_A** · 배수구 벽
```
a square dungeon wall block, one tile wide and one tile deep, about as tall as it is wide, flat top, all four sides finished, solid block, made of wet dull blue-grey stone blocks, a small rusty iron drain grate on the front face facing the viewer, with a dark water stain below it, flat solid stone top, muted desaturated colors, stylized low-poly dungeon game asset, chunky blocky shapes like voxel art, flat hand-painted colors, matte, no baked shadows, no text or letters, single object, isolated
```
**Wall_C1_B** · 창살 벽
```
a square dungeon wall block, one tile wide and one tile deep, about as tall as it is wide, flat top, all four sides finished, completely solid block, not hollow, made of wet dull blue-grey stone blocks, a small barred window with rusty iron bars set into the front face facing the viewer, flat solid closed stone top, muted desaturated colors, stylized low-poly dungeon game asset, chunky blocky shapes like voxel art, flat hand-painted colors, matte, no baked shadows, no text or letters, single object, isolated
```
**Wall_C1_C** · 무너진 벽
```
a square dungeon wall block, one tile wide and one tile deep, about as tall as it is wide, flat top, all four sides finished, completely solid block in one piece, made of wet dull blue-grey stone blocks, one top corner crumbled, a few loose broken arch stones resting on the flat solid top, no gap or split through the block, muted desaturated colors, stylized low-poly dungeon game asset, chunky blocky shapes like voxel art, flat hand-painted colors, matte, no baked shadows, no text or letters, single object, isolated
```

### 소품

**Prop_C1_Light** · 빛 · 푸른 벽등 — 벽면
```
an old iron wall lantern with pale blue glass panes, mounted against a wall, stylized low-poly dungeon game asset, chunky blocky shapes like voxel art, flat hand-painted colors, matte, no baked shadows, no text or letters, single object, isolated
```
**Prop_C1_Wall** · 벽 소품 · 밧줄과 두레박 — 벽면
```
a coiled rope and an old wooden bucket hanging from an iron wall hook, damp wood, stylized low-poly dungeon game asset, chunky blocky shapes like voxel art, flat hand-painted colors, matte, no baked shadows, no text or letters, single object, isolated
```
**Prop_C1_Big** · 바닥 · 멈춘 물레바퀴 조각 — 방 안쪽 벽 한 칸 대신
```
a broken segment of a wooden water wheel leaning upright, stopped, wet dark wood with rusty iron bands, stylized low-poly dungeon game asset, chunky blocky shapes like voxel art, flat hand-painted colors, matte, no baked shadows, no text or letters, single object, isolated
```
**Prop_C1_Floor** · 바닥 · 녹슨 바닥 창살 — 빈 바닥(얇게)
```
a very flat rusty iron floor grate set into a square stone slab, dark water visible below, stylized low-poly dungeon game asset, chunky blocky shapes like voxel art, flat hand-painted colors, matte, no baked shadows, no text or letters, single object, isolated
```

## 챕터 2 잿빛 용광로 (41~60층)

마검이 벼려진 대장간. 그을린 무쇠 위에 흰 재가 쌓이고, 빛은 아래의 불에서 온다.
지킬 것: 모루는 넣지 않는다. 벽 윗면의 흰 재가 바닥과의 대비를 만든다(지금 46층은 벽이 바닥에 묻힌다).

### 벽

**Wall_C2_Base** · 기본형
```
a square dungeon wall block, one tile wide and one tile deep, about as tall as it is wide, flat top, all four sides finished, made of soot-blackened cast iron and dark brick, a thick layer of pale white ash on the flat top, stylized low-poly dungeon game asset, chunky blocky shapes like voxel art, flat hand-painted colors, matte, no baked shadows, no text or letters, single object, isolated
```
**Wall_C2_A** · 통풍구 벽 ★
```
a square dungeon wall block, one tile wide and one tile deep, about as tall as it is wide, flat top, all four sides finished, completely solid block, made of soot-blackened dark brick with a few iron bands, a small rectangular furnace vent on the front face facing the viewer with dull orange glow inside, a thick layer of pale white ash on the flat top, stylized low-poly dungeon game asset, chunky blocky shapes like voxel art, flat hand-painted colors, matte, no baked shadows, no text or letters, single object, isolated
```
**Wall_C2_B** · 굴뚝 벽
```
a square dungeon wall block, one tile wide and one tile deep, about as tall as it is wide, flat top, all four sides finished, made of soot-blackened dark brick, a short broken iron chimney pipe sticking out of the top, white ash around it, stylized low-poly dungeon game asset, chunky blocky shapes like voxel art, flat hand-painted colors, matte, no baked shadows, no text or letters, single object, isolated
```
**Wall_C2_C** · 무너진 벽
```
a square dungeon wall block, one tile wide and one tile deep, about as tall as it is wide, flat top, all four sides finished, made of soot-blackened dark brick, one corner crumbled, cooled dark slag dripping down the front side, white ash on top, stylized low-poly dungeon game asset, chunky blocky shapes like voxel art, flat hand-painted colors, matte, no baked shadows, no text or letters, single object, isolated
```

### 소품

**Prop_C2_Light** · 빛 · 화로 (지금 화로 자리) — 빈 바닥
```
a round iron brazier on three short legs filled with glowing orange coals, stylized low-poly dungeon game asset, chunky blocky shapes like voxel art, flat hand-painted colors, matte, no baked shadows, no text or letters, single object, isolated
```
**Prop_C2_Wall** · 벽 소품 · 도르래 사슬과 갈고리 — 벽면
```
an iron pulley on a wall bracket with a hanging chain and a heavy hook, covered in soot, stylized low-poly dungeon game asset, chunky blocky shapes like voxel art, flat hand-painted colors, matte, no baked shadows, no text or letters, single object, isolated
```
**Prop_C2_Big** · 바닥 · 가죽 풀무 — 방 안쪽 벽 한 칸 대신
```
a large leather forge bellows with wooden handles and an iron nozzle, sooty, stylized low-poly dungeon game asset, chunky blocky shapes like voxel art, flat hand-painted colors, matte, no baked shadows, no text or letters, single object, isolated
```
**Prop_C2_Floor** · 바닥 · 슬래그 더미 — 빈 바닥
```
a low pile of dark slag lumps with faint glowing orange cracks, stylized low-poly dungeon game asset, chunky blocky shapes like voxel art, flat hand-painted colors, matte, no baked shadows, no text or letters, single object, isolated
```
**Prop_C2_Extra** · [선택] 바닥 · 휜 칼 모양 빈 거푸집 (복선) — 빈 바닥
```
an empty stone casting mold with a hollow in the shape of a curved crescent blade, nothing inside, stylized low-poly dungeon game asset, chunky blocky shapes like voxel art, flat hand-painted colors, matte, no baked shadows, no text or letters, single object, isolated
```

## 챕터 3 얼어붙은 심층 (61~80층)

탑지기들의 얼어붙은 초소. 서리 낀 돌, 눈 모자, 고드름. 해가 낮아 그림자가 길다.
지킬 것: 따뜻한 빛은 '마지막 불씨' 하나뿐이다 — 층마다 하나 놓인다(`Prop_C3_Light2`). 벽등은 차가운 빛이다.

### 벽

**Wall_C3_Base** · 기본형
```
a square dungeon wall block, one tile wide and one tile deep, about as tall as it is wide, flat top, all four sides finished, made of frost-covered blue-grey stone, a thick cap of snow on the flat top, stylized low-poly dungeon game asset, chunky blocky shapes like voxel art, flat hand-painted colors, matte, no baked shadows, no text or letters, single object, isolated
```
**Wall_C3_A** · 고드름 벽
```
a square dungeon wall block, one tile wide and one tile deep, about as tall as it is wide, flat top, all four sides finished, made of frost-covered blue-grey stone, icicles hanging from the top edge, snow cap on top, stylized low-poly dungeon game asset, chunky blocky shapes like voxel art, flat hand-painted colors, matte, no baked shadows, no text or letters, single object, isolated
```
**Wall_C3_B** · 화살 구멍 벽
```
a square dungeon wall block, one tile wide and one tile deep, about as tall as it is wide, flat top, all four sides finished, made of frost-covered blue-grey stone, the front side has a narrow vertical arrow slit, snow cap on top, stylized low-poly dungeon game asset, chunky blocky shapes like voxel art, flat hand-painted colors, matte, no baked shadows, no text or letters, single object, isolated
```
**Wall_C3_C** · 무너진 벽
```
a square dungeon wall block, one tile wide and one tile deep, about as tall as it is wide, flat top, all four sides finished, made of frost-covered blue-grey stone, cracked by frost with a chunk broken off, snow piled in the break, stylized low-poly dungeon game asset, chunky blocky shapes like voxel art, flat hand-painted colors, matte, no baked shadows, no text or letters, single object, isolated
```

### 소품

**Prop_C3_Light** · 빛 · 차가운 푸른 벽등 — 벽면
```
a frosted iron wall lantern with a cold pale blue flame, small icicles hanging from its base, stylized low-poly dungeon game asset, chunky blocky shapes like voxel art, flat hand-painted colors, matte, no baked shadows, no text or letters, single object, isolated
```
**Prop_C3_Wall** · 벽 소품 · 얼어붙은 창 걸이 — 벽면
```
a wooden wall rack holding two old spears, all covered in ice and frost, stylized low-poly dungeon game asset, chunky blocky shapes like voxel art, flat hand-painted colors, matte, no baked shadows, no text or letters, single object, isolated
```
**Prop_C3_Big** · 바닥 · 갑옷 걸이 — 방 안쪽 벽 한 칸 대신
```
an empty armor stand wearing a dented guard helmet and breastplate, frozen and frosted, stylized low-poly dungeon game asset, chunky blocky shapes like voxel art, flat hand-painted colors, matte, no baked shadows, no text or letters, single object, isolated
```
**Prop_C3_Floor** · 바닥 · 얼음에 갇힌 걸상 — 빈 바닥
```
a small wooden stool encased in a block of clear blue ice, stylized low-poly dungeon game asset, chunky blocky shapes like voxel art, flat hand-painted colors, matte, no baked shadows, no text or letters, single object, isolated
```
**Prop_C3_Light2** · [선택] 빛 2 · 마지막 불씨 화로 — 빈 바닥, 층마다 하나
```
a small iron brazier with a few last glowing embers in grey ash, frost creeping around the rim, stylized low-poly dungeon game asset, chunky blocky shapes like voxel art, flat hand-painted colors, matte, no baked shadows, no text or letters, single object, isolated
```

## 챕터 4 왕좌의 균열 (81~100층)

검은 태양이 낸 틈. 금 간 흑요석, 보랏빛 광택. 모든 것이 위로 떠오르려 한다.
지킬 것: 왕좌 소품은 만들지 않는다. 사람의 흔적처럼 보이지 않게 한다.

### 벽

**Wall_C4_Base** · 기본형
```
a square dungeon wall block, one tile wide and one tile deep, about as tall as it is wide, flat top, all four sides finished, made of cracked black obsidian with glossy pale purple highlights, a few small rock fragments floating just above the flat top, stylized low-poly dungeon game asset, chunky blocky shapes like voxel art, flat hand-painted colors, matte, no baked shadows, no text or letters, single object, isolated
```
**Wall_C4_A** · 빛 균열 벽
```
a square dungeon wall block, one tile wide and one tile deep, about as tall as it is wide, flat top, all four sides finished, made of black obsidian with thin glowing violet cracks running upward, stylized low-poly dungeon game asset, chunky blocky shapes like voxel art, flat hand-painted colors, matte, no baked shadows, no text or letters, single object, isolated
```
**Wall_C4_B** · 가시 벽
```
a square dungeon wall block, one tile wide and one tile deep, about as tall as it is wide, flat top, all four sides finished, made of black obsidian, sharp obsidian spikes growing straight up from the top, stylized low-poly dungeon game asset, chunky blocky shapes like voxel art, flat hand-painted colors, matte, no baked shadows, no text or letters, single object, isolated
```
**Wall_C4_C** · 부서진 벽
```
a square dungeon wall block, one tile wide and one tile deep, about as tall as it is wide, flat top, all four sides finished, solid body made of cracked black obsidian with glossy pale purple highlights, the same black obsidian as the rest, the top edge broken, a few small black obsidian chunks floating just above the top, close to the block, stylized low-poly dungeon game asset, chunky blocky shapes like voxel art, flat hand-painted colors, matte, no baked shadows, no text or letters, single object, isolated
```

### 소품

**Prop_C4_Light** · 빛 · 보랏빛 수정 무리 — 빈 바닥
```
a cluster of glowing violet crystals growing out of a dark rock, stylized low-poly dungeon game asset, chunky blocky shapes like voxel art, flat hand-painted colors, matte, no baked shadows, no text or letters, single object, isolated
```
**Prop_C4_Wall** · 벽 소품 · 위로 뜬 쇠사슬 (지금 사슬 자리) — 벽면
```
an iron chain attached to a wall ring, the loose end floating straight upward as if gravity pulls it up, stylized low-poly dungeon game asset, chunky blocky shapes like voxel art, flat hand-painted colors, matte, no baked shadows, no text or letters, single object, isolated
```
**Prop_C4_Floor** · 바닥 · 떠 있는 돌 조각 — 빈 바닥(바닥에 앉고 발목 높이로 줄어든다)
```
a floating chunk of black obsidian rock with a jagged pointed bottom, levitating, stylized low-poly dungeon game asset, chunky blocky shapes like voxel art, flat hand-painted colors, matte, no baked shadows, no text or letters, single object, isolated
```
**Prop_C4_Big** · 바닥 · 부서진 계단 조각 — 방 안쪽 벽 한 칸 대신
```
a broken fragment of a dark stone staircase, three cracked steps, stylized low-poly dungeon game asset, chunky blocky shapes like voxel art, flat hand-painted colors, matte, no baked shadows, no text or letters, single object, isolated
```

