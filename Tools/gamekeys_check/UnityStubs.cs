// GameKeys.cs 가 쓰는 UnityEngine 몫만 흉내 낸다 — 프레임마다 키·축 값을 손으로 넣고 돌린다.
using System;
using System.Collections.Generic;

namespace UnityEngine
{
    public enum KeyCode
    {
        None, Return, KeypadEnter, Escape, Space, Tab, Alpha1, Alpha2, Alpha3, I, M, V, W, A, S, D,
        UpArrow, DownArrow, LeftArrow, RightArrow,
        JoystickButton0, JoystickButton1, JoystickButton2, JoystickButton3, JoystickButton4,
        JoystickButton5, JoystickButton6, JoystickButton7, JoystickButton8, JoystickButton9,
    }

    public enum RuntimeInitializeLoadType { SubsystemRegistration }

    [AttributeUsage(AttributeTargets.Method)]
    public class RuntimeInitializeOnLoadMethodAttribute : Attribute
    {
        public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType type) { }
    }

    public static class Mathf
    {
        public static float Abs(float f) => Math.Abs(f);
        public static float Max(float a, float b) => Math.Max(a, b);
    }

    public static class Time
    {
        public static int frameCount;
        public static float unscaledTime;
    }

    public static class Input
    {
        public static readonly HashSet<KeyCode> Keys = new HashSet<KeyCode>(), Before = new HashSet<KeyCode>();
        // 여기 있는 이름만 Input Manager 에 있는 축이다. 없는 이름은 진짜처럼 ArgumentException.
        public static readonly Dictionary<string, float> Axes = new Dictionary<string, float>();

        public static bool GetKey(KeyCode k) => Keys.Contains(k);
        public static bool GetKeyDown(KeyCode k) => Keys.Contains(k) && !Before.Contains(k);
        public static bool GetKeyUp(KeyCode k) => !Keys.Contains(k) && Before.Contains(k);
        public static bool anyKey => Keys.Count > 0;
        public static bool anyKeyDown { get { foreach (KeyCode k in Keys) if (!Before.Contains(k)) return true; return false; } }

        public static float GetAxisRaw(string axis)
        {
            if (Axes.TryGetValue(axis, out float v) == false)
                throw new ArgumentException($"Input Axis {axis} is not setup.");
            return v;
        }
    }
}
