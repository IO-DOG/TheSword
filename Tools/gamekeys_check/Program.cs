using System;
using System.Reflection;
using UnityEngine;

// GameKeys(Assets/@Scripts/Managers/Core/GameKeys.cs)의 축 → 키 바꾸기를 프레임 단위로 돌려 본다. 에디터 없이 돈다 —
// 에디터에서는 패드 축 값을 손으로 넣을 수 없다. 실패하면 0 이 아닌 값으로 끝난다.
//   dotnet run --project Tools/gamekeys_check
static class Program
{
    static int _failed;

    static void Check(bool ok, string what)
    {
        if (ok == false)
        {
            _failed++;
            Console.WriteLine("FAIL " + what);
        }
    }

    // 다음 프레임. 앞 프레임의 키를 Before 로 넘긴 뒤 이번 프레임의 입력(input)을 넣는다(GetKeyDown·GetKeyUp).
    static void Step(Action input = null, float dt = 1f / 60f)
    {
        Input.Before.Clear();
        Input.Before.UnionWith(Input.Keys);
        input?.Invoke();
        Time.frameCount++;
        Time.unscaledTime += dt;
    }

    // 새 플레이. axes 만 Input Manager 에 있는 축이다.
    static void Fresh(params string[] axes)
    {
        typeof(GameKeys).GetMethod("ResetStatics", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
        Input.Keys.Clear();
        Input.Before.Clear();
        Input.Axes.Clear();
        foreach (string a in axes)
            Input.Axes[a] = 0f;
        Step();
    }

    static int Main()
    {
        string[] all = { "Horizontal", "Vertical", "PadDPadX", "PadDPadY", "PadRT", "PadRStickY" };

        // 1. 스틱 위: 누른 프레임에만 Down, 쥔 동안 Held, 문턱 언저리(0.4)는 그대로 쥐고, 0.3 밑에서 뗀다.
        Fresh(all);
        Step(() => { Input.Axes["Vertical"] = 0.6f; });
        Check(GameKeys.Down(KeyCode.W) && GameKeys.Held(KeyCode.W) && !GameKeys.Held(KeyCode.S), "stick up press");
        Check(GameKeys.AnyDown && GameKeys.AnyHeld, "stick counts as any key");
        Step(() => { Input.Axes["Vertical"] = 0.4f; });
        Check(!GameKeys.Down(KeyCode.W) && GameKeys.Held(KeyCode.W), "stick up hysteresis hold");
        Step(() => { Input.Axes["Vertical"] = 0.2f; });
        Check(GameKeys.Up(KeyCode.W) && !GameKeys.Held(KeyCode.W), "stick up release");
        Step(() => { Input.Axes["Vertical"] = 0.45f; });
        Check(!GameKeys.Held(KeyCode.W), "below press threshold from rest");

        // 2. 방향 패드 대각선: 쥐고 있던 축을 놓지 않고, 그 축을 놓으면 같은 프레임에 뗐다·눌렀다.
        Fresh(all);
        Step(() => { Input.Axes["PadDPadX"] = -1f; });
        Check(GameKeys.Down(KeyCode.A), "dpad left");
        Step(() => { Input.Axes["PadDPadY"] = 1f; });
        Check(GameKeys.Held(KeyCode.A) && !GameKeys.Held(KeyCode.W), "dpad diagonal keeps held axis");
        Step(() => { Input.Axes["PadDPadX"] = 0f; });
        Check(GameKeys.Up(KeyCode.A) && GameKeys.Down(KeyCode.W), "dpad diagonal hands over");
        Fresh(all);
        Step(() => { Input.Axes["PadDPadX"] = 1f; Input.Axes["PadDPadY"] = -1f; });
        Check(GameKeys.Held(KeyCode.S) && !GameKeys.Held(KeyCode.D), "fresh diagonal picks vertical");

        // 3. 화살표 키는 패드가 아니다: 같은 이름의 키보드 줄이 "Vertical" 을 1 로 만들어도 W 로 치지 않는다.
        Fresh(all);
        Step(() => { Input.Keys.Add(KeyCode.UpArrow); Input.Axes["Vertical"] = 1f; });
        Check(!GameKeys.Held(KeyCode.W) && !GameKeys.Down(KeyCode.W), "arrow key is not pad W");
        Check(GameKeys.Down(KeyCode.UpArrow), "arrow key still reads as itself");

        // 4. 목록 되풀이: 누른 순간, 0.4초 뒤, 그 뒤 0.12초마다(0.95초 안에 0.40·0.52·0.64·0.76·0.88). 키보드는 되풀이 없다.
        Fresh(all);
        Step(() => { Input.Axes["Vertical"] = -1f; }, 0.01f);
        Check(GameKeys.Nav(KeyCode.S), "nav first");
        int pulses = 0;
        for (int i = 0; i < 95; i++)
        {
            Step(null, 0.01f);
            if (GameKeys.Nav(KeyCode.S))
                pulses++;
        }
        Check(pulses == 5, $"nav repeats within 0.95s: {pulses}, want 5");
        Check(!GameKeys.Down(KeyCode.S), "repeat is not Down");
        Fresh(all);
        Step(() => { Input.Keys.Add(KeyCode.S); }, 0.01f);
        Check(GameKeys.Nav(KeyCode.S), "keyboard nav press");
        pulses = 0;
        for (int i = 0; i < 95; i++)
        {
            Step(null, 0.01f);
            if (GameKeys.Nav(KeyCode.S))
                pulses++;
        }
        Check(pulses == 0, "keyboard nav does not repeat");
        // 걷던 스틱을 쥔 채 목록이 떴다(누른 프레임에 Nav 를 묻지 않았다) — 키보드처럼 가만히 있다.
        Fresh(all);
        Step(() => { Input.Axes["Vertical"] = -1f; }, 0.01f);
        Check(GameKeys.Held(KeyCode.S), "walking hold");
        pulses = 0;
        for (int i = 0; i < 95; i++)
        {
            Step(null, 0.01f);
            if (GameKeys.Nav(KeyCode.S))
                pulses++;
        }
        Check(pulses == 0, $"held-over stick does not scroll a new list: {pulses}");

        // 5. RT = Space (쥐면 8배속), 오른쪽 스틱 = 줌, 단추.
        Fresh(all);
        Step(() => { Input.Axes["PadRT"] = 0.6f; });
        Check(GameKeys.Down(KeyCode.Space) && GameKeys.Held(KeyCode.Space), "RT press");
        Step(() => { Input.Axes["PadRT"] = 0f; });
        Check(GameKeys.Up(KeyCode.Space), "RT release");
        Step(() => { Input.Axes["PadRStickY"] = 0.9f; });
        Check(GameKeys.Zoom == 1, "right stick up zooms in");
        Step();
        Check(GameKeys.Zoom == 0 && !GameKeys.AnyDown, "zoom once, and zoom is not a key");
        Step(() => { Input.Keys.Add(KeyCode.JoystickButton7); });
        Check(GameKeys.Down(KeyCode.Escape) && !GameKeys.Down(KeyCode.Return), "Menu is Esc");
        Step(() => { Input.Keys.Add(KeyCode.JoystickButton4); });
        Check(GameKeys.Held(KeyCode.Tab) && GameKeys.Down(KeyCode.Tab), "LB is Tab");
        Step(() => { Input.Keys.Add(KeyCode.JoystickButton3); });
        Check(GameKeys.Down(KeyCode.Alpha2) && GameKeys.Down(KeyCode.I), "Y is skill 2 and inventory");

        // 6. 패드 축이 Input Manager 에 없으면 예외 없이 단추만 된다.
        Fresh("Horizontal", "Vertical");
        Step(() => { Input.Keys.Add(KeyCode.JoystickButton0); });
        Check(GameKeys.Down(KeyCode.Return) && !GameKeys.Held(KeyCode.Space) && GameKeys.Zoom == 0, "missing pad axes degrade to buttons");
        Fresh();
        Check(!GameKeys.AnyHeld && !GameKeys.Held(KeyCode.W), "no axes at all");

        Console.WriteLine(_failed == 0 ? "gamekeys_check: ok" : $"gamekeys_check: {_failed} failed");
        return _failed == 0 ? 0 : 1;
    }
}
