using UnityEngine;

/// <summary>
/// 키보드 또는 패드. 부르는 쪽은 키보드 키로 묻고, 패드에서 같은 일을 하는 자리도 같이 본다(Steam/deck.md 의 표).
/// 패드는 XInput 배치다 — 스팀 덱도 Steam Input 의 Gamepad 템플릿으로 이것을 내놓는다.
///
///   A Enter · B, ≡ Esc · X Y RB 1 2 3 · Y(맵) I · LB Tab · View M · L3 V · RT Space · 왼쪽 스틱, 방향 패드 W A S D · 오른쪽 스틱 줌
///
/// 단추는 레거시 Input 의 joystick button 이라 그대로 묻는다. 스틱·방향 패드·트리거는 축이라 여기서 눌렀다·쥐고 있다·뗐다로
/// 바꾼다 — 키처럼 한 번에 한 방향이고(대각선은 많이 기운 쪽), 쥐고 있는 동안 내내 쥐고 있다(PlayerController 는 쥔 키로 걷는다).
/// 화살표 키는 패드에 없다 — 화살표를 묻는 곳은 다 W A S D 도 같이 묻는다.
///
/// 축은 Input Manager 의 "Horizontal"·"Vertical"(왼쪽 스틱)과 PadDPadX·PadDPadY(위 +1)·PadRT·PadRStickY(위 +1)다.
/// 없는 축을 읽으면 ArgumentException 이라 플레이마다 한 번 물어 두고, 없으면 그 축만 없는 것으로 친다 — 단추는 그대로 된다.
/// 상태는 그 프레임에 처음 물을 때 한 번 잰다. InputManager 가 매 프레임 물으므로 "눌렀다" 가 빠지지 않는다.
/// </summary>
public static class GameKeys
{
    /// <summary>이번 프레임에 눌렀다.</summary>
    public static bool Down(KeyCode key) => Input.GetKeyDown(key) || OnPad(key, Pressed);
    /// <summary>쥐고 있다.</summary>
    public static bool Held(KeyCode key) => Input.GetKey(key) || OnPad(key, Holding);
    /// <summary>이번 프레임에 뗐다.</summary>
    public static bool Up(KeyCode key) => Input.GetKeyUp(key) || OnPad(key, Released);

    /// <summary>목록 고르기. Down 과 같은데 패드 방향은 쥐고 있으면 RepeatDelay 뒤부터 RepeatRate 마다 다시 친다.
    /// 키보드는 예전처럼 되풀이하지 않는다. 걷기는 이것을 쓰지 않는다 — 쥔 키로 걷는다.
    /// 되풀이는 누른 그 프레임에 목록이 받은 것만 한다: 걷던 스틱을 쥔 채 도감을 열면 키보드처럼 목록이 가만히 있다.</summary>
    public static bool Nav(KeyCode key) => Pulse(Virtual(key)) || Down(key);   // Pulse 먼저 — 누른 프레임에 물어야 되풀이가 걸린다

    /// <summary>"아무 키나". Input.anyKeyDown 은 패드 단추를 이미 센다 — 축으로 만든 키(방향·RT)만 더한다. 줌은 마우스 휠처럼 키가 아니다.
    /// 패드를 먼저 묻는다: InputManager 가 매 프레임 AnyHeld 로 상태를 재게 한다(키보드를 쥐고 있어도).</summary>
    public static bool AnyDown => PadAny(Pressed) || Input.anyKeyDown;
    public static bool AnyHeld => PadAny(Holding) || Input.anyKey;

    /// <summary>오른쪽 스틱 위아래 — 위 +1(마우스 휠을 위로 한 칸), 아래 -1, 아니면 0. 기울인 순간 한 번, 쥐고 있으면 되풀이.</summary>
    public static int Zoom => Pulse(Pad.ZoomIn) ? 1 : Pulse(Pad.ZoomOut) ? -1 : 0;

