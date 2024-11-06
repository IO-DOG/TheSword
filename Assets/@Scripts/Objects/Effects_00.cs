using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Effects_00 : MonoBehaviour
{
    GameObject fog;
    GameObject fallingLeavesPrefab;
    List<GameObject> fallingLeaves = new List<GameObject>();
    int leavesPoolSize = 7;


    void Start()
    {
        fog = Managers.Resource.Instantiate("Fog", transform);
        for (int i = 0; i < leavesPoolSize; i++)
        {
            fallingLeavesPrefab = Managers.Resource.Instantiate("FallingLeaves", transform);
            fallingLeavesPrefab.SetActive(false);
            fallingLeaves.Add(fallingLeavesPrefab);
        }
        SetFogPosition();
        Managers.Game.OnPortalAction -= SetFogPosition;
        Managers.Game.OnPortalAction += SetFogPosition;
        

        StartCoroutine(FallingLeaves());
    }

    GameObject GetPooledObejct()
    {
        foreach (var obj in fallingLeaves)
        {
            if(!obj.activeInHierarchy)
                return obj;
        }

        return null;
    }

    IEnumerator FallingLeaves()
    {
        while(true)
        {
            float spawnInterval = Random.Range(0, 5);
            yield return new WaitForSeconds(spawnInterval);

            GameObject obj = GetPooledObejct();
            if(obj != null)
            {
                obj.SetActive(true);
                obj.transform.localPosition = GetSpawnPosition();
            }
        }
    }

    void SetFogPosition()
    {
        fog.transform.localPosition = Managers.Game.Player.transform.position;
    }

    Vector3 GetSpawnPosition()
    {
        return new Vector3
            (
                Random.Range(CameraController._minBounds.x,CameraController._maxBounds.x),
                0.5f,
                Random.Range(CameraController._minBounds.z, CameraController._maxBounds.z)
            );
    }
}
