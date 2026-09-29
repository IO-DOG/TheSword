"""생성 콘텐츠의 이름·설명 (네 언어).

원본은 스토리 팀의 Tools/story/story_{kr,en,jp,cn}.json 의 "bestiary" 다
(형식은 Tools/story/FORMAT.md). 파일이나 항목이 없으면 아래 내장 문구로 채운다 —
그래서 영어·일본어·중국어 칸에 한국어가 들어가는 일은 없다.
예전에는 생성한 이름을 네 칸에 똑같이 한국어로 넣었고, 영어 층 이름은 "5F" 뿐이었다.

    tables()  -> ({언어: 표}, 스토리 도감이 없어 내장 문구를 쓴 언어 목록)
    name_clashes(표) -> 다른 몬스터가 같은 이름을 다는 자리 (generate_content 가 쓰기 전에 막는다)
"""

import json
import os

HERE = os.path.dirname(os.path.abspath(__file__))
STORY = os.path.join(HERE, "story")

LANGS = ("kr", "en", "jp", "cn")
COLUMNS = ("ScriptKr", "ScriptEn", "ScriptJp", "ScriptCn")      # LANGS 와 짝

FLOOR_NAME = {"kr": "{name} {floor}층", "en": "{name} {floor}F",
              "jp": "{name} {floor}階", "cn": "{name} 第{floor}层"}
# 접두어와 종 이름 사이. 일본어·중국어는 붙여 쓴다 ("霜の狼", "寒霜狼").
JOIN = {"kr": " ", "en": " ", "jp": "", "cn": ""}

