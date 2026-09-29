using System;
using System.Collections.Generic;
using UnityEngine;

// 몬스터와 부딪힌 순간과 전투가 열리는 순간 사이의 관문.
//
// 보스 등장 연출, "이 싸움은 죽는다" 확인 창 같은 것이 전투 직전에 끼어들어야 하는데,
// 각자 PlayerController 를 고치면 순서가 엉킨다. 그래서 끼어들 자리를 하나로 모았다.
//
// 끼어드는 쪽(Interceptor)은 true 를 돌려주면 흐름을 맡는다 — 할 일을 마친 뒤
// proceed() 를 부르면 다음 끼어드는 쪽으로, 마지막엔 전투로 넘어간다.
// proceed 를 끝내 부르지 않으면 싸우지 않는다(취소). false 를 돌려주면 그냥 지나간다.
public static class FightGate
{
    public delegate bool Interceptor(MonsterController monster, Action proceed);

    struct Entry { public Interceptor Fn; public int Order; }
    static readonly List<Entry> s_entries = new List<Entry>();

    // 관문이 처리 중이다 (연출·확인 창이 떠 있다). 이동 입력을 막는 데 쓴다.
    public static bool Pending { get; private set; }

    // order 가 작을수록 먼저 묻는다. 스토리 연출(0)이 확인 창(100)보다 먼저다.
    public static void Add(Interceptor fn, int order)
    {
        Remove(fn);
        s_entries.Add(new Entry { Fn = fn, Order = order });
        s_entries.Sort((a, b) => a.Order.CompareTo(b.Order));
    }

    public static void Remove(Interceptor fn) => s_entries.RemoveAll(e => e.Fn == fn);

    // PlayerController 가 몬스터와 부딪혔을 때 부른다. start 가 실제로 전투를 연다.
    public static void Request(MonsterController monster, Action start)
    {
        if (Pending || monster == null || start == null)
            return;
        Pending = true;
        Step(0, monster, start);
    }

    // 씬을 다시 올릴 때처럼, 중간에 흐름이 끊긴 채로 남지 않게 한다.
    public static void Reset() => Pending = false;

    static void Step(int index, MonsterController monster, Action start)
    {
        for (int i = index; i < s_entries.Count; i++)
        {
            int next = i + 1;
            bool called = false;
            Action proceed = () =>
            {
                if (called) return;   // 두 번 불러도 전투는 한 번
                called = true;
                Step(next, monster, start);
            };

            bool took;
            try { took = s_entries[i].Fn(monster, proceed); }
            catch (Exception e) { Debug.LogException(e); took = false; }

            if (took)
                return;
        }

        Pending = false;
        if (monster == null)
            return;
        try { start(); }
        catch (Exception e) { Debug.LogException(e); }
    }

    // 끼어든 쪽이 전투를 아예 취소할 때 (확인 창에서 "아니오").
    public static void Cancel() => Pending = false;
}
