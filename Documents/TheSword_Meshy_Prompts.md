# TheSword 챕터별 Meshy 3D 프롬프트

챕터마다 벽 4종, 빛 소품 1종, 벽 소품 1종, 바닥 대표 소품 2종입니다. [선택]은 여유가 있을 때만 만들면 됩니다. 회색 상자 안의 문장을 통째로 Meshy 프롬프트 칸에 붙이면 됩니다. 프롬프트 위의 굵은 이름이 **내보낸 파일의 이름**입니다 — 이름이 곧 놓일 자리라서, 이름만 맞으면 나머지는 도구가 합니다.
같은 내용이 노션에도 있습니다: 챕터별 Meshy 3D 프롬프트 (벽·대표 소품) — 노션 쪽에는 아래 "검토 결과" 의 수정(이름·벽 앞면·넣는 법)이 아직 없습니다.

## 먼저 읽을 것

- 모든 프롬프트 끝에 같은 스타일 꼬리말이 붙어 있다. 지우지 않아야 다섯 챕터가 한 게임처럼 보인다.
- Meshy에서 스타일 옵션을 고를 수 있으면 사실적(Realistic)보다 로우폴리·복셀 계열을 고른다.
- 명암은 Unity의 챕터 해가 넣는다. 텍스처에 그림자가 구워져 나오면 다시 뽑는다.
- **색은 그림 그대로 나온다.** 지금 벽은 챕터 00 그림 한 벌에 챕터 색을 곱해 쓰지만, 새 그림에는 곱하지 않는다. 수로의 밝은 돌색, 용광로 벽 윗면의 흰 재가 그대로 보인다.
- 폴리곤은 Remesh로 줄인다. 벽은 삼각형 3,000개 이하, 소품은 2,000개 이하를 기준으로 한다(지금 벽은 1,510~2,222개). 넘으면 들일 때 콘솔이 알려 준다.
- 텍스처는 도구가 512 이하로 다시 굽고 Point 필터를 건다(도트 몬스터와 섞이게). Meshy에서는 가장 작은 해상도로 받아도 된다. 바탕색(base color)만 쓴다 — 노멀·거칠기 맵은 있어도 무시된다.
- **크기·높이·피벗은 맞추지 않는다.** 도구가 지금 그림을 재서 맞춘다(아래 "넣는 법"). 지금 벽과 겹쳐 놓고 맞출 일이 없다.
- **Meshy 미리보기의 정면이 게임에서 카메라 쪽을 본다.** 카메라는 남쪽에서 50°로 내려다봐서 벽은 윗면과 앞면(남쪽 면)만 보인다. 벽감·배수구·창살·화살 구멍·흘러내린 슬래그는 정면에 와야 보인다 — 그래서 그 다섯 벽의 프롬프트는 "one side" 를 "the front side" 로 고쳤다. 벽에 거는 것은 등이 평평하고 정면이 앞을 보게 뽑는다.
- 놓는 자리는 이름이 정한다.
  - 벽에 거는 것(Light 0·1·3, Wall)은 카메라 쪽 벽면에 걸린다.
  - 바닥에 서는 것(Floor, Extra, 챕터 2·4의 Light, Light2)은 걸어 다니는 칸에 놓이므로 도구가 발목 높이(0.08)로 줄인다. 길을 막는 것처럼 보이면 퍼즐이 거짓말을 하기 때문이다. 키 큰 모양은 작아진다 — 화로·수정 무리는 낮고 넓게 뽑는 편이 낫다.
  - 큰 것(Big — 석관처럼 "벽 옆 빈칸" 이라 적었던 것)은 방 안쪽 벽 한 칸을 대신한다. 그 칸은 여전히 벽이라 못 지나간다. 생성 층에는 벽 옆 빈칸이 따로 없다.
- 글자, 비문, 낙서는 넣지 않는다. 게임의 네 언어(한·영·일·중)로 옮겨지지 않는다.
- 문은 다섯 챕터가 지금 문을 같이 쓴다 — Meshy 대상이 아니다. 지금 문은 초록·노랑·빨강 세 색의 같은 아치형 판자문이다(원·세모·마름모 표시는 아직 없다).
- 바닥 시트와 공기 알갱이는 2D라 Meshy 대상이 아니다.
- Meshy로 만든 에셋을 실제로 쓰면 Steam의 AI 콘텐츠 공개에 더한다 — `Steam/store/*.md` 의 Meshy 한 줄을 채운다(`Steam/README.md` AI 공개 항목). 상업 이용 조건은 쓰는 요금제의 약관을 확인한다.

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

자리마다 하는 일 (크기는 지금 그림을 재서 맞춘다 — 고르게 키우거나 줄이기만 하고 찌그러뜨리지 않는다)

