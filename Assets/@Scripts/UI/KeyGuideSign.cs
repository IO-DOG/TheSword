using UnityEngine;

/// <summary>
/// 1층 표지판 "MOVE WASD"(Dungeon_00_000 의 GuidePanel). 패드로 하는 동안은 애니메이터를 세우고 십자 방향 패드를 그린
/// 표지판(GuidePanel_Pad)으로 둔다 — 쓰지 않는 장치의 글리프를 보이지 않는다(Steam Deck Verified). 프리팹에 처음 놓인
/// 그림(GuidePanel_Control)은 ↑ ← ↓ → 키보드 방향키라 그것도 안 된다. 키보드로 돌아오면 다시 WASD 가 깜박인다.
/// </summary>
public class KeyGuideSign : MonoBehaviour
{
    [SerializeField] Sprite _padSprite;
    Animator _animator;
    SpriteRenderer _renderer;
    int _device = -1;

    void Awake()
    {
        _animator = GetComponent<Animator>();
        _renderer = GetComponent<SpriteRenderer>();
    }

    void LateUpdate()
    {
        if (_device == GameKeys.DeviceVersion || _padSprite == null)
            return;
        _device = GameKeys.DeviceVersion;
        _animator.enabled = GameKeys.UsingPad == false;
        if (GameKeys.UsingPad)
            _renderer.sprite = _padSprite;
    }
}
