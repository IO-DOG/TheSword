using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossZoneController : MonoBehaviour
{
    public enum BossType
    {
        None,
        KingSlime,
    }

    public BossType _bossType = BossType.None;

    private void Start()
    {
        Managers.Directing.BossOnAppearAction = null;

        switch (_bossType)
        {
            case BossType.KingSlime:
                Managers.Directing.BossOnAppearAction += Managers.Directing.Events.MeetKingSlime;
                break;
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            Managers.Directing.BossOnAppearAction.Invoke();
        }

        Managers.Resource.Destroy(gameObject);
    }
}
