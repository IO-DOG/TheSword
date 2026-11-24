// 생성 파일 — Tools/generate_content.py 가 쓴다(Tools/gimmicks.py). 손으로 고치지 않는다.
using System.Collections.Generic;

public static class GeneratedGimmicks
{
    /// <summary>층(1부터) -> 물살 칸 (열, 행, 방향 0위 1오른쪽 2아래 3왼쪽). 그 층 레버를 당기면 멈춘다.</summary>
    public static readonly Dictionary<int, (int x, int y, int dir)[]> Currents = new Dictionary<int, (int x, int y, int dir)[]>
    {
        { 21, new[] { (14, 11, 2) } },
        { 22, new[] { (15, 11, 0) } },
        { 23, new[] { (12, 19, 3) } },
        { 24, new[] { (17, 9, 1) } },
        { 26, new[] { (13, 6, 2) } },
        { 27, new[] { (18, 13, 2) } },
        { 28, new[] { (8, 10, 0) } },
        { 29, new[] { (12, 18, 2) } },
        { 31, new[] { (16, 17, 2) } },
        { 32, new[] { (13, 17, 1) } },
        { 33, new[] { (10, 6, 0) } },
        { 34, new[] { (19, 13, 0) } },
        { 35, new[] { (7, 18, 0) } },
        { 36, new[] { (21, 7, 1) } },
        { 37, new[] { (16, 17, 2) } },
        { 38, new[] { (13, 16, 3) } },
    };
}