| 자리 | 어디에 | 크기 | 층마다 |
|---|---|---|---|
| Wall_C?_Base·A·B·C | 벽 칸 전부 | 한 칸(0.32)을 채우고 지금 벽 바닥면(y 0.2)에 앉는다. 높이는 비율대로, 지금 벽(0.30)의 1.5배까지 | 기본형 약 80%, 나머지 셋 약 7%씩. 같은 변형은 붙어 서지 않는다 |
| Light (챕터 0·1·3) | 카메라 쪽 벽면 | 지금 횃불(불꽃 포함) 가로·세로 안 | 5·4·3개, 점광원 하나씩 |
| Light (챕터 2·4) | 벽에 붙은 빈 바닥(걷는 칸) | 지금 화로 발자국(0.19) 안, 높이 0.08(발목) 안 | 5·4개, 점광원 하나씩 |
| Wall | 카메라 쪽 벽면 | 지금 사슬의 가장 긴 변(0.2) 안 | 4~8개 |
| Floor · Extra | 벽에 붙은 빈 바닥(걷는 칸, 몬스터·아이템·문·계단 칸은 뺀다) | 화로 발자국 안, 높이 0.08(발목) 안 | 약 8개 · 약 3개 |
| Big | 방 안쪽 벽 한 칸 대신(그 칸은 여전히 막혀 있다) | 벽 한 칸 | 2개 |
| Light2 | 벽에 붙은 빈 바닥(걷는 칸) | 화로 발자국 안, 높이 0.08 안 | 1개, 따뜻한 점광원 |

- 그림이 없는 자리는 지금 모습 그대로다. 벽 변형이 빠지면 그 챕터의 기본형이, 기본형도 없으면 지금 벽이 선다.
- 소품에는 콜라이더가 없고, 새 벽은 지금 벽의 콜라이더를 그대로 갖는다 — 길·충돌·전투 예측이 한 칸도 달라지지 않는다.

## 넣는 법

1. **Meshy에서 내보내기** — 형식은 **GLB**(앞면 방향이 정확하다. FBX·OBJ도 받지만 앞이 돌아갈 수 있다). Remesh는 삼각형(triangle)으로, 벽 3,000·소품 2,000 이하.
2. **파일 이름을 이름표대로** 바꿔 `Assets/@Resources/Art/Meshy/` 에 넣는다. 예: `Wall_C1_Base.glb`, `Prop_C3_Light2.glb`. 대소문자는 도구가 맞춘다. Meshy가 붙인 긴 이름은 그대로 두면 건너뛴다.
   `Assets/@Resources/Art/ChapterKit/` 에는 같은 이름의 임시 복셀 그림이 있을 수 있다. 같은 이름이면 **Meshy 쪽이 이긴다** — 하나씩 넣고 다시 Import 하면 그 자리만 바뀐다.
3. Unity 메뉴 **TheSword → Import Meshy Art**. 콘솔의 `[MeshyArt]` 줄에 파일마다 맞춘 크기와 삼각형 수가 찍힌다.
4. **소품(Prop_)을 처음 넣었으면** 메뉴 **TheSword → Build Map Decorations**(층 장식 96개를 다시 굽는다). 벽만 넣었거나 이미 있던 소품을 다시 넣었으면 할 일이 없다.
5. 플레이해서 그 챕터 층을 본다(아래 "넣은 뒤 볼 것").

도구가 알아서 하는 것 — 손댈 일이 없다.
- 지금 그림(벽 `Tilemap_C00_W01~03`, `Deco_Torch`·`Deco_Handcuff`·`Deco_FireBowl`)을 열어 재고, 위 표대로 크기·높이·피벗을 맞춘다.
- 텍스처를 512 이하 PNG로 다시 굽고 Point 필터·밉맵을 건다(복셀 팔레트 256x1은 손대지 않고 복사해 압축·밉맵 없이). 재질은 지금 벽 재질을 복사해 그림만 바꾼다 — 같은 빛을 받는다.
- 바탕색 그림 옆에 `{그림 이름}_Emit.png`(같은 배치에서 빛나는 칸만 켜고 나머지는 검정)가 있으면 그 칸이 빛난다 — 불꽃·숯·등 유리·수정. 복셀 키트가 쓰고, 그림이 모델 속에 든 Meshy GLB는 지금처럼 빛나지 않는다.
- 콜라이더·빛·애니메이션은 떼고, 벽에만 지금 벽의 콜라이더·레이어를 단다.
- 프리팹 `Assets/@Resources/Art/Built/{이름}.prefab` 을 만들고 어드레서블 "PreLoad" 로 등록한다.
- 같은 이름을 다시 넣으면 그 프리팹 속만 바꾼다(층 장식의 자리는 그대로).
- 넣은 모델 파일은 지우지 않는다 — 메시를 거기서 읽는다.

