using System.Collections;
using System.Collections.Generic;
using UnityEditor.Build.Pipeline;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class Effects_00 : MonoBehaviour
{
    GameObject fog;
    GameObject fallingLeavesPrefab;
    GameObject dust;
    List<GameObject> fallingLeaves = new List<GameObject>();
    int leavesPoolSize = 7;


    void Start()
    {
        fog = Managers.Resource.Instantiate("Fog", transform);
        dust = Managers.Resource.Instantiate("DustFloaty", transform);
        for (int i = 0; i < leavesPoolSize; i++)
        {
            fallingLeavesPrefab = Managers.Resource.Instantiate("FallingLeaves", transform);
            fallingLeavesPrefab.SetActive(false);
            fallingLeaves.Add(fallingLeavesPrefab);
        }

        Managers.Game.OnPortalAction -= SetFogPosition;
        Managers.Game.OnPortalAction += SetFogPosition;
        Managers.Game.OnPortalAction -= SetLight;
        Managers.Game.OnPortalAction += SetLight;
        //Managers.Game.OnPortalAction -= SetDustPosition;
        //Managers.Game.OnPortalAction += SetDustPosition;
        Managers.Game.OnPortalAction.Invoke();

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
        if (Managers.Game.Player.gameObject == null)
            return;
        if (fog == null)
            return;
        fog.transform.localPosition = Managers.Game.Player.transform.position;
        if (Managers.Game.PlayerData.CurStageid == 0)
        {
            fog.SetActive(false);
        }
        else
        {
            fog.SetActive(true);
        }

    }

    //void SetDustPosition()
    //{
    //    dust.transform.localPosition = Managers.Game.Player.transform.position;
    //}

    void SetLight()
    {
        if (Managers.Game.PlayerData.CurStageid != 2)
        {
            Volume postProcessingVolume = Managers.Game.MainCamera.GetComponent<Volume>();
            ColorAdjustments colorAdjustments;
            if (postProcessingVolume.profile.TryGet<ColorAdjustments>(out colorAdjustments))
            {
                colorAdjustments.active = false;
            }
        }

        if (Managers.Game.PlayerData.CurStageid == 0)
        {
            Managers.Game.DirectionalLight.intensity = 1.5f;
            Managers.Game.DirectionalLight.color = new Color(255/255f, 244/255f, 214/255f);
        }
        else if(Managers.Game.PlayerData.CurStageid == 1)
        {
            Managers.Game.DirectionalLight.intensity = 1f;
            Managers.Game.DirectionalLight.color = new Color(239/255f, 236/255f, 206/255f);
        }
        else if (Managers.Game.PlayerData.CurStageid == 2)
        {
            Volume postProcessingVolume = Managers.Game.MainCamera.GetComponent<Volume>();
            ColorAdjustments colorAdjustments;
            if (postProcessingVolume.profile.TryGet<ColorAdjustments>(out colorAdjustments))
            {
                colorAdjustments.active = true; // 효과 활성화
                colorAdjustments.hueShift.Override(Mathf.Clamp(116, -180, 180));
            }
        }
    }

    Vector3 GetSpawnPosition()
    {
        Bounds bounds = Managers.Game.MainCamera.GetComponentInChildren<CameraController>()._bg.bounds;
        return new Vector3
            (
                Random.Range(bounds.min.x, bounds.max.x),
                0.5f,
                Random.Range(bounds.min.z, bounds.max.z)
            );
    }
}
