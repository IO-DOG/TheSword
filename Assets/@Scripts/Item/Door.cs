using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Door : MonoBehaviour
{
    /// <summary>
    /// 어떤 콜라이더에서든 그 문의 Door 를 찾아낸다.
    ///
    /// 문 타일은 프리팹마다 계층이 달라서, Door 가 콜라이더와 같은 오브젝트에
    /// 있기도 하고 형제 자식에 있기도 하다. GetComponent 하나로 집으면
    /// 생성된 층에서 null 이 되고, 그러면 문이 영영 안 열린다.
    /// 부모로 한 칸씩 올라가며 그 아래를 통째로 뒤진다.
    /// </summary>
    public static Door Find(GameObject from)
    {
        // 문은 타일 하나에 하나뿐이라 형제까지 뒤져도 안전하다.
        return Util.FindInTile<Door>(from);
    }

    public int _keyIndex = 0;
    public int _doorIndex_forActive = 0;
    CameraController _camera;

    private void Start()
    {
        _camera = Camera.main.GetComponentInChildren<CameraController>();
        _rotateAngle = new Vector3(0f, transform.rotation.eulerAngles.y + 90f, 0f);
        _doorLockPos = transform.parent.GetChild(1);
        _mark = KeyMark.OnDoor(this);   // 문이 열리면(이 오브젝트가 꺼지면) 같이 사라진다

        //#region Fade Setting
        //GetComponent<MeshRenderer>().material.SetFloat("_Mode", 2);
        //GetComponent<MeshRenderer>().material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        //GetComponent<MeshRenderer>().material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        //GetComponent<MeshRenderer>().material.SetInt("_ZWrite", 0);
        //GetComponent<MeshRenderer>().material.DisableKeyword("_ALPHATEST_ON");
        //GetComponent<MeshRenderer>().material.EnableKeyword("_ALPHABLEND_ON");
        //GetComponent<MeshRenderer>().material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        //GetComponent<MeshRenderer>().material.renderQueue = 3000;
        //#endregion
    }

    #region Open Door Effect
    Vector3 _rotateAngle;
    Transform _doorLockPos;
    GameObject _mark;

    Coroutine _openDoorCoroutine;
    public void CoOpenDoor(float time)
    {
        gameObject.GetComponent<Collider>().enabled = false;
        if (_mark != null)
            _mark.SetActive(false);     // 열쇠를 쓴 순간 걷는다 — 문짝이 돌며 사라지는 동안 남아 있지 않게

        Managers.Sound.Play(Define.Sound.Effect, "DoorUnlock_SFX");
        _openDoorCoroutine = StartCoroutine(OpenDoor(time));
        GameEvents.RaiseDoorOpened(_keyIndex);
    }

    IEnumerator OpenDoor(float time)
    {
        yield return new WaitForSeconds(1f);
        Managers.Sound.Play(Define.Sound.Effect, "DoorOpen_SFX");

        float elapsedTime = 0.0f;
        Quaternion targetRotation = Quaternion.Euler(_rotateAngle);

        while (elapsedTime < time)
        {
            transform.rotation = Quaternion.Euler(Vector3.Lerp(transform.rotation.eulerAngles, targetRotation.eulerAngles, elapsedTime / time));

            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }

    Coroutine _doorLockOpenAnimCoroutine;
    public void CoDoorLockOpenAnim()
    {
        _doorLockOpenAnimCoroutine = StartCoroutine(DoorLockOpenAnim());
    }

    // 자물쇠 그림이 없어도 문은 열리고 잠금은 풀려야 한다 — 여기서 예외가 나면 OnInteract 가 영영 남아 플레이어가 멈춘다.
    const float LockOpenTime = 0.68f;   // DoorLockOpen.anim 길이
    const float LockShakeTime = 0.42f;  // DoorLockIsLocked.anim 길이

    GameObject SpawnLock(string state, out float time)
    {
        GameObject go = Managers.Resource.Instantiate("DoorLock", _doorLockPos);
        time = state == null ? LockOpenTime : LockShakeTime;
        if (go == null)
            return null;
        Animator animator = go.GetComponent<Animator>();
        if (animator != null)
        {
            if (state != null)
                animator.Play(state);
            time = animator.GetCurrentAnimatorStateInfo(0).length;
        }
        if (_camera != null)
            go.transform.localScale = new Vector3(go.transform.localScale.x, go.transform.localScale.y * _camera._scaleMultiplier, go.transform.localScale.z * _camera._scaleMultiplier);
        return go;
    }

    IEnumerator DoorLockOpenAnim()
    {
        GameObject go = SpawnLock(null, out float time);
        yield return new WaitForSeconds(time);
        Managers.Resource.Destroy(go);
        Managers.Game.OnInteract = false;
    }


    /// <summary>열쇠 없이 문에 부딪혔다 (자물쇠 + FX_Cross).</summary>
    public void CoDoorLockLockedAnim()
    {
        _doorLockLockedAnimCoroutine = StartCoroutine(DoorLockLockedAnim());
        GameEvents.RaiseDoorBlocked(_keyIndex);
    }


    Coroutine _doorLockLockedAnimCoroutine;
    IEnumerator DoorLockLockedAnim()
    {
        Managers.Sound.Play(Define.Sound.Effect, "DoorLock_SFX");

        GameObject go = SpawnLock("DoorLockIsLocked", out float time);
        GameObject cross = Managers.Resource.Instantiate("FX_Cross", _doorLockPos);
        yield return new WaitForSeconds(time);
        Managers.Resource.Destroy(go);
        Managers.Resource.Destroy(cross);
    }



    public Sequence FadeDoor()
    {
        Tween tween1 = gameObject.GetComponent<MeshRenderer>().material.DOFade(1f, 1f);
        Tween tween2 = gameObject.GetComponent<MeshRenderer>().material.DOFade(0.0f, 0.8f);

        Sequence seq = DOTween.Sequence();
        seq.Append(tween1).Append(tween2);

        return seq;
    }
    #endregion Effect
}