    const int Holding = 0, Pressed = 1, Released = 2;
    const float Press = 0.5f;           // 이만큼 기울이면 눌렀다
    const float Release = 0.3f;         // 쥐고 있던 것은 이 밑으로 돌아와야 뗐다 — 문턱 언저리에서 떨지 않게
    const float Sticky = 0.15f;         // 대각선에서 쥐고 있던 방향을 놓지 않는 몫
    const float RepeatDelay = 0.4f;     // 목록에서 쥐고 있으면 이 뒤부터
    const float RepeatRate = 0.12f;     // 이만큼마다 한 칸

    // 축으로 만든 키. 이름은 그것이 대신하는 키다. Count 는 "없다".
    enum Pad { W, A, S, D, Space, ZoomIn, ZoomOut, Count }

    static readonly bool[] _held = new bool[(int)Pad.Count];
    static readonly bool[] _was = new bool[(int)Pad.Count];
    static readonly bool[] _pulse = new bool[(int)Pad.Count];
    static readonly bool[] _armed = new bool[(int)Pad.Count];     // 누른 프레임에 Nav·Zoom 이 물었다 — 되풀이해도 된다
    static readonly float[] _repeatAt = new float[(int)Pad.Count];
    static int _frame = -1;
    static bool _probed, _hasStick, _hasDPad, _hasRT, _hasZoom;

