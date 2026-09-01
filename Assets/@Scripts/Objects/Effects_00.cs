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

    // 지금 층 챕터의 공기 설정. 배속·간격이 여기서 온다.
    Vector2 airInterval = new Vector2(2f, 5f);
    // 파티클의 원래 크기. 배율을 매번 곱하면 층을 옮길수록 커지거나 사라진다.
    readonly Dictionary<ParticleSystem, float> baseSize = new Dictionary<ParticleSystem, float>();

    /// <summary>지금 층의 챕터 번호(0~4). CurChapter 는 "00"~"04" 문자열이다.</summary>
    static int ChapterIndex()
    {
        int ch;
        if (int.TryParse(Managers.Game.CurChapter, out ch))
            return ch;
        return 0;
    }

    /// <summary>손수 만든 1~4층인가. 그 층 조명은 인트로 연출에 맞춰져 있어 건드리지 않는다.</summary>
    static bool HandAuthored()
    {
        return Managers.Game.PlayerData.CurStageid < 4;
    }


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
        Managers.Game.OnPortalAction -= ApplyAir;
        Managers.Game.OnPortalAction += ApplyAir;
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
            float spawnInterval = Random.Range(airInterval.x, airInterval.y);
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

    /// <summary>챕터마다 공중에 떠도는 것을 바꾼다.
    ///
    /// 벽 아트가 챕터 00 세트뿐이라, 방에 들어섰을 때 "여기는 다른 곳이다" 를
    /// 가장 먼저 말해 주는 것이 이 알갱이들이다. 잎이 100층 내내 떨어지면
    /// 어느 챕터든 같은 지하 묘소로 읽힌다. 같은 파티클을 색·중력·크기·빈도만
    /// 바꿔서 물방울(수로) · 불티(용광로) · 눈(심층) · 티끌(균열)로 쓴다.
    /// 불티와 티끌은 중력이 음수라 떠오른다.</summary>
    void ApplyAir()
    {
        ChapterTheme.Theme t = ChapterTheme.Get(ChapterIndex());
        airInterval = t.AirInterval;

        foreach (GameObject leaf in fallingLeaves)
            Dress(leaf, t.AirColor, t.AirGravity, t.AirSize, t.AirSpeed);

        // 먼지는 카메라 주변을 떠다니는 것이라 색만 맞춘다.
        Dress(dust, t.DustColor, float.NaN, 1f, 1f);

        if (fog == null)
            return;
        VisualEffect vfx = fog.GetComponent<VisualEffect>();
        if (vfx != null && vfx.HasVector4("FogBaseColor"))
            vfx.SetVector4("FogBaseColor", t.FogBase);
    }

    /// <summary>파티클 하나를 챕터 색·중력·크기·배속으로 갈아입힌다.</summary>
    void Dress(GameObject go, Color color, float gravity, float size, float speed)
    {
        if (go == null)
            return;

        foreach (ParticleSystem ps in go.GetComponentsInChildren<ParticleSystem>(true))
        {
            ParticleSystem.MainModule main = ps.main;

            float basis;
            if (baseSize.TryGetValue(ps, out basis) == false)
            {
                basis = main.startSizeMultiplier;
                baseSize[ps] = basis;
            }

            main.startColor = color;
            main.startSizeMultiplier = basis * size;
            main.simulationSpeed = speed;
            if (float.IsNaN(gravity) == false)
                main.gravityModifier = gravity;
        }
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
        else if (HandAuthored())
        {
            Managers.Game.DirectionalLight.color = new Color(192 / 255f, 189 / 255f, 179 / 255f);
            Managers.Game.DirectionalLight.intensity = 1.5f;

            if (postProcessingVolume.profile.TryGet<ColorAdjustments>(out colorAdjustments))
            {
                colorAdjustments.colorFilter.Override(new Color(255 / 255f, 231 / 255f, 206 / 255f));
            }
        }
        else
        {
            // 생성 층은 ChapterTheme 이 정한 시간대를 그대로 쓴다.
            //
            // 예전에는 여기서 색과 세기를 하드코딩해 덮어썼다. GenerateMap 이
            // ChapterTheme.Apply 를 부른 직후에 이 Start -> OnPortalAction 이
            // 돌기 때문에, 챕터 1~4 가 전부 챕터 00 의 조명으로 나왔다.
            // 안개만 바뀌고 해의 색·각도가 그대로였으니 "필터를 씌운 것" 으로
            // 보인 것이 당연했다.
            ChapterTheme.Theme t = ChapterTheme.Get(ChapterIndex());
            ChapterTheme.Apply(ChapterIndex(), Managers.Game.DirectionalLight);

            if (postProcessingVolume.profile.TryGet<ColorAdjustments>(out colorAdjustments))
            {
                colorAdjustments.colorFilter.Override(t.ColorFilter);
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
                Vector4 color = ChapterTheme.Get(ChapterIndex()).FogSecondary;
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