# ---------------------------------------------------------------- 내장 문구
# 줄바꿈은 ScriptData 처럼 글자 그대로의 \n (GetString 이 바꾼다).
# 종 이름은 접두어와 겹치지 않게 짓는다 — "심연 심연의 거수", "잿불 잿빛 파수꾼" 이
# 나오던 자리다. 그래서 8·9 번은 색을 뺀 "파수꾼"·"거수" 다(색은 챕터가 입힌다).
FALLBACK = {
    "kr": dict(
        chapters=["이끼 낀 지하 묘소", "무너진 수로", "잿빛 용광로", "얼어붙은 심층", "왕좌의 균열"],
        prefixes=["이끼", "수렁", "잿불", "서리", "심연"],
        species=["라임 슬라임", "망고 슬라임", "크로우", "정령", "늑대",
                 "고블린 창병", "해골 전사", "고블린 방패병", "파수꾼", "거수"],
        species_desc=[
            "말랑한 몸으로 칼끝을 흘려보내는 슬라임.",
            "달큰한 냄새로 먹잇감을 꾀는 슬라임.",
            "무덤가의 반짝이는 것을 노리는 까마귀.",
            "탑이 떨어질 때 깨어난 작은 정령.",
            "피 냄새를 따라 층을 오르내리는 늑대.",
            "긴 창으로 먼저 찌르는 고블린.",
            "먼저 온 도전자의 뼈가 다시 일어섰다.",
            "커다란 방패 뒤에 숨어 틈을 노리는 고블린.",
            "주인 없는 명령을 지금도 지키는 돌의 파수꾼.",
            "탑의 뿌리에서 기어 나온 거대한 짐승.",
        ],
        chapter_desc=[
            "묘소의 이끼와 낙엽 사이에 숨어 있다.",
            "무너진 수로의 물소리에 발소리를 감춘다.",
            "용광로의 불티를 뒤집어써 몸이 뜨겁다.",
            "심층의 냉기에 얼어 아픔에 무디다.",
            "왕좌의 균열에서 새어 나온 티끌을 먹고 자랐다.",
        ],
        bosses=[
            ("묘지기 늑대", "묘소 입구를 지키는 늑대 우두머리.\\n쓰러지기 직전에 한 번 날뛰며 상처를 틀어막는다."),
            ("수로의 방패대장", "무너진 수로를 틀어막은 고블린 대장.\\n방패를 든 채 싸움을 시작한다."),
            ("잿빛 파수꾼", "용광로의 불씨로 움직이는 파수꾼.\\n모든 공격이 치명타다."),
            ("얼어붙은 해골 기사", "심층의 얼음 속에서 죽지 못한 기사.\\n치명타가 아니면 거의 다치지 않는다."),
            ("왕좌의 거수", "탑의 가장 깊은 곳, 왕좌를 차지한 거수.\\n다섯 번 맞을 때마다 포효한다."),
        ],
        traits=[
            ("없음", "특별한 능력이 없다."),
            ("야수", "체력이 10% 이하가 되면 한 번, 최대 체력의 40%를 회복한다.\\n그만큼 더 때려야 한다 — 예상 피해에는 이미 들어 있다."),
            ("마법", "모든 공격이 치명타다.\\n오래 끌수록 아프다 — 체력이 낮으니 빨리 끝내라."),
            ("수호", "방패를 든 채 싸움을 시작하고, 방어 게이지가 두 배로 빨리 찬다.\\n방패는 치명타가 아닌 공격을 1로 막지만, 한 대 맞으면 깨진다."),
            ("불사", "치명타가 아닌 공격은 20%만 받는다.\\n치명타가 들어가는 차례를 세어 두고 싸워라."),
            ("검사", "공격 속도가 두 배다.\\n한 대는 약하지만 쉴 새 없이 찌른다 — 방어력이 높을수록 덜 아프다."),
            ("거대", "다섯 번 맞을 때마다 포효해, 자기 일반 공격 피해의 20%를 때린 쪽에게 돌려준다.\\n체력이 많다 — 한 대가 셀수록 포효를 덜 듣는다."),
            ("암살", "숨어 있는 동안 치명타가 아닌 공격을 모두 피한다. 치명타를 맞으면 모습을 드러낸다.\\n치명 횟수는 싸움이 끝나도 이어진다 — 싸우는 순서가 값을 바꾼다."),
            ("갑옷", "최대 체력의 30%만큼의 껍질이 모든 피해를 먼저 받아 낸다. 방어력은 0이다.\\n껍질만 깨면 무르다."),
            ("분열", "쓰러지면 세 마리로 갈라진다."),
        ],
        book=("몬스터 도감", "마검의 기억을 옮겨 적은 책.\\n이 층의 마물과 싸움의 값을 한눈에 보여 준다."),
    ),
    "en": dict(
        chapters=["Mossy Catacombs", "Collapsed Aqueduct", "Ashen Furnace", "Frozen Depths", "Rift of the Throne"],
        prefixes=["Moss", "Mire", "Ember", "Frost", "Abyssal"],
        species=["Lime Slime", "Mango Slime", "Crow", "Spirit", "Wolf",
                 "Goblin Spearman", "Skeleton Warrior", "Goblin Shieldbearer", "Sentinel", "Behemoth"],
        species_desc=[
            "A soft slime whose body lets blades slide off.",
            "A slime that lures prey with a sweet smell.",
            "A crow that hunts for anything shiny among the graves.",
            "A small spirit that woke when the tower fell.",
            "A wolf that follows the smell of blood from floor to floor.",
            "A goblin that strikes first with a long spear.",
            "The bones of an earlier challenger, standing up again.",
            "A goblin that hides behind a huge shield, waiting for an opening.",
            "A stone sentinel still keeping orders with no master left.",
            "A huge beast that crawled up from the tower's roots.",
        ],
        chapter_desc=[
            "It lurks among the moss and fallen leaves of the catacombs.",
            "It hides its footsteps in the dripping of the ruined aqueduct.",
            "Covered in furnace embers, it burns to the touch.",
            "The cold of the depths has numbed it to pain.",
            "It grew fat on the dust leaking from the throne's rift.",
        ],
        bosses=[
            ("Gravekeeper Wolf", "The wolf lord that guards the catacombs.\\nJust before it falls, it goes berserk once and closes its wounds."),
            ("Shield Captain of the Aqueduct", "The goblin captain who dammed the ruined aqueduct.\\nIt starts every fight behind its shield."),
            ("Ashen Sentinel", "A sentinel that runs on the furnace's embers.\\nEvery attack is a critical hit."),
            ("Frozen Bone Knight", "A knight who could not die in the ice of the depths.\\nOnly critical hits really hurt it."),
            ("Behemoth of the Throne", "The behemoth on the throne at the tower's deepest point.\\nIt roars back every fifth hit it takes."),
        ],
        traits=[
            ("None", "No special ability."),
            ("Beast", "Once, when its HP drops to 10% or less, it recovers 40% of its max HP.\\nYou must hit it that much more — the forecast already counts it."),
            ("Magic", "Every attack is a critical hit.\\nThe longer the fight, the more it hurts — its HP is low, so end it fast."),
            ("Guardian", "Starts the fight with its shield up, and its guard gauge fills twice as fast.\\nThe shield cuts a non-critical hit to 1, but breaks after one hit."),
            ("Immortal", "Takes only 20% damage from non-critical hits.\\nCount your attacks and time your critical hits."),
            ("Swordsman", "Attacks twice as fast.\\nEach hit is light but relentless — every point of defense counts twice."),
            ("Titan", "Every fifth hit it takes, it roars back for 20% of its normal attack.\\nIt has lots of HP — the harder you hit, the fewer roars you hear."),
            ("Assassin", "While hidden it dodges every non-critical hit; a critical hit reveals it.\\nYour critical count carries over between fights — the order you fight in changes the price."),
            ("Armor", "A shell worth 30% of its max HP absorbs all damage first. Its defense is 0.\\nOnce the shell breaks, it is soft."),
            ("Split", "Splits into three when defeated."),
        ],
        book=("Monster Manual", "A book of the Ego Sword's memories.\\nShows this floor's monsters and what each fight will cost."),
    ),
    "jp": dict(
        chapters=["苔むした地下墓所", "崩れた水路", "灰色の溶鉱炉", "凍てついた深層", "玉座の亀裂"],
        prefixes=["苔むした", "沼の", "熾火の", "霜の", "深淵の"],
        species=["ライムスライム", "マンゴースライム", "クロウ", "精霊", "狼",
                 "ゴブリン槍兵", "骸骨戦士", "ゴブリン盾兵", "番兵", "巨獣"],
        species_desc=[
            "柔らかい体で刃を受け流すスライム。",
            "甘い匂いで獲物をおびき寄せるスライム。",
            "墓場の光り物を狙うカラス。",
            "塔が落ちた時に目覚めた小さな精霊。",
            "血の匂いを追って階を行き来する狼。",
            "長い槍で先に突いてくるゴブリン。",
            "先に来た挑戦者の骨が再び立ち上がった。",
            "大盾の陰に隠れて隙をうかがうゴブリン。",
            "主なき命令を今も守り続ける石の番兵。",
            "塔の根から這い出してきた巨大な獣。",
        ],
        chapter_desc=[
            "墓所の苔と落ち葉の間に潜んでいる。",
            "崩れた水路の水音に足音を紛れさせる。",
            "溶鉱炉の火の粉を浴びて体が熱い。",
            "深層の冷気に凍え、痛みに鈍い。",
            "玉座の亀裂から漏れる塵を喰らって育った。",
        ],
        bosses=[
            ("墓守りの狼", "墓所の入口を守る狼の首領。\\n倒れる寸前に一度だけ暴れ、傷をふさぐ。"),
            ("水路の盾隊長", "崩れた水路をせき止めたゴブリンの隊長。\\n盾を構えたまま戦いを始める。"),
            ("灰の番兵", "溶鉱炉の残り火で動く番兵。\\nすべての攻撃が会心の一撃。"),
            ("凍てついた骸骨騎士", "深層の氷の中で死ねなかった騎士。\\n会心でなければほとんど傷つかない。"),
            ("玉座の巨獣", "塔の最も深い場所で玉座を占める巨獣。\\n5回攻撃を受けるたびに咆哮する。"),
        ],
        traits=[
            ("なし", "特別な能力はない。"),
            ("野獣", "HPが10%以下になると一度だけ、最大HPの40%を回復する。\\nその分だけ多く殴る必要がある。予想ダメージには含まれている。"),
            ("魔法", "すべての攻撃が会心の一撃になる。\\n長引くほど痛い。HPは低いので素早く倒そう。"),
            ("守護", "盾を構えた状態で戦闘を始め、防御ゲージが2倍の速さでたまる。\\n盾は会心以外の攻撃を1に抑えるが、一撃で壊れる。"),
            ("不死", "会心以外の攻撃は20%しか受けない。\\n会心が出る順番を数えて戦おう。"),
            ("剣士", "攻撃速度が2倍。\\n一撃は軽いが絶え間なく突いてくる。防御力が高いほど痛くない。"),
            ("巨体", "5回攻撃を受けるたびに咆哮し、通常攻撃の20%のダメージを殴った側に返す。\\nHPが多い。一撃が重いほど咆哮は少なくなる。"),
            ("暗殺", "潜んでいる間は会心以外の攻撃をすべて避ける。会心を受けると姿を現す。\\n会心のカウントは戦闘後も続く。戦う順番で代償が変わる。"),
            ("鎧", "最大HPの30%分の殻が、すべてのダメージを先に受け止める。防御力は0。\\n殻さえ割れば脆い。"),
            ("分裂", "倒すと3体に分裂する。"),
        ],
        book=("モンスター図鑑", "魔剣の記憶を書き写した本。\\nこの階の魔物と戦いの代償がひと目でわかる。"),
    ),
    "cn": dict(
        chapters=["苔藓地下墓穴", "坍塌水道", "灰烬熔炉", "冰封深层", "王座裂隙"],
        prefixes=["苔藓", "泥沼", "余烬", "寒霜", "深渊"],
        species=["青柠史莱姆", "芒果史莱姆", "乌鸦", "精灵", "狼",
                 "哥布林枪兵", "骷髅战士", "哥布林盾兵", "哨兵", "巨兽"],
        species_desc=[
            "用柔软的身体卸开刀锋的史莱姆。",
            "用甜腻气味引诱猎物的史莱姆。",
            "专盯坟地里闪光之物的乌鸦。",
            "塔坠落时苏醒的小精灵。",
            "循着血腥味在各层间游荡的狼。",
            "用长枪抢先出手的哥布林。",
            "先来挑战者的骸骨再次站了起来。",
            "躲在巨盾后伺机而动的哥布林。",
            "至今仍在执行无主命令的石之哨兵。",
            "从塔根深处爬出的巨兽。",
        ],
        chapter_desc=[
            "潜伏在墓穴的苔藓与落叶之间。",
            "借着坍塌水道的滴水声掩去脚步。",
            "浑身沾满熔炉火星，灼热难当。",
            "被深层寒气冻得感觉迟钝。",
            "以王座裂隙中渗出的尘埃为食长大。",
        ],
        bosses=[
            ("守墓狼", "守卫墓穴入口的狼首领。\\n倒下之前会狂暴一次，堵住伤口。"),
            ("水道盾卫队长", "堵住坍塌水道的哥布林队长。\\n总是举着盾牌开战。"),
            ("灰烬哨兵", "靠熔炉余烬驱动的哨兵。\\n所有攻击都是暴击。"),
            ("冰封骷髅骑士", "在深层寒冰中无法死去的骑士。\\n不是暴击就几乎伤不了它。"),
            ("王座巨兽", "盘踞在塔最深处王座上的巨兽。\\n每挨五次攻击就咆哮一次。"),
        ],
        traits=[
            ("无", "没有特殊能力。"),
            ("野兽", "生命值降到10%以下时，会回复一次最大生命值的40%。\\n需要多打这么多——预计伤害已计算在内。"),
            ("魔法", "所有攻击都是暴击。\\n拖得越久越疼——它生命值低，速战速决。"),
            ("守护", "举盾开战，防御槽充能速度是两倍。\\n盾牌会把非暴击攻击压到1点，但挨一下就会破。"),
            ("不死", "非暴击攻击只受到20%的伤害。\\n数好暴击的次序再开打。"),
            ("剑士", "攻击速度是两倍。\\n每一击很轻但连绵不断——防御力越高越不疼。"),
            ("巨躯", "每挨五次攻击就咆哮一次，向攻击者反弹其普通攻击20%的伤害。\\n生命值很高——单次伤害越高，咆哮越少。"),
            ("暗杀", "隐身时会闪避所有非暴击攻击；被暴击命中就会现形。\\n暴击计数在战斗之间延续——战斗顺序会改变代价。"),
            ("铠甲", "相当于最大生命值30%的外壳会先吸收所有伤害，防御力为0。\\n外壳一破就很脆弱。"),
            ("分裂", "被击败后会分裂成三只。"),
        ],
        book=("怪物图鉴", "抄录魔剑记忆的书。\\n一眼看清本层的魔物与每场战斗的代价。"),
    ),
}