## 넣은 뒤 볼 것

- 콘솔 `[MeshyArt]` 에 "기준을 넘는다" 가 있으면 Remesh를 더 줄여 다시 넣는다. "표에 없는 이름" 은 파일 이름을 고친다.
- 그 챕터 층에서
  - 벽감·창살·배수구가 **앞면**(카메라 쪽)에 보이는가 — 안 보이면 Meshy 정면이 뒤를 향했다. 다시 뽑거나 GLB로 다시 받는다.
  - 벽등·사슬이 벽 앞면에 붙어 있는가(뜨거나 묻히지 않는가).
  - 큰 것이 방 안쪽 벽 자리에 서 있고, 그 칸으로 걸어 들어갈 수 없는가.
  - 바닥 소품 위로 걸어 지나갈 수 있는가.
  - 불빛: 새 불은 점광원이 하나다(지금 횃불·화로 프리팹은 제 안에 하나씩 더 갖고 있었다). 어두우면 `MapDecoSetup` 의 챕터 표 `LightRange` 를 올린다.

---

## 챕터 0 이끼 낀 지하 묘소 (5~20층)

한낮의 무덤. 따뜻한 회갈색 벽돌에 이끼가 위에서 아래로 흘러내린다.
지킬 것: 칼 꽂힌 무덤 표지의 칼은 평범한 쇠칼(눈 없음). 벽 윗면에 숲이나 늘어진 뿌리를 올리지 않는다.

### 벽

**Wall_C0_Base** · 기본형
```
a square dungeon wall block, one tile wide and one tile deep, about as tall as it is wide, flat top, all four sides finished, made of worn warm grey-brown grave bricks, green moss growing on the flat top and trickling down the sides, stylized low-poly dungeon game asset, chunky blocky shapes like voxel art, flat hand-painted colors, matte, no baked shadows, no text or letters, single object, isolated
```
**Wall_C0_A** · 금 간 벽
```
a square dungeon wall block, one tile wide and one tile deep, about as tall as it is wide, flat top, all four sides finished, made of cracked warm grey-brown grave bricks with a few chipped corners, moss growing in the cracks, stylized low-poly dungeon game asset, chunky blocky shapes like voxel art, flat hand-painted colors, matte, no baked shadows, no text or letters, single object, isolated
```
**Wall_C0_B** · 벽감 벽
```
a square dungeon wall block, one tile wide and one tile deep, about as tall as it is wide, flat top, all four sides finished, made of warm grey-brown grave bricks, the front side has a small empty arched niche with a melted candle stub, moss on top, stylized low-poly dungeon game asset, chunky blocky shapes like voxel art, flat hand-painted colors, matte, no baked shadows, no text or letters, single object, isolated
```
**Wall_C0_C** · 무너진 벽
```
a square dungeon wall block, one tile wide and one tile deep, about as tall as it is wide, flat top, all four sides finished, made of warm grey-brown grave bricks, one top corner crumbled away with loose bricks, moss on the broken edge, stylized low-poly dungeon game asset, chunky blocky shapes like voxel art, flat hand-painted colors, matte, no baked shadows, no text or letters, single object, isolated
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
a square dungeon wall block, one tile wide and one tile deep, about as tall as it is wide, flat top, all four sides finished, made of wet blue-grey stone blocks, the front side has a small rusty iron drain grate with a water stain below it, stylized low-poly dungeon game asset, chunky blocky shapes like voxel art, flat hand-painted colors, matte, no baked shadows, no text or letters, single object, isolated
```
**Wall_C1_B** · 창살 벽
```
a square dungeon wall block, one tile wide and one tile deep, about as tall as it is wide, flat top, all four sides finished, made of wet blue-grey stone blocks, the front side has a small window with rusty iron bars, stylized low-poly dungeon game asset, chunky blocky shapes like voxel art, flat hand-painted colors, matte, no baked shadows, no text or letters, single object, isolated
```
**Wall_C1_C** · 무너진 벽
```
a square dungeon wall block, one tile wide and one tile deep, about as tall as it is wide, flat top, all four sides finished, made of wet blue-grey stone blocks, half collapsed, broken arch stones resting on the top, stylized low-poly dungeon game asset, chunky blocky shapes like voxel art, flat hand-painted colors, matte, no baked shadows, no text or letters, single object, isolated
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
**Wall_C2_A** · 리벳 벽
```
a square dungeon wall block, one tile wide and one tile deep, about as tall as it is wide, flat top, all four sides finished, made of riveted soot-blackened iron plates with scorch marks, pale white ash on the flat top, stylized low-poly dungeon game asset, chunky blocky shapes like voxel art, flat hand-painted colors, matte, no baked shadows, no text or letters, single object, isolated
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
a square dungeon wall block, one tile wide and one tile deep, about as tall as it is wide, flat top, all four sides finished, made of black obsidian, the top broken into chunks that float upward above the block, stylized low-poly dungeon game asset, chunky blocky shapes like voxel art, flat hand-painted colors, matte, no baked shadows, no text or letters, single object, isolated
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

