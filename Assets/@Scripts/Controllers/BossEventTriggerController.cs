using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossEventTriggerController : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            Managers.Directing.BossOnAppearAction.Invoke();
        }

        Managers.Resource.Destroy(gameObject);
    }
}
