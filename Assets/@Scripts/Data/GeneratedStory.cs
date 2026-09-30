// Tools/story_gen.py 가 Tools/story/ 대본에서 굽는다. 손으로 고치지 않는다.
// 대본: story_kr.json. 문구는 ScriptData 에 있다 (화자 300000~, 챕터 300100~, 선택지 300200~, 대사 301000~).
public static class GeneratedStory
{
    public static readonly StorySpeaker[] Speakers =
    {
        new StorySpeaker("damian", 6, StoryPortrait.Damian, 0),
        new StorySpeaker("sword", 4018, StoryPortrait.Sword, 0),
        new StorySpeaker("narration", 0, StoryPortrait.None, 0),
        new StorySpeaker("chief", 300000, StoryPortrait.None, 0),
        new StorySpeaker("boss0", 300001, StoryPortrait.Boss, 0),
        new StorySpeaker("boss1", 300002, StoryPortrait.Boss, 1),
        new StorySpeaker("boss2", 300003, StoryPortrait.Boss, 2),
        new StorySpeaker("boss3", 300004, StoryPortrait.Boss, 3),
        new StorySpeaker("boss4", 300005, StoryPortrait.Boss, 4),
    };

    public static readonly StoryScene[] Scenes =
    {
        new StoryScene("pro_contract_after", StoryKind.Dialogue, StoryTrigger.Prologue, 0,
            new[]
            {
                new StoryLine(1, "Illust_MagicalSword_Treasure", 301000, null),
                new StoryLine(0, null, 301001, null),
                new StoryLine(1, "Illust_MagicalSword_Smile", 301002, null),
                new StoryLine(1, null, 301003, null),
                new StoryLine(0, "Illust_Adventurer_Question", 301004, null),
                new StoryLine(1, "Illust_MagicalSword_Smile", 301005, null),
            },
            null,
            new[] { new StoryCue(0, StoryCueKind.Flash, null), new StoryCue(2, StoryCueKind.Emote, "SweatDrop") }),
        new StoryScene("mech_forecast", StoryKind.Dialogue, StoryTrigger.Mechanic, 0,
            new[]
            {
                new StoryLine(1, "Illust_MagicalSword_Smile", 301040, null),
                new StoryLine(1, null, 301041, null),
                new StoryLine(0, "Illust_Adventurer_Thinking", 301042, null),
                new StoryLine(0, null, 301043, null),
                new StoryLine(1, "Illust_MagicalSword_Angry", 301044, null),
                new StoryLine(1, "Illust_MagicalSword_Nerve", 301045, null),
            },
            null,
            new[] { new StoryCue(5, StoryCueKind.Emote, "Question") }),
        new StoryScene("mech_crit", StoryKind.Dialogue, StoryTrigger.Mechanic, 10,
            new[]
            {
                new StoryLine(1, "Illust_MagicalSword_Smile", 301080, null),
                new StoryLine(0, "Illust_Adventurer_Question", 301081, null),
                new StoryLine(1, null, 301082, null),
                new StoryLine(1, "Illust_MagicalSword_Smile", 301083, null),
            },
            null,
            null),
        new StoryScene("pro_kingslime_reveal", StoryKind.Dialogue, StoryTrigger.Prologue, 1,
            new[]
            {
                new StoryLine(1, "Illust_MagicalSword_Angry", 301120, null),
                new StoryLine(1, "Illust_MagicalSword_Nerve", 301121, null),
                new StoryLine(1, "Illust_MagicalSword_Angry", 301122, null),
                new StoryLine(0, null, 301123, null),
                new StoryLine(1, "Illust_MagicalSword_Smile", 301124, null),
                new StoryLine(1, null, 301125, null),
                new StoryLine(1, null, 301126, null),
                new StoryLine(0, "Illust_Adventurer_Thinking", 301127, null),
            },
            null,
            new[] { new StoryCue(1, StoryCueKind.Shake, null), new StoryCue(999, StoryCueKind.Pose, "DrawSword") }),
        new StoryScene("pro_kingslime_clear", StoryKind.Dialogue, StoryTrigger.Prologue, 2,
            new[]
            {
                new StoryLine(1, "Illust_MagicalSword_Smile", 301160, null),
                new StoryLine(0, "Illust_Adventurer_Thinking", 301161, null),
                new StoryLine(1, "Illust_MagicalSword_Treasure", 301162, null),
                new StoryLine(1, "Illust_MagicalSword_Smile", 301163, null),
                new StoryLine(0, null, 301164, null),
                new StoryLine(1, "Illust_MagicalSword_Silense", 301165, null),
                new StoryLine(1, "Illust_MagicalSword_Question", 301166, null),
                new StoryLine(0, "Illust_Adventurer_Question", 301167, null),
                new StoryLine(1, null, 301168, null),
                new StoryLine(1, "Illust_MagicalSword_Sleep", 301169, null),
            },
            null,
            new[] { new StoryCue(0, StoryCueKind.BgmStop, null), new StoryCue(3, StoryCueKind.Fx, "FX_PowerWave"), new StoryCue(6, StoryCueKind.Dark, null), new StoryCue(7, StoryCueKind.Clear, null), new StoryCue(10, StoryCueKind.Emote, "Sleepy"), new StoryCue(999, StoryCueKind.BgmFloor, null) }),
        new StoryScene("village_card", StoryKind.Card, StoryTrigger.Village, 0,
            new[]
            {
                new StoryLine(2, null, 301200, "04_탑_용병수정"),
                new StoryLine(2, null, 301201, "Intro04"),
                new StoryLine(2, null, 301202, "05_마을"),
            },
            null,
            new[] { new StoryCue(0, StoryCueKind.Bgm, "StartIntro_BGM"), new StoryCue(999, StoryCueKind.BgmFloor, null) }),
        new StoryScene("village_graves", StoryKind.Card, StoryTrigger.FloorFirst, 6,
            new[]
            {
                new StoryLine(2, null, 301240, "05_마을"),
            },
            null,
            new[] { new StoryCue(0, StoryCueKind.Bgm, "StartIntro_BGM") }),
        new StoryScene("village_chief", StoryKind.Dialogue, StoryTrigger.FloorFirst, 6,
            new[]
            {
                new StoryLine(3, null, 301280, null),
                new StoryLine(0, "Illust_Adventurer_Question", 301281, null),
                new StoryLine(3, null, 301282, null),
                new StoryLine(3, null, 301283, null),
                new StoryLine(1, "Illust_MagicalSword_Treasure", 301284, null),
                new StoryLine(0, null, 301285, null),
                new StoryLine(3, null, 301286, null),
                new StoryLine(3, null, 301287, null),
                new StoryLine(3, null, 301288, null),
                new StoryLine(3, null, 301289, null),
                new StoryLine(0, null, 301290, null),
            },
            null,
            new[] { new StoryCue(0, StoryCueKind.Backdrop, null), new StoryCue(999, StoryCueKind.BgmFloor, null) }),
        new StoryScene("ch0_start", StoryKind.Dialogue, StoryTrigger.ChapterStart, 0,
            new[]
            {
                new StoryLine(0, "Illust_Adventurer_Question", 301320, null),
                new StoryLine(1, "Illust_MagicalSword_Question", 301321, null),
            },
            null,
            new[] { new StoryCue(1, StoryCueKind.Emote, "Question"), new StoryCue(1, StoryCueKind.CamUp, null) }),
        new StoryScene("trait_guardian", StoryKind.Dialogue, StoryTrigger.TraitFirst, 3,
            new[]
            {
                new StoryLine(1, null, 301360, null),
                new StoryLine(0, "Illust_Adventurer_Thinking", 301361, null),
            },
            null,
            null),
        new StoryScene("mech_rune", StoryKind.Dialogue, StoryTrigger.Mechanic, 6,
            new[]
            {
                new StoryLine(1, "Illust_MagicalSword_Treasure", 301400, null),
                new StoryLine(0, "Illust_Adventurer_Thinking", 301401, null),
                new StoryLine(1, "Illust_MagicalSword_Smile", 301402, null),
            },
            null,
            null),
        new StoryScene("mech_levelup", StoryKind.Dialogue, StoryTrigger.Mechanic, 11,
            new[]
            {
                new StoryLine(1, "Illust_MagicalSword_Smile", 301440, null),
                new StoryLine(1, null, 301441, null),
                new StoryLine(0, "Illust_Adventurer_Question", 301442, null),
                new StoryLine(1, "Illust_MagicalSword_Silense", 301443, null),
            },
            null,
            null),
        new StoryScene("mech_spare_key", StoryKind.Bark, StoryTrigger.Mechanic, 4,
            new[]
            {
                new StoryLine(1, null, 301480, null),
            },
            null,
            null),
        new StoryScene("mech_nokey", StoryKind.Bark, StoryTrigger.Mechanic, 7,
            new[]
            {
                new StoryLine(1, null, 301520, null),
            },
            null,
            null),
        new StoryScene("mech_fatal", StoryKind.Dialogue, StoryTrigger.Mechanic, 1,
            new[]
            {
                new StoryLine(1, null, 301560, null),
                new StoryLine(1, null, 301561, null),
                new StoryLine(0, null, 301562, null),
            },
            null,
            null),
        new StoryScene("mech_overflow", StoryKind.Dialogue, StoryTrigger.Mechanic, 2,
            new[]
            {
                new StoryLine(1, null, 301600, null),
                new StoryLine(0, "Illust_Adventurer_Thinking", 301601, null),
                new StoryLine(1, null, 301602, null),
            },
            null,
            null),
        new StoryScene("mech_vault", StoryKind.Dialogue, StoryTrigger.Mechanic, 3,
            new[]
            {
                new StoryLine(1, null, 301640, null),
                new StoryLine(1, null, 301641, null),
                new StoryLine(1, null, 301642, null),
                new StoryLine(1, null, 301643, null),
                new StoryLine(0, "Illust_Adventurer_Thinking", 301644, null),
            },
            null,
            null),
        new StoryScene("mech_choice", StoryKind.Dialogue, StoryTrigger.Mechanic, 5,
            new[]
            {
                new StoryLine(1, null, 301680, null),
                new StoryLine(0, "Illust_Adventurer_Question", 301681, null),
                new StoryLine(1, null, 301682, null),
                new StoryLine(0, "Illust_Adventurer_Thinking", 301683, null),
            },
            null,
            null),
        new StoryScene("mech_death", StoryKind.Dialogue, StoryTrigger.Mechanic, 9,
            new[]
            {
                new StoryLine(0, "Illust_Adventurer_Silence", 301720, null),
                new StoryLine(1, "Illust_MagicalSword_Angry", 301721, null),
                new StoryLine(1, null, 301722, null),
                new StoryLine(2, null, 301723, null),
            },
            null,
            null),
        new StoryScene("f11_motto", StoryKind.Dialogue, StoryTrigger.FloorFirst, 11,
            new[]
            {
                new StoryLine(1, "Illust_MagicalSword_Smile", 301760, null),
                new StoryLine(1, "Illust_MagicalSword_Smile", 301761, null),
                new StoryLine(0, "Illust_Adventurer_Question", 301762, null),
                new StoryLine(1, "Illust_MagicalSword_Silense", 301763, null),
                new StoryLine(0, null, 301764, null),
                new StoryLine(1, "Illust_MagicalSword_Panic", 301765, null),
            },
            null,
            new[] { new StoryCue(6, StoryCueKind.Emote, "SweatDrop") }),
        new StoryScene("f16_nap", StoryKind.Dialogue, StoryTrigger.FloorFirst, 16,
            new[]
            {
                new StoryLine(1, "Illust_MagicalSword_Sleep", 301800, null),
                new StoryLine(0, "Illust_Adventurer_Silence", 301801, null),
                new StoryLine(0, "Illust_Adventurer_Question", 301802, null),
                new StoryLine(1, "Illust_MagicalSword_Panic", 301803, null),
                new StoryLine(0, null, 301804, null),
                new StoryLine(1, "Illust_MagicalSword_Angry", 301805, null),
            },
            null,
            new[] { new StoryCue(1, StoryCueKind.Emote, "Sleepy"), new StoryCue(4, StoryCueKind.Emote, "Surprise") }),
        new StoryScene("trait_beast", StoryKind.Dialogue, StoryTrigger.TraitFirst, 1,
            new[]
            {
                new StoryLine(1, "Illust_MagicalSword_Nerve", 301840, null),
                new StoryLine(0, "Illust_Adventurer_Question", 301841, null),
                new StoryLine(1, "Illust_MagicalSword_Smile", 301842, null),
            },
            null,
            null),
        new StoryScene("boss0_intro", StoryKind.Dialogue, StoryTrigger.BossIntro, 0,
            new[]
            {
                new StoryLine(4, null, 301880, null),
                new StoryLine(0, "Illust_Adventurer_Question", 301881, null),
                new StoryLine(1, "Illust_MagicalSword_Nerve", 301882, null),
                new StoryLine(4, null, 301883, null),
                new StoryLine(0, null, 301884, null),
                new StoryLine(1, "Illust_MagicalSword_Smile", 301885, null),
            },
            null,
            null),
        new StoryScene("boss0_defeat", StoryKind.Dialogue, StoryTrigger.BossDefeat, 0,
            new[]
            {
                new StoryLine(4, null, 301920, null),
                new StoryLine(2, null, 301921, null),
                new StoryLine(1, "Illust_MagicalSword_Silense", 301922, null),
                new StoryLine(0, "Illust_Adventurer_Question", 301923, null),
                new StoryLine(1, null, 301924, null),
                new StoryLine(0, "Illust_Adventurer_Thinking", 301925, null),
                new StoryLine(1, "Illust_MagicalSword_Question", 301926, null),
                new StoryLine(1, null, 301927, null),
                new StoryLine(0, null, 301928, null),
                new StoryLine(1, "Illust_MagicalSword_Silense", 301929, null),
            },
            null,
            new[] { new StoryCue(0, StoryCueKind.BgmStop, null), new StoryCue(0, StoryCueKind.Boom, null), new StoryCue(2, StoryCueKind.CamBoss, null), new StoryCue(3, StoryCueKind.Fx, "FX_PowerWave"), new StoryCue(999, StoryCueKind.BgmFloor, null) }),
        new StoryScene("mech_warp", StoryKind.Dialogue, StoryTrigger.Mechanic, 8,
            new[]
            {
                new StoryLine(0, "Illust_Adventurer_Thinking", 301960, null),
                new StoryLine(0, "Illust_Adventurer_Silence", 301961, null),
                new StoryLine(0, "Illust_Adventurer_Thinking", 301962, null),
                new StoryLine(1, "Illust_MagicalSword_Silense", 301963, null),
                new StoryLine(1, null, 301964, null),
            },
            null,
            null),
        new StoryScene("ch1_start", StoryKind.Dialogue, StoryTrigger.ChapterStart, 1,
            new[]
            {
                new StoryLine(0, "Illust_Adventurer_Thinking", 302000, null),
                new StoryLine(1, "Illust_MagicalSword_Angry", 302001, null),
                new StoryLine(1, null, 302002, null),
                new StoryLine(0, "Illust_Adventurer_Question", 302003, null),
                new StoryLine(1, "Illust_MagicalSword_Silense", 302004, null),
                new StoryLine(1, "Illust_MagicalSword_Silense", 302005, null),
            },
            null,
            new[] { new StoryCue(0, StoryCueKind.CamUp, null), new StoryCue(2, StoryCueKind.Emote, "SweatDrop"), new StoryCue(4, StoryCueKind.Emote, "Question") }),
        new StoryScene("trait_knight", StoryKind.Dialogue, StoryTrigger.TraitFirst, 5,
            new[]
            {
                new StoryLine(1, null, 302040, null),
                new StoryLine(0, "Illust_Adventurer_Thinking", 302041, null),
            },
            null,
            null),
        new StoryScene("f27_sleeptalk", StoryKind.Dialogue, StoryTrigger.FloorFirst, 27,
            new[]
            {
                new StoryLine(1, "Illust_MagicalSword_Sleep", 302080, null),
                new StoryLine(1, "Illust_MagicalSword_Sleep", 302081, null),
                new StoryLine(0, "Illust_Adventurer_Question", 302082, null),
                new StoryLine(1, "Illust_MagicalSword_Panic", 302083, null),
                new StoryLine(0, null, 302084, null),
                new StoryLine(1, "Illust_MagicalSword_Angry", 302085, null),
            },
            null,
            new[] { new StoryCue(1, StoryCueKind.Emote, "Sleepy"), new StoryCue(3, StoryCueKind.Emote, "Question"), new StoryCue(4, StoryCueKind.Emote, "Surprise"), new StoryCue(6, StoryCueKind.Emote, "Angry") }),
        new StoryScene("f31_trust", StoryKind.Dialogue, StoryTrigger.FloorFirst, 31,
            new[]
            {
                new StoryLine(0, null, 302120, null),
                new StoryLine(1, "Illust_MagicalSword_Question", 302121, null),
                new StoryLine(0, null, 302122, null),
                new StoryLine(1, "Illust_MagicalSword_Surprise", 302123, null),
                new StoryLine(1, "Illust_MagicalSword_Smile", 302124, null),
                new StoryLine(1, "Illust_MagicalSword_Nerve", 302125, null),
            },
            null,
            new[] { new StoryCue(0, StoryCueKind.CamMonster, null), new StoryCue(1, StoryCueKind.CamPlayer, null), new StoryCue(5, StoryCueKind.Emote, "Grinning") }),
        new StoryScene("f35_suspect", StoryKind.Dialogue, StoryTrigger.FloorFirst, 35,
            new[]
            {
                new StoryLine(1, null, 302160, null),
                new StoryLine(1, "Illust_MagicalSword_Smile", 302161, null),
                new StoryLine(0, "Illust_Adventurer_Thinking", 302162, null),
                new StoryLine(1, "Illust_MagicalSword_Panic", 302163, null),
                new StoryLine(0, "Illust_Adventurer_Thinking", 302164, null),
            },
            null,
            new[] { new StoryCue(3, StoryCueKind.Emote, "Question"), new StoryCue(4, StoryCueKind.Emote, "SweatDrop") }),
        new StoryScene("boss1_intro", StoryKind.Dialogue, StoryTrigger.BossIntro, 1,
            new[]
            {
                new StoryLine(5, null, 302200, null),
                new StoryLine(5, null, 302201, null),
                new StoryLine(0, "Illust_Adventurer_Thinking", 302202, null),
                new StoryLine(1, "Illust_MagicalSword_Silense", 302203, null),
                new StoryLine(0, "Illust_Adventurer_Thinking", 302204, null),
                new StoryLine(5, null, 302205, null),
                new StoryLine(5, null, 302206, null),
                new StoryLine(1, "Illust_MagicalSword_Angry", 302207, null),
            },
            null,
            new[] { new StoryCue(8, StoryCueKind.Emote, "Angry"), new StoryCue(999, StoryCueKind.Pose, "DrawSword") }),
        new StoryScene("boss1_defeat", StoryKind.Dialogue, StoryTrigger.BossDefeat, 1,
            new[]
            {
                new StoryLine(5, null, 302240, null),
                new StoryLine(5, null, 302241, null),
                new StoryLine(1, "Illust_MagicalSword_Silense", 302242, null),
                new StoryLine(1, "Illust_MagicalSword_Silense", 302243, null),
                new StoryLine(0, "Illust_Adventurer_Question", 302244, null),
                new StoryLine(1, "Illust_MagicalSword_Silense", 302245, null),
                new StoryLine(0, null, 302246, null),
                new StoryLine(1, "Illust_MagicalSword_Question", 302247, null),
                new StoryLine(0, null, 302248, null),
                new StoryLine(1, "Illust_MagicalSword_Nerve", 302249, null),
            },
            null,
            new[] { new StoryCue(3, StoryCueKind.Boom, null), new StoryCue(3, StoryCueKind.BgmFloor, null) }),
        new StoryScene("ch2_start", StoryKind.Dialogue, StoryTrigger.ChapterStart, 2,
            new[]
            {
                new StoryLine(0, "Illust_Adventurer_Thinking", 302280, null),
                new StoryLine(0, "Illust_Adventurer_Question", 302281, null),
                new StoryLine(1, "Illust_MagicalSword_Silense", 302282, null),
                new StoryLine(1, "Illust_MagicalSword_Silense", 302283, null),
            },
            null,
            new[] { new StoryCue(0, StoryCueKind.CamUp, null) }),
        new StoryScene("trait_magic", StoryKind.Dialogue, StoryTrigger.TraitFirst, 2,
            new[]
            {
                new StoryLine(1, null, 302320, null),
                new StoryLine(0, "Illust_Adventurer_Thinking", 302321, null),
            },
            null,
            null),
        new StoryScene("trait_titan", StoryKind.Dialogue, StoryTrigger.TraitFirst, 6,
            new[]
            {
                new StoryLine(1, null, 302360, null),
                new StoryLine(0, "Illust_Adventurer_Thinking", 302361, null),
            },
            null,
            null),
        new StoryScene("f45_forge", StoryKind.Dialogue, StoryTrigger.FloorFirst, 45,
            new[]
            {
                new StoryLine(1, "Illust_MagicalSword_Silense", 302400, null),
                new StoryLine(0, "Illust_Adventurer_Question", 302401, null),
                new StoryLine(1, "Illust_MagicalSword_Question", 302402, null),
                new StoryLine(0, "Illust_Adventurer_Thinking", 302403, null),
                new StoryLine(1, "Illust_MagicalSword_Nerve", 302404, null),
            },
            null,
            new[] { new StoryCue(2, StoryCueKind.Emote, "Question"), new StoryCue(5, StoryCueKind.Emote, "SweatDrop") }),
        new StoryScene("f50_leftovers", StoryKind.Dialogue, StoryTrigger.FloorFirst, 50,
            new[]
            {
                new StoryLine(1, "Illust_MagicalSword_Sleep", 302440, null),
                new StoryLine(0, null, 302441, null),
                new StoryLine(1, "Illust_MagicalSword_Smile", 302442, null),
                new StoryLine(0, null, 302443, null),
                new StoryLine(0, "Illust_Adventurer_Silence", 302444, null),
                new StoryLine(1, "Illust_MagicalSword_Silense", 302445, null),
                new StoryLine(0, null, 302446, null),
                new StoryLine(1, null, 302447, null),
                new StoryLine(0, null, 302448, null),
                new StoryLine(1, "Illust_MagicalSword_Smile", 302449, null),
            },
            null,
            new[] { new StoryCue(1, StoryCueKind.Emote, "Sleepy"), new StoryCue(3, StoryCueKind.Emote, "Grinning"), new StoryCue(10, StoryCueKind.Emote, "Grinning") }),
        new StoryScene("f55_look", StoryKind.Dialogue, StoryTrigger.FloorFirst, 55,
            new[]
            {
                new StoryLine(1, "Illust_MagicalSword_Bad", 302480, null),
                new StoryLine(0, "Illust_Adventurer_Question", 302481, null),
                new StoryLine(1, "Illust_MagicalSword_Nerve", 302482, null),
                new StoryLine(0, "Illust_Adventurer_Thinking", 302483, null),
                new StoryLine(1, "Illust_MagicalSword_Nerve", 302484, null),
            },
            null,
            new[] { new StoryCue(1, StoryCueKind.CamClose, null), new StoryCue(2, StoryCueKind.Emote, "SweatDrop"), new StoryCue(999, StoryCueKind.CamPlayer, null) }),
        new StoryScene("boss2_intro", StoryKind.Dialogue, StoryTrigger.BossIntro, 2,
            new[]
            {
                new StoryLine(6, null, 302520, null),
                new StoryLine(6, null, 302521, null),
                new StoryLine(0, "Illust_Adventurer_Question", 302522, null),
                new StoryLine(1, null, 302523, null),
                new StoryLine(0, null, 302524, null),
                new StoryLine(1, null, 302525, null),
            },
            null,
            new[] { new StoryCue(3, StoryCueKind.Emote, "Question") }),
        new StoryScene("boss2_defeat", StoryKind.Dialogue, StoryTrigger.BossDefeat, 2,
            new[]
            {
                new StoryLine(6, null, 302560, null),
                new StoryLine(6, null, 302561, null),
                new StoryLine(1, "Illust_MagicalSword_Surprise", 302562, null),
                new StoryLine(3, null, 302563, null),
                new StoryLine(3, null, 302564, null),
                new StoryLine(0, "Illust_Adventurer_Question", 302565, null),
                new StoryLine(1, "Illust_MagicalSword_Silense", 302566, null),
                new StoryLine(1, null, 302567, null),
                new StoryLine(0, "Illust_Adventurer_Thinking", 302568, null),
                new StoryLine(1, null, 302569, null),
                new StoryLine(0, "Illust_Adventurer_Silence", 302570, null),
                new StoryLine(1, "Illust_MagicalSword_Nerve", 302571, null),
            },
            null,
            new[] { new StoryCue(0, StoryCueKind.BgmStop, null), new StoryCue(3, StoryCueKind.Boom, null), new StoryCue(3, StoryCueKind.White, null), new StoryCue(6, StoryCueKind.Clear, null), new StoryCue(999, StoryCueKind.BgmFloor, null) }),
        new StoryScene("ch3_start", StoryKind.Dialogue, StoryTrigger.ChapterStart, 3,
            new[]
            {
                new StoryLine(0, "Illust_Adventurer_Thinking", 302600, null),
                new StoryLine(1, "Illust_MagicalSword_Silense", 302601, null),
                new StoryLine(0, "Illust_Adventurer_Question", 302602, null),
                new StoryLine(1, "Illust_MagicalSword_Silense", 302603, null),
            },
            null,
            null),
        new StoryScene("trait_immortal", StoryKind.Dialogue, StoryTrigger.TraitFirst, 4,
            new[]
            {
                new StoryLine(1, "Illust_MagicalSword_Silense", 302640, null),
                new StoryLine(0, "Illust_Adventurer_Thinking", 302641, null),
            },
            null,
            null),
        new StoryScene("trait_assassin", StoryKind.Dialogue, StoryTrigger.TraitFirst, 7,
            new[]
            {
                new StoryLine(1, null, 302680, null),
                new StoryLine(0, "Illust_Adventurer_Thinking", 302681, null),
            },
            null,
            null),
        new StoryScene("f66_awake", StoryKind.Dialogue, StoryTrigger.FloorFirst, 66,
            new[]
            {
                new StoryLine(0, null, 302720, null),
                new StoryLine(1, "Illust_MagicalSword_Silense", 302721, null),
                new StoryLine(0, "Illust_Adventurer_Thinking", 302722, null),
                new StoryLine(1, "Illust_MagicalSword_Sleep", 302723, null),
                new StoryLine(0, null, 302724, null),
                new StoryLine(1, "Illust_MagicalSword_Silense", 302725, null),
            },
            null,
            new[] { new StoryCue(4, StoryCueKind.Emote, "Sleepy") }),
        new StoryScene("f72_enough", StoryKind.Dialogue, StoryTrigger.FloorFirst, 72,
            new[]
            {
                new StoryLine(0, "Illust_Adventurer_Question", 302760, null),
                new StoryLine(1, "Illust_MagicalSword_Bad", 302761, null),
                new StoryLine(0, "Illust_Adventurer_Question", 302762, null),
                new StoryLine(1, "Illust_MagicalSword_Silense", 302763, null),
            },
            null,
            new[] { new StoryCue(0, StoryCueKind.CamClose, null), new StoryCue(3, StoryCueKind.CamPlayer, null) }),
        new StoryScene("f76_nohome", StoryKind.Dialogue, StoryTrigger.FloorFirst, 76,
            new[]
            {
                new StoryLine(1, "Illust_MagicalSword_Silense", 302800, null),
                new StoryLine(0, null, 302801, null),
                new StoryLine(1, "Illust_MagicalSword_Silense", 302802, null),
                new StoryLine(0, "Illust_Adventurer_Question", 302803, null),
                new StoryLine(1, "Illust_MagicalSword_Silense", 302804, null),
            },
            null,
            null),
        new StoryScene("boss3_intro", StoryKind.Dialogue, StoryTrigger.BossIntro, 3,
            new[]
            {
                new StoryLine(7, null, 302840, null),
                new StoryLine(1, "Illust_MagicalSword_Surprise", 302841, null),
                new StoryLine(7, null, 302842, null),
                new StoryLine(7, null, 302843, null),
                new StoryLine(7, null, 302844, null),
                new StoryLine(1, "Illust_MagicalSword_Silense", 302845, null),
                new StoryLine(0, "Illust_Adventurer_Silence", 302846, null),
                new StoryLine(7, null, 302847, null),
                new StoryLine(7, null, 302848, null),
                new StoryLine(0, "Illust_Adventurer_Thinking", 302849, null),
                new StoryLine(1, "Illust_MagicalSword_Silense", 302850, null),
                new StoryLine(7, null, 302851, null),
                new StoryLine(7, null, 302852, null),
                new StoryLine(7, null, 302853, null),
                new StoryLine(7, null, 302854, null),
                new StoryLine(0, null, 302855, null),
            },
            null,
            new[] { new StoryCue(5, StoryCueKind.Shake, null), new StoryCue(9, StoryCueKind.CamClose, null), new StoryCue(12, StoryCueKind.CamBoss, null), new StoryCue(999, StoryCueKind.Pose, "DrawSword") }),
        new StoryScene("boss3_defeat", StoryKind.Dialogue, StoryTrigger.BossDefeat, 3,
            new[]
            {
                new StoryLine(7, null, 302880, null),
                new StoryLine(1, "Illust_MagicalSword_Silense", 302881, null),
                new StoryLine(7, null, 302882, null),
                new StoryLine(0, "Illust_Adventurer_Question", 302883, null),
                new StoryLine(1, "Illust_MagicalSword_Silense", 302884, null),
                new StoryLine(0, "Illust_Adventurer_Silence", 302885, null),
                new StoryLine(0, "Illust_Adventurer_Silence", 302886, null),
                new StoryLine(1, "Illust_MagicalSword_Silense", 302887, null),
                new StoryLine(1, "Illust_MagicalSword_Nerve", 302888, null),
                new StoryLine(0, "Illust_Adventurer_Thinking", 302889, null),
                new StoryLine(1, "Illust_MagicalSword_Silense", 302890, null),
                new StoryLine(1, "Illust_MagicalSword_Silense", 302891, null),
                new StoryLine(1, "Illust_MagicalSword_Silense", 302892, null),
                new StoryLine(1, "Illust_MagicalSword_Silense", 302893, null),
                new StoryLine(0, "Illust_Adventurer_Thinking", 302894, null),
                new StoryLine(1, "Illust_MagicalSword_Silense", 302895, null),
                new StoryLine(0, "Illust_Adventurer_Silence", 302896, null),
                new StoryLine(1, "Illust_MagicalSword_Silense", 302897, null),
                new StoryLine(0, null, 302898, null),
                new StoryLine(0, null, 302899, null),
                new StoryLine(1, "Illust_MagicalSword_Question", 302900, null),
                new StoryLine(0, null, 302901, null),
            },
            null,
            new[] { new StoryCue(0, StoryCueKind.BgmStop, null), new StoryCue(4, StoryCueKind.Soul, null), new StoryCue(6, StoryCueKind.CamPlayer, null), new StoryCue(999, StoryCueKind.BgmFloor, null) }),
        new StoryScene("ch4_card", StoryKind.Card, StoryTrigger.ChapterStart, 4,
            new[]
            {
                new StoryLine(2, null, 302920, "Intro05"),
                new StoryLine(2, null, 302921, null),
                new StoryLine(2, null, 302922, null),
                new StoryLine(2, null, 302923, "Intro03"),
            },
            null,
            new[] { new StoryCue(0, StoryCueKind.BgmStop, null) }),
        new StoryScene("ch4_start", StoryKind.Dialogue, StoryTrigger.ChapterStart, 4,
            new[]
            {
                new StoryLine(1, "Illust_MagicalSword_Silense", 302960, null),
                new StoryLine(0, "Illust_Adventurer_Thinking", 302961, null),
                new StoryLine(1, null, 302962, null),
                new StoryLine(0, null, 302963, null),
            },
            null,
            new[] { new StoryCue(1, StoryCueKind.CamUp, null), new StoryCue(1, StoryCueKind.BgmFloor, null) }),
        new StoryScene("trait_armor", StoryKind.Dialogue, StoryTrigger.TraitFirst, 8,
            new[]
            {
                new StoryLine(0, "Illust_Adventurer_Thinking", 303000, null),
                new StoryLine(1, "Illust_MagicalSword_Silense", 303001, null),
            },
            null,
            null),
        new StoryScene("f85_honest", StoryKind.Dialogue, StoryTrigger.FloorFirst, 85,
            new[]
            {
                new StoryLine(0, null, 303040, null),
                new StoryLine(1, "Illust_MagicalSword_Silense", 303041, null),
                new StoryLine(0, null, 303042, null),
                new StoryLine(1, null, 303043, null),
            },
            null,
            new[] { new StoryCue(2, StoryCueKind.Emote, "NoAnswer") }),
        new StoryScene("f90_rule", StoryKind.Dialogue, StoryTrigger.FloorFirst, 90,
            new[]
            {
                new StoryLine(0, null, 303080, null),
                new StoryLine(1, "Illust_MagicalSword_Smile", 303081, null),
                new StoryLine(0, null, 303082, null),
                new StoryLine(1, null, 303083, null),
                new StoryLine(1, null, 303084, null),
                new StoryLine(1, null, 303085, null),
                new StoryLine(0, "Illust_Adventurer_Question", 303086, null),
                new StoryLine(1, null, 303087, null),
                new StoryLine(0, "Illust_Adventurer_Thinking", 303088, null),
                new StoryLine(0, "Illust_Adventurer_Question", 303089, null),
                new StoryLine(1, null, 303090, null),
            },
            null,
            null),
        new StoryScene("f95_price", StoryKind.Dialogue, StoryTrigger.FloorFirst, 95,
            new[]
            {
                new StoryLine(1, null, 303120, null),
                new StoryLine(1, null, 303121, null),
                new StoryLine(1, null, 303122, null),
                new StoryLine(1, "Illust_MagicalSword_Silense", 303123, null),
                new StoryLine(0, "Illust_Adventurer_Thinking", 303124, null),
                new StoryLine(1, "Illust_MagicalSword_Silense", 303125, null),
                new StoryLine(0, null, 303126, null),
                new StoryLine(1, "Illust_MagicalSword_Question", 303127, null),
                new StoryLine(0, "Illust_Adventurer_Silence", 303128, null),
            },
            null,
            new[] { new StoryCue(7, StoryCueKind.CamClose, null), new StoryCue(999, StoryCueKind.CamPlayer, null) }),
        new StoryScene("f99_eve", StoryKind.Dialogue, StoryTrigger.FloorFirst, 99,
            new[]
            {
                new StoryLine(1, null, 303160, null),
                new StoryLine(1, "Illust_MagicalSword_Sleep", 303161, null),
                new StoryLine(0, "Illust_Adventurer_Silence", 303162, null),
                new StoryLine(0, null, 303163, null),
            },
            null,
            new[] { new StoryCue(2, StoryCueKind.Emote, "Sleepy") }),
        new StoryScene("f100_lesson", StoryKind.Dialogue, StoryTrigger.FloorFirst, 100,
            new[]
            {
                new StoryLine(0, null, 303200, null),
                new StoryLine(1, null, 303201, null),
                new StoryLine(1, null, 303202, null),
                new StoryLine(1, null, 303203, null),
                new StoryLine(1, null, 303204, null),
                new StoryLine(0, null, 303205, null),
                new StoryLine(1, "Illust_MagicalSword_Smile", 303206, null),
            },
            null,
            new[] { new StoryCue(0, StoryCueKind.Emote, "Sleepy") }),
        new StoryScene("boss4_intro", StoryKind.Dialogue, StoryTrigger.BossIntro, 4,
            new[]
            {
                new StoryLine(8, null, 303240, null),
                new StoryLine(8, null, 303241, null),
                new StoryLine(0, "Illust_Adventurer_Thinking", 303242, null),
                new StoryLine(1, null, 303243, null),
                new StoryLine(8, null, 303244, null),
                new StoryLine(1, "Illust_MagicalSword_Smile", 303245, null),
                new StoryLine(0, null, 303246, null),
                new StoryLine(1, "Illust_MagicalSword_Smile", 303247, null),
            },
            null,
            new[] { new StoryCue(1, StoryCueKind.Shake, null), new StoryCue(2, StoryCueKind.Shake, null), new StoryCue(5, StoryCueKind.Shake, null) }),
        new StoryScene("ending_choice", StoryKind.Choice, StoryTrigger.Ending, 0,
            new[]
            {
                new StoryLine(1, null, 303280, null),
                new StoryLine(1, null, 303281, null),
                new StoryLine(0, "Illust_Adventurer_Silence", 303282, null),
                new StoryLine(1, null, 303283, null),
                new StoryLine(0, "Illust_Adventurer_Thinking", 303284, null),
                new StoryLine(1, "Illust_MagicalSword_Smile", 303285, null),
                new StoryLine(1, null, 303286, null),
                new StoryLine(0, "Illust_Adventurer_Question", 303287, null),
                new StoryLine(1, null, 303288, null),
                new StoryLine(1, null, 303289, null),
                new StoryLine(1, null, 303290, null),
                new StoryLine(0, "Illust_Adventurer_Surprise", 303291, null),
                new StoryLine(1, "Illust_MagicalSword_Smile", 303292, null),
            },
            new[] { new StoryChoice(StoryEnding.Seal, 300200), new StoryChoice(StoryEnding.Hold, 300201) },
            null),
        new StoryScene("ending_seal", StoryKind.Dialogue, StoryTrigger.Ending, 1,
            new[]
            {
                new StoryLine(2, null, 303320, null),
                new StoryLine(2, null, 303321, null),
                new StoryLine(1, "Illust_MagicalSword_Smile", 303322, null),
                new StoryLine(1, "Illust_MagicalSword_Smile", 303323, null),
                new StoryLine(1, null, 303324, null),
                new StoryLine(0, null, 303325, null),
                new StoryLine(1, null, 303326, null),
                new StoryLine(0, "Illust_Adventurer_Silence", 303327, null),
                new StoryLine(1, null, 303328, null),
                new StoryLine(1, "Illust_MagicalSword_Sleep", 303329, null),
                new StoryLine(2, null, 303330, null),
            },
            null,
            new[] { new StoryCue(0, StoryCueKind.Pose, "ContractSword"), new StoryCue(0, StoryCueKind.Fx, "FX_ContractSwordEffect"), new StoryCue(0, StoryCueKind.VortexStop, null), new StoryCue(0, StoryCueKind.Flash, null), new StoryCue(3, StoryCueKind.Pose, "Idle"), new StoryCue(10, StoryCueKind.Emote, "Sleepy"), new StoryCue(10, StoryCueKind.White, null), new StoryCue(11, StoryCueKind.Dark, null) }),
        new StoryScene("epilogue_seal", StoryKind.Card, StoryTrigger.Epilogue, 1,
            new[]
            {
                new StoryLine(2, null, 303360, "Intro04"),
                new StoryLine(2, null, 303361, "00_배경"),
                new StoryLine(2, null, 303362, null),
                new StoryLine(2, null, 303363, null),
                new StoryLine(2, null, 303364, null),
                new StoryLine(2, null, 303365, null),
                new StoryLine(2, null, 303366, null),
                new StoryLine(2, null, 303367, null),
            },
            null,
            null),
        new StoryScene("ending_hold", StoryKind.Dialogue, StoryTrigger.Ending, 2,
            new[]
            {
                new StoryLine(1, "Illust_MagicalSword_Panic", 303400, null),
                new StoryLine(0, null, 303401, null),
                new StoryLine(1, "Illust_MagicalSword_Angry", 303402, null),
                new StoryLine(1, "Illust_MagicalSword_Panic", 303403, null),
                new StoryLine(0, "Illust_Adventurer_Thinking", 303404, null),
                new StoryLine(1, "Illust_MagicalSword_Angry", 303405, null),
                new StoryLine(0, null, 303406, null),
                new StoryLine(1, "Illust_MagicalSword_Silense", 303407, null),
                new StoryLine(1, null, 303408, null),
                new StoryLine(1, null, 303409, null),
                new StoryLine(0, null, 303410, null),
                new StoryLine(0, null, 303411, null),
                new StoryLine(2, null, 303412, null),
            },
            null,
            new[] { new StoryCue(0, StoryCueKind.Bgm, "EgoSword_Encounter_Event"), new StoryCue(0, StoryCueKind.Hands, null), new StoryCue(0, StoryCueKind.Pose, "ContractSword"), new StoryCue(3, StoryCueKind.Shake, null), new StoryCue(8, StoryCueKind.Walk, null), new StoryCue(13, StoryCueKind.Flash, null), new StoryCue(13, StoryCueKind.Dark, null) }),
        new StoryScene("epilogue_hold", StoryKind.Card, StoryTrigger.Epilogue, 2,
            new[]
            {
                new StoryLine(2, null, 303440, "Intro05"),
                new StoryLine(2, null, 303441, null),
                new StoryLine(2, null, 303442, null),
                new StoryLine(2, null, 303443, "Intro01"),
                new StoryLine(2, null, 303444, null),
                new StoryLine(2, null, 303445, null),
            },
            null,
            null),
        new StoryScene("ending_dawn", StoryKind.Dialogue, StoryTrigger.Ending, 3,
            new[]
            {
                new StoryLine(1, "Illust_MagicalSword_Panic", 303480, null),
                new StoryLine(0, null, 303481, null),
                new StoryLine(1, "Illust_MagicalSword_Question", 303482, null),
                new StoryLine(1, "Illust_MagicalSword_Surprise", 303483, null),
                new StoryLine(0, null, 303484, null),
                new StoryLine(1, null, 303485, null),
                new StoryLine(0, "Illust_Adventurer_Thinking", 303486, null),
                new StoryLine(1, "Illust_MagicalSword_Smile", 303487, null),
                new StoryLine(1, "Illust_MagicalSword_Smile", 303488, null),
                new StoryLine(1, null, 303489, null),
                new StoryLine(1, null, 303490, null),
                new StoryLine(1, "Illust_MagicalSword_Smile", 303491, null),
                new StoryLine(2, null, 303492, null),
                new StoryLine(0, "Illust_Adventurer_Silence", 303493, null),
                new StoryLine(1, null, 303494, null),
                new StoryLine(1, null, 303495, null),
                new StoryLine(0, null, 303496, null),
                new StoryLine(1, "Illust_MagicalSword_Smile", 303497, null),
            },
            null,
            new[] { new StoryCue(0, StoryCueKind.Hands, null), new StoryCue(3, StoryCueKind.Shake, null), new StoryCue(11, StoryCueKind.Pose, "ContractSword"), new StoryCue(11, StoryCueKind.Fx, "FX_ContractSwordEffect"), new StoryCue(11, StoryCueKind.VortexIn, null), new StoryCue(11, StoryCueKind.Bgm, "MainTitle_BGM"), new StoryCue(13, StoryCueKind.White, null), new StoryCue(14, StoryCueKind.Clear, null), new StoryCue(14, StoryCueKind.Pose, "Idle") }),
        new StoryScene("epilogue_dawn", StoryKind.Card, StoryTrigger.Epilogue, 3,
            new[]
            {
                new StoryLine(2, null, 303520, "ForestIllust"),
                new StoryLine(2, null, 303521, "ForestColorIllust"),
                new StoryLine(2, null, 303522, null),
                new StoryLine(2, null, 303523, null),
                new StoryLine(2, null, 303524, null),
                new StoryLine(2, null, 303525, "Intro02"),
                new StoryLine(2, null, 303526, null),
                new StoryLine(2, null, 303527, "LoadingIllust2"),
            },
            null,
            null),
        new StoryScene("credits", StoryKind.Credits, StoryTrigger.Credits, 0,
            new[]
            {
                new StoryLine(2, null, 303560, null),
                new StoryLine(2, null, 303561, null),
                new StoryLine(2, null, 303562, null),
                new StoryLine(2, null, 303563, null),
                new StoryLine(2, null, 303564, null),
                new StoryLine(2, null, 303565, null),
                new StoryLine(2, null, 303566, null),
            },
            null,
            null),
        new StoryScene("bark_basic_1", StoryKind.Bark, StoryTrigger.FloorType, 0,
            new[]
            {
                new StoryLine(1, null, 303600, null),
            },
            null,
            null),
        new StoryScene("bark_basic_2", StoryKind.Bark, StoryTrigger.FloorType, 0,
            new[]
            {
                new StoryLine(0, null, 303640, null),
            },
            null,
            null),
        new StoryScene("bark_basic_3", StoryKind.Bark, StoryTrigger.FloorType, 0,
            new[]
            {
                new StoryLine(1, null, 303680, null),
            },
            null,
            null),
        new StoryScene("bark_stingy_1", StoryKind.Bark, StoryTrigger.FloorType, 1,
            new[]
            {
                new StoryLine(1, null, 303720, null),
            },
            null,
            null),
        new StoryScene("bark_stingy_2", StoryKind.Bark, StoryTrigger.FloorType, 1,
            new[]
            {
                new StoryLine(0, "Illust_Adventurer_Thinking", 303760, null),
            },
            null,
            null),
        new StoryScene("bark_stingy_3", StoryKind.Bark, StoryTrigger.FloorType, 1,
            new[]
            {
                new StoryLine(1, null, 303800, null),
            },
            null,
            null),
        new StoryScene("bark_gate_1", StoryKind.Bark, StoryTrigger.FloorType, 2,
            new[]
            {
                new StoryLine(1, null, 303840, null),
            },
            null,
            null),
        new StoryScene("bark_gate_2", StoryKind.Bark, StoryTrigger.FloorType, 2,
            new[]
            {
                new StoryLine(0, "Illust_Adventurer_Thinking", 303880, null),
            },
            null,
            null),
        new StoryScene("bark_gate_3", StoryKind.Bark, StoryTrigger.FloorType, 2,
            new[]
            {
                new StoryLine(1, null, 303920, null),
            },
            null,
            null),
        new StoryScene("bark_plenty_1", StoryKind.Bark, StoryTrigger.FloorType, 3,
            new[]
            {
                new StoryLine(1, null, 303960, null),
            },
            null,
            null),
        new StoryScene("bark_plenty_2", StoryKind.Bark, StoryTrigger.FloorType, 3,
            new[]
            {
                new StoryLine(0, null, 304000, null),
            },
            null,
            null),
        new StoryScene("bark_plenty_3", StoryKind.Bark, StoryTrigger.FloorType, 3,
            new[]
            {
                new StoryLine(1, null, 304040, null),
            },
            null,
            null),
        new StoryScene("bark_treasure_1", StoryKind.Bark, StoryTrigger.FloorType, 4,
            new[]
            {
                new StoryLine(1, null, 304080, null),
            },
            null,
            null),
        new StoryScene("bark_treasure_2", StoryKind.Bark, StoryTrigger.FloorType, 4,
            new[]
            {
                new StoryLine(1, null, 304120, null),
            },
            null,
            null),
        new StoryScene("bark_treasure_3", StoryKind.Bark, StoryTrigger.FloorType, 4,
            new[]
            {
                new StoryLine(0, "Illust_Adventurer_Thinking", 304160, null),
            },
            null,
            null),
        new StoryScene("bark_death_1", StoryKind.Bark, StoryTrigger.Death, 0,
            new[]
            {
                new StoryLine(1, null, 304200, null),
            },
            null,
            null),
        new StoryScene("bark_death_2", StoryKind.Bark, StoryTrigger.Death, 0,
            new[]
            {
                new StoryLine(1, null, 304240, null),
            },
            null,
            null),
        new StoryScene("bark_death_3", StoryKind.Bark, StoryTrigger.Death, 0,
            new[]
            {
                new StoryLine(1, null, 304280, null),
            },
            null,
            null),
        new StoryScene("bark_death_4", StoryKind.Bark, StoryTrigger.Death, 0,
            new[]
            {
                new StoryLine(0, "Illust_Adventurer_Silence", 304320, null),
            },
            null,
            null),
        new StoryScene("bark_death_5", StoryKind.Bark, StoryTrigger.Death, 0,
            new[]
            {
                new StoryLine(1, null, 304360, null),
            },
            null,
            null),
    };

