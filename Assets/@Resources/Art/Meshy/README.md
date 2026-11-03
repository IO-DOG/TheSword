# Meshy 결과물 넣는 곳

Meshy 에서 뽑은 모델(GLB 권장, FBX·OBJ 도 된다)을 이 폴더에 이름 규칙대로 넣고, Unity 메뉴

1. **TheSword → Import Meshy Art** (크기·바닥 높이·재질·어드레서블을 알아서 맞춘다)
2. **TheSword → Build Map Decorations** (소품을 새로 넣었으면)

를 누르면 게임에 들어간다. 이름 규칙(`Wall_C{챕터}_{Base|A|B|C}`, `Prop_C{챕터}_{Light|Light2|Wall|Floor|Extra|Big}`)과
프롬프트는 `Documents/TheSword_Meshy_Prompts.md`. 같은 이름이 `../ChapterKit/`(게임 복셀 그림체로 만든 기본 그림)에도
있으면 **이 폴더의 것이 이긴다.**