---

## 검토 결과 — 원문에서 고친 것과 까닭

1. **벽 이름.** 원문대로 "W02 기본형을 W02로, 나머지를 W00·W01·W03과 짝지어" 이름 붙이면 다섯 챕터 모두 챕터 00 그림만 읽힌다 — 맵 조립(`MapBuilder`)이 벽 이름을 `Tilemap_C00_W{번호}` 로 못박아 두었다. 데이터의 벽 번호는 "역할" 도 아니다. 칸마다 그 챕터의 두 번호 중 하나를 무작위로 고른 것이고(챕터 0은 W01·W02, 1은 W02·W03 …), 번호를 바꾸면 맵 데이터가 바뀌어 세이브가 깨진다. W00은 벽이 아니라 손수 만든 1~2층 프리팹에만 들어 있는 높이 0.2짜리 다른 모델이고, 생성 층이 읽을 `Tilemap_C00_W00` 은 없다. → 새 이름 `Wall_C{챕터}_Base/A/B/C`, 변형은 칸 자리로 고른다(데이터는 그대로).
2. **"Tilemap_C0_W02"** 는 OBJ 원본 이름이고, 맵에 놓이는 것은 그것을 0.1배로 놓은 프리팹 `Tilemap_C00_W02` 다. 겹쳐 맞출 필요도 없어졌다 — 도구가 잰다.
3. **벽 크기.** 칸 간격 0.32·바닥면 y 0.2·높이 0.30은 맞다. 다만 지금 기본형 W02는 발자국이 0.30 x 0.30이다(W01은 0.32 x 0.32에 높이 0.22). 새 벽은 칸(0.32)을 꽉 채운다.
4. **"벽에 거는 소품은 벽면 아래쪽에 붙는다"** — 실제로는 지금 횃불·사슬이 벽 칸 한가운데에 놓여 벽 속에 들어가 있다(횃불 너비 0.06, 벽 0.30). 새 소품은 카메라가 보는 남쪽 벽면에 건다. 같은 까닭으로 벽의 장식 면은 정면이어야 해서 다섯 벽 프롬프트의 "one side" 를 "the front side" 로 고쳤다(벽감·배수구·창살·화살 구멍·슬래그).
5. **"석관처럼 큰 것은 벽 옆 빈칸에"** — 생성 층에는 벽 옆 빈칸이 없다. 바닥에 닿은 빈칸은 생성기가 모두 벽으로 채운다. 걷는 칸에 세우면 길을 막는 것처럼 보여 퍼즐이 거짓말을 한다. → 큰 것은 방 안쪽 벽 한 칸을 대신한다(그 칸은 그대로 막혀 있다). "바닥" 에 서는 것은 바닥 불(화로·수정 무리·마지막 불씨)까지 걷는 칸이라 도구가 발목 높이로 줄인다 — 칼이 꽂힌 무덤 표지와 떠 있는 돌 조각은 작아진다. 지금 화로는 이 규칙보다 먼저 놓여 높이 0.14에 불꽃까지 서 있고, 25개는 몬스터·아이템·문·계단 칸 위에 겹쳐 있다(챕터 2·4). 새 것은 빈 바닥에만 선다.
6. **문 표시.** 다섯 챕터가 지금 문을 같이 쓰는 것은 맞다. 그런데 지금 문은 세 색이 모양까지 같은 판자문이고, 원·세모·마름모는 `TheSword_Art_Music_Prompts.md` 의 새 문 제안에만 있다.
7. **언어 수.** 게임은 네 언어다. 다섯은 스토어 페이지(번체 포함)다.
8. **챕터 3의 따뜻한 빛.** 지금은 층마다 횃불 셋이 저마다 주황 점광원을 품고 있어 따뜻한 빛이 셋이다. 새 그림을 넣으면 벽등 셋은 차가운 빛, 마지막 불씨만 따뜻하다(층마다 하나).
9. **불빛 수.** 지금 횃불·화로 프리팹은 제 안에 점광원(범위 20·10)을 하나씩 더 갖고 있어 층마다 실제 점광원이 불의 두 배다(6~10개). 새 불은 불마다 하나라 층당 3~5개가 된다 — 더 가볍고, 대신 덜 밝을 수 있다.
10. **AI 공개 자리.** Steam 쪽에 Meshy 한 줄이 이미 준비돼 있다(`Steam/store/*.md`). 실제로 쓴 것만 채운다.
