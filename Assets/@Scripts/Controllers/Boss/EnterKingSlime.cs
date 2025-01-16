using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnterKingSlime : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            // 킹슬라임 전투

        }

        Managers.Resource.Destroy(gameObject);
    }
}
