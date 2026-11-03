using TMPro;
using UnityEngine;

/// <summary>
/// 키 안내 글. 키보드면 그 문구, 패드면 패드 짝을 보이고, 장치나 언어가 바뀌면 다시 칠한다 —
/// 전투 중에 패드를 집어도 떠 있는 안내가 따라 바뀐다. 다시 Set 하면 문구만 갈아 끼운다.
/// 장치는 GameKeys.UsingPad(입력만 안다), 짝은 Tools/ui_text_parts/pad.py 의 TWIN(GeneratedUiText.PadTwin).
/// </summary>
public class PromptText : MonoBehaviour
{
    TMP_Text _text;
    System.Func<string> _make;
    int _device = -1;
    Define.ScriptType _language;

    /// <summary>키 안내 문구의 id. 패드면 그 짝, 짝이 없으면 그대로.</summary>
    public static int Id(int id)
    {
        int pad = GameKeys.UsingPad ? GeneratedUiText.PadTwin(id) : 0;
        return pad != 0 ? pad : id;
    }

    public static string Get(int id) => Managers.GetString(Id(id));

    /// <summary>전투 스킬 칸의 단추 이름 — 0 강타 · 1 철벽 · 2 흡혈(BattleSkills.Kind 순서, GameKeys.Button 의 X Y RB).</summary>
    public static string SkillKey(int i) => GameKeys.UsingPad ? PadSkillKeys[i] : (i + 1).ToString();
    static readonly string[] PadSkillKeys = { "X", "Y", "RB" };

    public static void Set(TMP_Text text, int id) => Set(text, () => Get(id));

    public static void Set(TMP_Text text, System.Func<string> make)
    {
        PromptText prompt = text.GetComponent<PromptText>();
        if (prompt == null)
            prompt = text.gameObject.AddComponent<PromptText>();
        prompt._text = text;
        prompt._make = make;
        prompt.Paint();
    }

    void LateUpdate()
    {
        if (_device != GameKeys.DeviceVersion || _language != GameSettings.Language)
            Paint();
    }

    void Paint()
    {
        _device = GameKeys.DeviceVersion;
        _language = GameSettings.Language;
        _text.text = _make();
    }
}
