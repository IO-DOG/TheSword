using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Door : MonoBehaviour
{
    public int _keyIndex = 0;
    public int _doorIndex_forActive = 0;
    CameraController _camera;

    private void Start()
    {
        _camera = Camera.main.GetComponentInChildren<CameraController>();
        _rotateAngle = new Vector3(0f, transform.rotation.eulerAngles.y + 90f, 0f);
        _doorLockPos = transform.parent.GetChild(1);

        #region Fade Setting
        GetComponent<MeshRenderer>().material.SetFloat("_Mode", 2);
        GetComponent<MeshRenderer>().material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        GetComponent<MeshRenderer>().material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        GetComponent<MeshRenderer>().material.SetInt("_ZWrite", 0);
        GetComponent<MeshRenderer>().material.DisableKeyword("_ALPHATEST_ON");
        GetComponent<MeshRenderer>().material.EnableKeyword("_ALPHABLEND_ON");
        GetComponent<MeshRenderer>().material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        GetComponent<MeshRenderer>().material.renderQueue = 3000;
        #endregion
    }

    #region Open Door Effect
    Vector3 _rotateAngle;
    Transform _doorLockPos;

    Coroutine _openDoorCoroutine;
    public void CoOpenDoor(float time)
    { 
        _openDoorCoroutine = StartCoroutine(OpenDoor(time));
    }

    IEnumerator OpenDoor(float time)
    {
        Managers.Data.DoorActiveDic[_doorIndex_forActive] = false;
        Managers.Game.SaveGame();
        yield return new WaitForSeconds(1f);
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

    IEnumerator DoorLockOpenAnim()
    {
        Managers.Game.OnDirect = true;
        GameObject go = Managers.Resource.Instantiate("DoorLock", _doorLockPos);
        go.transform.localScale = new Vector3(go.transform.localScale.x, go.transform.localScale.y * _camera._scaleMultiplier, go.transform.localScale.z * _camera._scaleMultiplier);
        float time = go.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).length;
        yield return new WaitForSeconds(time);
        Managers.Game.OnDirect = false;
        Managers.Resource.Destroy(go);
    }


    public void CoDoorLockLockedAnim()
    {
        _doorLockLockedAnimCoroutine = StartCoroutine(DoorLockLockedAnim());
    }


    Coroutine _doorLockLockedAnimCoroutine;
    IEnumerator DoorLockLockedAnim()
    {
        GameObject go = Managers.Resource.Instantiate("DoorLock", _doorLockPos);
        go.GetComponent<Animator>().Play("DoorLockIsLocked");
        go.transform.localScale = new Vector3(go.transform.localScale.x, go.transform.localScale.y * _camera._scaleMultiplier, go.transform.localScale.z * _camera._scaleMultiplier);

        GameObject cross = Managers.Resource.Instantiate("FX_Cross", _doorLockPos);

        float time = go.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).length;
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