def _text(v):
    """스토리 파일의 문구. 진짜 줄바꿈은 ScriptData 식 \\n 으로 바꾼다."""
    if not isinstance(v, str) or not v.strip():
        return None
    return v.strip().replace("\r\n", "\n").replace("\n", "\\n")


def _story(lang):
    path = os.path.join(STORY, f"story_{lang}.json")
    if not os.path.exists(path):
        return {}
    with open(path, "r", encoding="utf-8") as f:
        return json.load(f).get("bestiary") or {}


def _merge(lang, story):
    """내장 표 위에 스토리 도감을 항목 단위로 덮는다. 빈 칸은 내장 문구가 남는다."""
    t = {k: (list(v) if isinstance(v, list) else v) for k, v in FALLBACK[lang].items()}
    t["mob_desc"] = {}
    for c, row in enumerate(story.get("chapters") or []):
        if c < len(t["chapters"]):
            t["chapters"][c] = _text(row.get("name")) or t["chapters"][c]
            t["prefixes"][c] = _text(row.get("mob_prefix")) or t["prefixes"][c]
    for row in story.get("species") or []:
        art = row.get("art")
        if isinstance(art, int) and 0 <= art < len(t["species"]):
            t["species"][art] = _text(row.get("name")) or t["species"][art]
    for key, v in (story.get("mob_desc") or {}).items():
        if _text(v):
            ch, art = (int(x) for x in key.split(":"))
            t["mob_desc"][ch, art] = _text(v)
    for row in story.get("bosses") or []:
        ch = row.get("chapter")
        if isinstance(ch, int) and 0 <= ch < len(t["bosses"]):
            name, desc = t["bosses"][ch]
            desc = _text(row.get("desc")) or desc
            title = _text(row.get("title"))
            t["bosses"][ch] = (_text(row.get("name")) or name,
                               f"{title}\\n{desc}" if title else desc)
    for row in story.get("traits") or []:
        tid = row.get("id")
        if isinstance(tid, int) and 0 <= tid < len(t["traits"]):
            name, desc = t["traits"][tid]
            t["traits"][tid] = (_text(row.get("name")) or name, _text(row.get("desc")) or desc)
    book = (story.get("items") or {}).get("monster_book") or {}
    t["book"] = (_text(book.get("name")) or t["book"][0], _text(book.get("desc")) or t["book"][1])
    return t


