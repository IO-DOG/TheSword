using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossZoneController : MonoBehaviour
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
