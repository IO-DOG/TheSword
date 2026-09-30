using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossEventTriggerController : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            // 등장 연출을 건 보스가 없는 층(이미 잡았다)에서는 비어 있다.
            Managers.Directing.BossOnAppearAction?.Invoke();
        }

        Managers.Resource.Destroy(gameObject);
    }
}