def tables():
    """({언어: 표}, 스토리 도감이 없어 내장 문구만 쓴 언어 목록)."""
    out, missing = {}, []
    for lang in LANGS:
        story = _story(lang)
        if not story:
            missing.append(lang)
        out[lang] = _merge(lang, story)
    return out, missing


def floor_name(t, lang, ch, floor):
    return FLOOR_NAME[lang].format(name=t["chapters"][ch], floor=floor)


def mob_name(t, lang, ch, art):
    prefix, species = t["prefixes"][ch], t["species"][art]
    # 접두어가 종 이름에 이미 들어 있으면 한 번만 쓴다 ("심연 심연의 거수" 방지).
    return species if prefix in species else f"{prefix}{JOIN[lang]}{species}"


def mob_desc(t, lang, ch, art):
    if (ch, art) in t["mob_desc"]:
        return t["mob_desc"][ch, art]
    return f"{t['species_desc'][art]}\\n{t['chapter_desc'][ch]}"


def name_clashes(books):
    """다른 몬스터가 같은 이름을 다는 자리. 없으면 빈 목록.

    FORMAT.md 의 종 순서가 "0 슬라임, 1 슬라임(다른 색)" 이라 둘 다 "슬라임" 으로 지으면
    "이끼 슬라임" 이 둘이 된다 — 8·18…98층은 3·4번 자리가 그 두 그림이라, 한 층에 같은
    이름이 스탯도 특성도 다르게 선다. 같은 챕터·같은 그림은 같은 종이라 겹쳐도 된다.
    """
    out = []
    for lang in LANGS:
        t, seen = books[lang], {}
        named = [(mob_name(t, lang, c, a), f"chapters[{c}] + species art {a}")
                 for c in range(len(t["chapters"])) for a in range(len(t["species"]))]
        named += [(name, f"bosses chapter {c}") for c, (name, _) in enumerate(t["bosses"])]
        for name, who in named:
            if name in seen:
                out.append(f"story_{lang}.json 도감: '{name}' 이 {seen[name]} 와 {who} 에 겹친다")
            else:
                seen[name] = who
    return out


