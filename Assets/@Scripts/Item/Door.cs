using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Door : MonoBehaviour
{
    public int _keyIndex = 0;
    CameraController _camera;

    private void Start()
    {
        _camera = Camera.main.GetComponentInChildren<CameraController>();
        _rotateAngle = new Vector3(0f, transform.rotation.eulerAngles.y + 90f, 0f);
        _doorLockPos = transform.parent.GetChild(1);
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
        float elapsedTime = 0.0f;
        Quaternion targetRotation = Quaternion.Euler(_rotateAngle);

        while (elapsedTime < time)
        {
            transform.rotation = Quaternion.Euler(Vector3.Lerp(transform.rotation.eulerAngles, targetRotation.eulerAngles, elapsedTime / time));

            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }

    Coroutine _doorLockAnimCoroutine;
    public void CoDoorLockAnim()
    {
        _doorLockAnimCoroutine = StartCoroutine(DoorLockAnim());
    }

    IEnumerator DoorLockAnim()
    {
        GameObject go = Managers.Resource.Instantiate("DoorLock", _doorLockPos);
        go.transform.localScale = new Vector3(go.transform.localScale.x, go.transform.localScale.y * _camera.scaleMultiplier, go.transform.localScale.z * _camera.scaleMultiplier);
        float time = go.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).length;
        yield return new WaitForSeconds(time);
        Managers.Resource.Destroy(go);
    }
    #endregion Effect
}
