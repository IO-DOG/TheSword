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
        { 92, new[] { (11, 16, 3) } },
    };

    /// <summary>층(1부터) -> 짝 균열 두 칸 (열, 행). 한쪽에 들어서면 다른 쪽으로 옮겨진다(같은 구역의 관문 하나를 건너뛴다).</summary>
    public static readonly Dictionary<int, (int ax, int ay, int bx, int by)> Rifts = new Dictionary<int, (int ax, int ay, int bx, int by)>
    {
        { 81, (5, 18, 19, 20) },
        { 82, (3, 18, 11, 6) },
        { 83, (11, 1, 18, 2) },
        { 84, (13, 1, 3, 2) },
        { 85, (18, 17, 10, 5) },
        { 86, (7, 18, 21, 21) },
        { 87, (8, 15, 8, 3) },
        { 88, (11, 19, 22, 14) },
        { 89, (2, 4, 5, 13) },
        { 90, (14, 2, 22, 7) },
        { 91, (5, 20, 23, 20) },
        { 93, (23, 4, 11, 8) },
        { 94, (3, 10, 12, 16) },
        { 95, (21, 2, 11, 5) },
        { 96, (12, 15, 21, 17) },
        { 97, (17, 15, 5, 14) },
        { 98, (12, 10, 21, 1) },
        { 99, (2, 3, 1, 8) },
    };
}