def has_hangul(text):
    return any("가" <= c <= "힣" or "ㄱ" <= c <= "ㆎ" for c in text or "")


if __name__ == "__main__":
    books, missing = tables()
    for lang in LANGS:
        t = books[lang]
        assert len(t["chapters"]) == len(t["prefixes"]) == len(t["chapter_desc"]) == len(t["bosses"]) == 5
        assert len(t["species"]) == len(t["species_desc"]) == 10 and len(t["traits"]) == 10
        names = [mob_name(t, lang, c, a) for c in range(5) for a in range(10)]
        if lang != "kr":
            texts = names + [x for pair in t["bosses"] + t["traits"] + [t["book"]] for x in pair]
            texts += t["chapters"] + [mob_desc(t, lang, c, a) for c in range(5) for a in range(10)]
            assert not any(has_hangul(x) for x in texts), lang
    assert not name_clashes(books), name_clashes(books)[:3]      # 이름이 겹치지 않는다
    # FORMAT.md 대로 0·1 을 둘 다 "슬라임" 이라 지으면 걸려야 한다.
    twins = {"species": [{"art": 0, "name": "슬라임"}, {"art": 1, "name": "슬라임"}]}
    assert "species art 1" in name_clashes({lang: _merge(lang, twins) for lang in LANGS})[0]
    assert mob_name(_merge("kr", {}), "kr", 4, 9) == "심연 거수"
    assert mob_name(dict(prefixes=["심연"], species=["심연의 거수"]), "kr", 0, 0) == "심연의 거수"
    print("도감 점검 통과 — 스토리 도감 없음:", ", ".join(missing) or "없음")
