using System.Collections;
using System.Collections.Generic;
using UnityEditor.Build.Pipeline;
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

        Managers.Game.OnPortalAction -= SetFogPosition;
        Managers.Game.OnPortalAction += SetFogPosition;

        SetFogPosition();
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
        while (true)
        {
            float spawnInterval = Random.Range(2, 5);
            yield return new WaitForSeconds(spawnInterval);

            GameObject obj = GetPooledObejct();
            if(obj != null)
            {
                obj.SetActive(true);
                obj.transform.position = GetSpawnPosition();
            }
        }
    }

    void SetFogPosition()
    {
        fog.transform.localPosition = Managers.Game.Player.transform.position;
    }

    Vector3 GetSpawnPosition()
    {
        Bounds bounds = Managers.Game.MainCamera.GetComponentInChildren<CameraController>()._bg.bounds;
        Debug.Log(bounds.min + " : " + bounds.max);
        return new Vector3
            (
                Random.Range(bounds.min.x, bounds.max.x),
                0.5f,
                Random.Range(bounds.min.z, bounds.max.z)
            );
    }
}
