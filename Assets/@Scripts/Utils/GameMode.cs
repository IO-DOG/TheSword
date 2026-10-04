/// <summary>
/// 판의 규칙. 새 게임에서 고르고(UI_TitleScene) 체크포인트에 남는다(CurPlayerData.Mode).
/// 탑의 법(Tower)은 같은 지도·같은 물건에 몬스터 표만 더 아픈 것(MonsterData_Tower.json)을 쓴다 — 기획 L4.
/// </summary>
public enum GameMode
{
    Normal = 0,
    Tower = 1,
}
