using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.VFX;

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
        Managers.Game.OnPortalAction -= SetDustPosition;
        Managers.Game.OnPortalAction += SetDustPosition;
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

    void SetDustPosition()
    {
        if (Managers.Game.Player.gameObject == null)
            return;
        if (dust == null)
            return;
        dust.transform.localPosition = Managers.Game.Player.transform.position;
    }

    void SetLight()
    {
        Volume postProcessingVolume = Managers.Game.MainCamera.GetComponent<Volume>();
        Vignette vignette;
        ColorAdjustments colorAdjustments;

        if (postProcessingVolume.profile.TryGet<Vignette>(out vignette))
        {
            vignette.intensity.Override(Mathf.Clamp(0.3f, 0, 1));
        }

        // light
        if (Managers.Game.PlayerData.CurStageid == 0 || Managers.Game.PlayerData.CurStageid == Managers.Game.BossRoomId)
        {
            Managers.Game.DirectionalLight.color = new Color(255/255f, 244/255f, 214/255f);
            Managers.Game.DirectionalLight.intensity = 1.5f;
        }
        else if(Managers.Game.PlayerData.CurStageid == 2)
        {
            // 마검 뽑기 전
            if(PlayerPrefs.GetInt("ISMEETSWORD") == 0)
            {
                // 마검방 
                Managers.Game.DirectionalLight.color = new Color(213 / 255f, 199 / 255f, 255 / 255f);
                Managers.Game.DirectionalLight.intensity = 0.57f;

                if (postProcessingVolume.profile.TryGet<ColorAdjustments>(out colorAdjustments))
                {
                    colorAdjustments.colorFilter.Override(new Color(205 / 255f, 153 / 255f, 255 / 255f));
                }
            }
            else
            {
                Managers.Game.DirectionalLight.color = new Color(255 / 255f, 244 / 255f, 214 / 255f);
                Managers.Game.DirectionalLight.intensity = 1.5f;

                GameObject fireflies = GameObject.Find("MagicalSwordRoomFireflies");
                if(fireflies != null)
                {
                    fireflies.SetActive(false);
                }

                GameObject godray = GameObject.Find("MagicalSwordRoomGodray");
                if (godray != null)
                {
                    godray.GetComponent<SpriteRenderer>().material = Managers.Resource.Load<Material>("Godray3");
                }

                if (postProcessingVolume.profile.TryGet<ColorAdjustments>(out colorAdjustments))
                {
                    colorAdjustments.colorFilter.Override(new Color(255 / 255f, 231 / 255f, 206 / 255f));
                }
            }
        }
        else
        {
            Managers.Game.DirectionalLight.color = new Color(192 / 255f, 189 / 255f, 179 / 255f);
            Managers.Game.DirectionalLight.intensity = 1.5f;

            if (postProcessingVolume.profile.TryGet<ColorAdjustments>(out colorAdjustments))
            {
                colorAdjustments.colorFilter.Override(new Color(255 / 255f, 231 / 255f, 206 / 255f));
            }
        }

        // fog
        if (Managers.Game.PlayerData.CurStageid == Managers.Game.BossRoomId)
        {
            if (fog !=null && fog.GetComponent<VisualEffect>().HasVector4("FogSeconderyColor"))
            {
                Vector4 color = new Color(12 / 255f, 166 / 255f, 18 / 255f);
                fog.GetComponent<VisualEffect>().SetVector4("FogSeconderyColor", color);
            }
        }
        else
        {
            if (fog != null && fog.GetComponent<VisualEffect>().HasVector4("FogSeconderyColor"))
            {
                Vector4 color = new Color(1f, 1f, 1f);
                fog.GetComponent<VisualEffect>().SetVector4("FogSeconderyColor", color);
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