    // 에디터는 도메인 리로드 없이 플레이를 되풀이한다 — 지난 플레이의 쥔 방향을 넘기지 않고, 그새 더한 축을 다시 묻는다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        _frame = -1;
        _probed = false;
        System.Array.Clear(_held, 0, _held.Length);
        System.Array.Clear(_was, 0, _was.Length);
        System.Array.Clear(_pulse, 0, _pulse.Length);
        System.Array.Clear(_armed, 0, _armed.Length);
    }

    static KeyCode Button(KeyCode key)
    {
        switch (key)
        {
            case KeyCode.Return:
            case KeyCode.KeypadEnter: return KeyCode.JoystickButton0;   // A
            case KeyCode.Escape: return KeyCode.JoystickButton1;        // B (≡ 은 OnPad 가 같이 본다)
            case KeyCode.Alpha1: return KeyCode.JoystickButton2;        // X
            case KeyCode.Alpha2:                                        // Y — 전투에서는 스킬 2, 맵에서는 인벤토리
            case KeyCode.I: return KeyCode.JoystickButton3;
            case KeyCode.Tab: return KeyCode.JoystickButton4;           // LB
            case KeyCode.Alpha3: return KeyCode.JoystickButton5;        // RB
            case KeyCode.M: return KeyCode.JoystickButton6;             // View
            case KeyCode.V: return KeyCode.JoystickButton8;             // L3
            default: return KeyCode.None;
        }
    }

    static Pad Virtual(KeyCode key)
    {
        switch (key)
        {
            case KeyCode.W: return Pad.W;
            case KeyCode.A: return Pad.A;
            case KeyCode.S: return Pad.S;
            case KeyCode.D: return Pad.D;
            case KeyCode.Space: return Pad.Space;                       // RT
            default: return Pad.Count;
        }
    }

    static bool OnPad(KeyCode key, int phase)
    {
        KeyCode button = Button(key);
        if (button != KeyCode.None)
            return Ask(button, phase) || (key == KeyCode.Escape && Ask(KeyCode.JoystickButton7, phase));
        Pad pad = Virtual(key);
        return pad != Pad.Count && Ask(pad, phase);
    }

    static bool Ask(KeyCode button, int phase) =>
        phase == Pressed ? Input.GetKeyDown(button) : phase == Released ? Input.GetKeyUp(button) : Input.GetKey(button);

    static bool Ask(Pad pad, int phase)
    {
        Poll();
        int i = (int)pad;
        return phase == Pressed ? _held[i] && _was[i] == false : phase == Released ? _held[i] == false && _was[i] : _held[i];
    }

    static bool PadAny(int phase)
    {
        for (Pad pad = Pad.W; pad <= Pad.Space; pad++)
            if (Ask(pad, phase))
                return true;
        return false;
    }

    static bool Pulse(Pad pad)
    {
        if (pad == Pad.Count)
            return false;
        Poll();
        int i = (int)pad;
        if (_held[i] && _was[i] == false)
            _armed[i] = true;
        return _pulse[i] && _armed[i];
    }

    static void Poll()
    {
        if (_frame == Time.frameCount)
            return;
        _frame = Time.frameCount;
        if (_probed == false)
        {
            _probed = true;
            _hasStick = Has("Horizontal") && Has("Vertical");
            _hasDPad = Has("PadDPadX") && Has("PadDPadY");
            _hasRT = Has("PadRT");
            _hasZoom = Has("PadRStickY");
        }

        float x = 0f, y = 0f;
        if (_hasStick)
        {
            x = Stick("Horizontal", KeyCode.A, KeyCode.D, KeyCode.LeftArrow, KeyCode.RightArrow);
            y = Stick("Vertical", KeyCode.W, KeyCode.S, KeyCode.UpArrow, KeyCode.DownArrow);
        }
        if (_hasDPad)
        {
            x = Larger(x, Input.GetAxisRaw("PadDPadX"));
            y = Larger(y, Input.GetAxisRaw("PadDPadY"));
        }

        System.Array.Copy(_held, _was, _held.Length);
        Pad dir = Direction(x, y);
        for (Pad pad = Pad.W; pad <= Pad.D; pad++)
            _held[(int)pad] = pad == dir;
        _held[(int)Pad.Space] = _hasRT && Input.GetAxisRaw("PadRT") > Threshold(Pad.Space);
        float zoom = _hasZoom ? Input.GetAxisRaw("PadRStickY") : 0f;
        _held[(int)Pad.ZoomIn] = zoom > Threshold(Pad.ZoomIn);
        _held[(int)Pad.ZoomOut] = -zoom > Threshold(Pad.ZoomOut);

        // 메뉴가 떠 있으면 시간이 멈춘다(UIManager.RefreshTimeScale) — 되풀이는 실시간으로 잰다.
        float now = Time.unscaledTime;
        for (int i = 0; i < _held.Length; i++)
        {
            _pulse[i] = _held[i] && (_was[i] == false || now >= _repeatAt[i]);
            if (_pulse[i])
                _repeatAt[i] = now + (_was[i] ? RepeatRate : RepeatDelay);
            if (_held[i] == false)
                _armed[i] = false;
        }
    }

    static float Threshold(Pad pad) => _was[(int)pad] ? Release : Press;

    // 네 방향 중 하나 — 키 하나를 쥔 것과 같다. 대각선은 많이 기운 쪽이고, 둘이 비슷하면(방향 패드의 대각선) 쥐고 있던 쪽을 놓지 않는다.
    static Pad Direction(float x, float y)
    {
        bool wasX = _was[(int)Pad.A] || _was[(int)Pad.D];
        bool wasY = _was[(int)Pad.W] || _was[(int)Pad.S];
        float ax = Mathf.Abs(x), ay = Mathf.Abs(y);
        if (Mathf.Max(ax, ay) < (wasX || wasY ? Release : Press))
            return Pad.Count;
        bool horizontal = wasX ? ax >= ay - Sticky : wasY ? ax > ay + Sticky : ax > ay;
        return horizontal ? (x > 0f ? Pad.D : Pad.A) : (y > 0f ? Pad.W : Pad.S);
    }

    // "Horizontal"·"Vertical" 은 키보드 줄과 스틱 줄이 같은 이름이라 GetAxisRaw 가 둘 중 큰 값을 준다. 키보드가 쥐고 있으면
    // 그 값은 키보드 것이니 버린다 — 안 그러면 화살표 키가 패드의 W A S D 가 되어 화살표로 걷는다.
    static float Stick(string axis, KeyCode a, KeyCode b, KeyCode c, KeyCode d) =>
        Input.GetKey(a) || Input.GetKey(b) || Input.GetKey(c) || Input.GetKey(d) ? 0f : Input.GetAxisRaw(axis);

    static float Larger(float a, float b) => Mathf.Abs(a) >= Mathf.Abs(b) ? a : b;

    static bool Has(string axis)
    {
        try
        {
            Input.GetAxisRaw(axis);
            return true;
        }
        catch (System.ArgumentException)
        {
            return false;
        }
    }
}