    // 층별 사실 — 인덱스는 층 번호(1~100). 0 번과 손수 만든 1~4층은 비어 있다.
    // generate_content.floor_type: 0 기본 1 인색 2 관문 3 넉넉 4 보물
    public static readonly sbyte[] FloorTypes =
    {
        -1, -1, -1, -1, -1, 0, 1, 2, 3, 4, 0, 1, 2, 3, 4, 0, 1, 2, 3, 4,
        0, 1, 2, 3, 4, 0, 1, 2, 3, 4, 0, 1, 2, 3, 4, 0, 1, 2, 3, 4,
        0, 1, 2, 3, 4, 0, 1, 2, 3, 4, 0, 1, 2, 3, 4, 0, 1, 2, 3, 4,
        0, 1, 2, 3, 4, 0, 1, 2, 3, 4, 0, 1, 2, 3, 4, 0, 1, 2, 3, 4,
        0, 1, 2, 3, 4, 0, 1, 2, 3, 4, 0, 1, 2, 3, 4, 0, 1, 2, 3, 4,
        0,
    };
    // 금고 문(네 번째 문)의 Door._doorIndex_forActive, 없으면 -1
    public static readonly int[] VaultDoors =
    {
        -1, -1, -1, -1, -1, -1, -1, 10, -1, 16, -1, 23, -1, 32, -1, 37, -1, 44, -1, 52,
        -1, 58, 63, -1, -1, 73, 75, 81, -1, -1, -1, 92, -1, 102, -1, 108, -1, 115, -1, 121,
        -1, 129, -1, 137, -1, 142, 147, -1, -1, 157, 160, 164, -1, -1, -1, 178, -1, 185, -1, 191,
        -1, 197, -1, 207, -1, 212, -1, 219, -1, 227, 230, -1, -1, 242, 246, 249, -1, -1, -1, 262,
        -1, 270, -1, 276, -1, 282, -1, 291, -1, 298, -1, 302, -1, 312, 316, -1, -1, 326, 327, 332,
        -1,
    };
    // 여분 열쇠의 ConsumableItem._itemIndex_forActive, 없으면 -1
    public static readonly int[] SpareKeys =
    {
        -1, -1, -1, -1, -1, 23, -1, -1, 44, -1, -1, -1, 79, -1, -1, -1, 111, -1, -1, -1,
        151, -1, -1, -1, 180, -1, -1, -1, -1, 222, -1, -1, 248, -1, -1, -1, 282, -1, -1, -1,
        322, -1, -1, -1, 354, -1, -1, -1, 384, -1, -1, -1, -1, 427, -1, -1, 453, -1, -1, -1,
        490, -1, -1, -1, 526, -1, -1, -1, 559, -1, -1, -1, 590, -1, -1, -1, -1, 633, -1, -1,
        658, -1, -1, -1, 692, -1, -1, -1, 726, -1, -1, -1, 761, -1, -1, -1, 797, -1, -1, -1,
        -1,
    };
    // 둘 중 하나 보상(칸 끝의 ~)이 있는 층
    public static readonly bool[] ChoiceFloors =
    {
        false, false, false, false, false, false, false, false, false, true, false, false, false, false, true, false, false, false, false, true,
        false, false, false, false, true, false, false, false, false, true, false, false, false, false, true, false, false, false, false, true,
        true, false, false, false, true, false, false, false, false, true, false, false, false, false, true, false, false, false, false, true,
        true, false, false, false, true, false, false, false, false, true, false, false, false, false, true, false, false, false, false, true,
        true, false, false, false, true, false, false, false, false, true, false, false, false, false, true, false, false, false, false, true,
        false,
    };
    // 들어설 때 뜨는 층 유형 바크(Scenes 의 번호), 없으면 -1 — story_gen.bark_schedule (바이블 R13·R15)
    public static readonly int[] FloorBarks =
    {
        -1, -1, -1, -1, -1, -1, -1, 71, -1, -1, 65, -1, -1, -1, 77, -1, -1, -1, 74, -1,
        -1, -1, -1, 75, -1, -1, 68, -1, -1, -1, 66, -1, -1, -1, 78, -1, -1, -1, 76, -1,
        -1, -1, -1, 74, -1, -1, -1, 72, -1, -1, -1, 69, -1, -1, 79, -1, -1, -1, 75, -1,
        -1, -1, -1, 76, -1, -1, -1, 73, -1, -1, 67, -1, -1, -1, 77, -1, -1, -1, 74, -1,
        -1, -1, -1, 75, -1, -1, -1, 72, -1, -1, -1, 70, -1, -1, 78, -1, -1, -1, 76, -1,
        -1,
    };
    // 마검이 값만 말하는 층 — 죽음 바크는 데미안 것만 (바이블 R13)
    public const int SwordQuietFrom = 81, SwordQuietTo = 89;
    // 챕터 c 의 보스(MonsterData id)와 첫 생성 층, 챕터 카드 문구
    public static readonly int[] BossIds = { 900, 901, 902, 903, 904 };
    public static readonly int[] ChapterFirstFloors = { 5, 21, 41, 61, 81 };
    public static readonly int[] ChapterNameIds = { 300100, 300101, 300102, 300103, 300104 };
    public static readonly int[] ChapterSubtitleIds = { 300110, 300111, 300112, 300113, 300114 };
}
