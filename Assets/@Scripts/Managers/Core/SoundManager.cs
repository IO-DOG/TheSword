using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager
{
    private AudioSource[] _audioSources = new AudioSource[(int)Define.Sound.Max];
    private Dictionary<string, AudioClip> _audioClips = new Dictionary<string, AudioClip>();

    private GameObject _soundRoot = null;
    public int _totalEffectCount = 1;

    public void Init()
    {
        if (_soundRoot == null)
        {
            _soundRoot = GameObject.Find("@SoundRoot");
            if (_soundRoot == null)
            {
                _soundRoot = new GameObject { name = "@SoundRoot" };
                UnityEngine.Object.DontDestroyOnLoad(_soundRoot);

                string[] soundTypeNames = System.Enum.GetNames(typeof(Define.Sound));
                for (int count = 0; count < soundTypeNames.Length - 1; count++)
                {
                    GameObject go = new GameObject { name = soundTypeNames[count] };
                    _audioSources[count] = go.AddComponent<AudioSource>();
                    go.transform.parent = _soundRoot.transform;
                }

                _audioSources[(int)Define.Sound.Bgm].loop = true;
                _audioSources[(int)Define.Sound.SubBgm].loop = true;
            }
        }
    }

    public void Clear()
    {
        foreach (AudioSource audioSource in _audioSources)
            audioSource.Stop();
        _audioClips.Clear();
    }

    public void Play(Define.Sound type)
    {
        AudioSource audioSource = _audioSources[(int)type];
        audioSource.Play();
    }

    public void Play(Define.Sound type, string key, float pitch = 1.0f)
    {
        AudioSource audioSource = _audioSources[(int)type];

        // 없는 키는 조용히 넘어간다. 그대로 두면 아래에서 null 의 length 를 읽어 예외가 나고,
        // 소리를 부른 쪽(전투·연출 코루틴)이 통째로 죽는다. 곡이면 지금 곡을 끊지도 않는다.
        if (type == Define.Sound.Bgm)
        {
            LoadAudioClip(key, (audioClip) =>
            {
                if (audioClip == null)
                    return;
                if (audioSource.isPlaying)
                    audioSource.Stop();

               audioSource.clip = audioClip;
               // pitch 인자를 받아 놓고 쓰지 않았다. 챕터마다 조를 바꾸는 지금은
               // 이걸 안 되돌리면 타이틀 곡이 지하 4챕터의 낮은 조로 나온다.
               audioSource.pitch = pitch;
               //if (Managers.Game.BGMOn)
               audioSource.Play();
            });
        }
        else if (type == Define.Sound.SubBgm)
        {
            LoadAudioClip(key, (audioClip) =>
            {
                if (audioClip == null)
                    return;
                if (audioSource.isPlaying)
                    audioSource.Stop();

                audioSource.clip = audioClip;
                //if (Managers.Game.EffectSoundOn)
                audioSource.Play();
            });
        }
        else
        {
            LoadAudioClip(key, (audioClip) =>
            {
                if (audioClip == null)
                    return;
                // 배속 높이는 여기서 얹지 않는다 — 전투 시계가 내는 소리(타격·방어·치명)만 PlayByGameSpeed 로 얹는다.
                // 전투 중에 나는 소리 전부에 얹었더니 기본 2배속에서 전투 시작·보스 등장·게임 오버 소리까지 1.3배로 새됐다
                // (전투는 창이 열리기 전에 켜지고 게임 오버 소리가 난 뒤에 꺼진다).
                audioSource.pitch = pitch;

                //audioSource.volume = PlayerPrefs.GetFloat("CUREFFECTSOUND", 1) / (float)_totalEffectCount; // 오디오 수에 따를 볼륨 조절
                _totalEffectCount++;
                //if (Managers.Game.EffectSoundOn)
                float audioLength = audioClip.length;

                CoroutineManager.StartCoroutine(CoTotalEffectCountControl(audioLength)); // 오디오가 끝나면 오디오수 조절
                audioSource.PlayOneShot(audioClip);
                
            });
        }
    }

    public IEnumerator CoPlay(Define.Sound type, string key, float pitch = 1.0f, float time = 0f)
    {
        yield return new WaitForSeconds(time);

        Play(type, key, pitch);
    }

    public void Play(Define.Sound type, AudioClip audioClip, float pitch = 1.0f)
    {
        AudioSource audioSource = _audioSources[(int)type];

        if (type == Define.Sound.Bgm)
        {
            if (audioSource.isPlaying)
                audioSource.Stop();

            audioSource.clip = audioClip;
            //if (Managers.Game.BGMOn)
            audioSource.Play();
        }
        else if (type == Define.Sound.SubBgm)
        {
            if (audioSource.isPlaying)
                audioSource.Stop();

            audioSource.clip = audioClip;
            //if (Managers.Game.EffectSoundOn)
            audioSource.Play();
        }
        else if (audioClip != null)
        {
            audioSource.pitch = pitch;
            //if (Managers.Game.EffectSoundOn)
            audioSource.PlayOneShot(audioClip);
        }
    }

    /// <summary>
    /// 전투 시계가 내는 소리 — 전투 배속만큼 높아진다(1.3 까지, BattlePitch). 타격·방어(UI_PlayerCard·UI_MonsterCard)와
    /// 치명타(UI_BattlePopup.Crit)가 쓴다. 효과음 소스가 하나라 높이는 가장 나중에 튼 소리를 따른다.
    /// </summary>
    public void PlayByGameSpeed(Define.Sound type, string key, float pitch = 1f)
    {
        AudioSource audioSource = _audioSources[(int)type];

        if (type == Define.Sound.Bgm)
        {
            LoadAudioClip(key, (audioClip) =>
            {
                if (audioSource.isPlaying)
                    audioSource.Stop();

                audioSource.clip = audioClip;
                //if (Managers.Game.BGMOn)
                audioSource.Play();
            });
        }
        else if (type == Define.Sound.SubBgm)
        {
            LoadAudioClip(key, (audioClip) =>
            {
                if (audioSource.isPlaying)
                    audioSource.Stop();

                audioSource.clip = audioClip;
                //if (Managers.Game.EffectSoundOn)
                audioSource.Play();
            });
        }
        else
        {
            LoadAudioClip(key, (audioClip) =>
            {
                if (audioClip == null)
                    return;
                audioSource.pitch = pitch * BattlePitch;
                //if (Managers.Game.EffectSoundOn)
                audioSource.PlayOneShot(audioClip);
            });
        }
    }

    /// <summary>전투 배속에 맞춰 올리는 소리 높이의 끝.</summary>
    public const float MaxBattlePitch = 1.3f;

    // 전투 배속(UI_BattlePopup.Speed — 설정 1~4, 누르고 있으면 8, 봇 8~16)을 그대로 곱하면 두 옥타브 넘게 새된 소리가 났다.
    // 빨라진 느낌만 남기고 1.3 에서 멈춘다.
    static float BattlePitch => Mathf.Min(UI_BattlePopup.Speed, MaxBattlePitch);

    /// <summary>효과음 크기 — 설정의 효과음 슬라이더에 지금의 페이드(로딩 삽화가 0 으로 내린다)까지 얹힌 값이다.</summary>
    public float EffectVolume
    {
        get
        {
            AudioSource effect = _audioSources[(int)Define.Sound.Effect];
            return effect != null ? effect.volume : 1f;
        }
    }

    // 프리팹마다 AudioSource 원래 크기. 처음 볼 때 프리팹에서 한 번 읽는다 — 인스턴스에서 읽으면 풀에서 다시 꺼낸 것은
    // 이미 줄여 둔 값이라, 꺼낼 때마다 한 번 더 곱해져 점점 작아진다.
    readonly Dictionary<GameObject, float[]> _baseVolumes = new Dictionary<GameObject, float[]>();

    /// <summary>
    /// 프리팹이 스스로 트는 소리(PlayOnAwake — 타격·베기 이펙트, 열쇠·물약 줍기, 죽음 등 서른다섯 개)를 효과음 크기에 맞춘다.
    /// 그 소리들은 이 관리자를 거치지 않아서 슬라이더를 0 으로 내려도 제 크기로 울렸다. ResourceManager.Instantiate 가
    /// 만들 때(풀에서 꺼낼 때도) 부른다. 서드파티 프리팹을 고치지 않고 여기서 맞춘다 — 원래 크기 x 효과음 크기.
    /// </summary>
    public void FollowEffectVolume(GameObject prefab, GameObject instance)
    {
        if (prefab == null || instance == null)
            return;
        if (_baseVolumes.TryGetValue(prefab, out float[] bases) == false)
        {
            AudioSource[] original = prefab.GetComponentsInChildren<AudioSource>(true);
            bases = new float[original.Length];
            for (int i = 0; i < original.Length; i++)
                bases[i] = original[i].volume;
            _baseVolumes.Add(prefab, bases);
        }
        if (bases.Length == 0)
            return;

        // 복제본의 AudioSource 는 프리팹과 같은 순서로 나온다 (같은 계층을 같은 순서로 훑는다).
        AudioSource[] live = instance.GetComponentsInChildren<AudioSource>(true);
        float volume = EffectVolume;
        for (int i = 0; i < live.Length && i < bases.Length; i++)
            live[i].volume = bases[i] * volume;
    }

    public void Stop(Define.Sound type)
    {
        AudioSource audioSource = _audioSources[(int)type];
        audioSource.Stop();
    }

    public void PlayButtonClick()
    {
        Play(Define.Sound.Effect, "Click_CommonButton");
    }

    public void PlayPopupClose()
    {
        Play(Define.Sound.Effect, "PopupClose_Common");
    }
    private void LoadAudioClip(string key, Action<AudioClip> callback)
    {
        AudioClip audioClip = null;
        if (_audioClips.TryGetValue(key, out audioClip))
        {
            callback?.Invoke(audioClip);
            return;
        }

        audioClip = Managers.Resource.Load<AudioClip>(key);

        // 못 찾은 것은 담아 두지 않는다. null 을 담으면 나중에 올라와도 끝까지 무음이다.
        if (audioClip != null && !_audioClips.ContainsKey(key))
            _audioClips.Add(key, audioClip);

        callback?.Invoke(audioClip);

        //Managers.Resource.LoadAsync<AudioClip>(key, (audioClip) =>
        //{
        //    if (!_audioClips.ContainsKey(key))
        //        _audioClips.Add(key, audioClip);
        //    callback?.Invoke(audioClip);
        //});
    }

    public void SetVolume(float value)
    {
        _audioSources[(int)Define.Sound.Bgm].volume = value;
        _audioSources[(int)Define.Sound.Effect].volume = value;
    }

    public void SetBGMVolume(float value)
    {
        _audioSources[(int)Define.Sound.Bgm].volume = value;
    }

    public void SetEffectVolume(float value)
    {
        _audioSources[(int)Define.Sound.Effect].volume = value;
    }

    IEnumerator CoTotalEffectCountControl(float time)
    {
        WaitForSeconds delay = new WaitForSeconds(time);
        yield return delay;
        _totalEffectCount--;
        _totalEffectCount = Mathf.Max(1, _totalEffectCount);
    }

    // 페이드 타임이 총 합쳐서 time.
    // time/2 시간씩 fadeOut, fadeIn
    /// <summary>BGM 을 갈아 끼운다.
    ///
    /// pitch 는 챕터마다 같은 곡을 다른 조·속도로 쓰기 위한 것이다
    /// (ChapterTheme.BgmPitch). 곡이 챕터 00 것 하나뿐인 동안의 방편이라,
    /// 새 곡을 넣으면 pitch 를 1 로 두고 어드레서블 키만 늘리면 된다.
    /// </summary>
    public void FadeAndPlayBGM(string key, float time, float pitch = 1f)
    {
        AudioSource audioSource = _audioSources[(int)Define.Sound.Bgm];
        // 되돌아갈 소리 크기는 설정값이다. 지금 크기를 잡으면, 로딩 삽화가 막 0 으로 내려 둔
        // 챕터 첫 층에서 새 곡이 0 으로 페이드인되어 그 챕터 내내 무음이었다.
        float volume = ConfiguredBgmVolume;
        LoadAudioClip(key, (audioClip) =>
        {
            if (audioClip == null)
                return;

            // 이미 그 곡이 그 조로 돌고 있으면 굳이 끊지 않는다 — 층을 옮길 때마다
            // 같은 곡이 페이드로 끊겼다 이어지면 그게 더 거슬린다.
            if (audioSource.isPlaying && audioSource.clip == audioClip
                && Mathf.Approximately(audioSource.pitch, pitch))
                return;

            if (audioSource.isPlaying == false)
            {
                // 꺼져 있으면 예전에는 아무 일도 하지 않았다 — 그대로 무음이었다.
                audioSource.clip = audioClip;
                audioSource.pitch = pitch;
                audioSource.volume = 0f;
                audioSource.Play();
                audioSource.DOFade(volume, time / 2f);
                return;
            }

            audioSource.DOFade(0f, time / 2f).OnComplete(() =>
            {
                audioSource.Stop();
                audioSource.clip = audioClip;
                audioSource.pitch = pitch;
                audioSource.Play();
                audioSource.DOFade(volume, time / 2f);
            });
        });
    }

    public void FadeAndStopBGM(float time)
    {
        AudioSource audioSource = _audioSources[(int)Define.Sound.Bgm];

        if (audioSource.isPlaying)
        {
            audioSource.DOFade(0f, time).OnComplete(() =>
            {
                audioSource.Stop();
            });
        }
    }

    public AudioSource GetAudioSource(Define.Sound type)
    {
        return _audioSources[(int)type];
    }

    /// <summary>설정 화면이 정한 곡 소리 크기 (전체 x 배경음).</summary>
    public static float ConfiguredBgmVolume =>
        PlayerPrefs.GetFloat("CURBGMSOUND", 1) * PlayerPrefs.GetFloat("SAVESOUND", 1);

    public void FadeInBGM(float time)
    {
        CoroutineManager.StartCoroutine(CoFadeInBGM(time));
    }
    IEnumerator CoFadeInBGM(float time)
    {
        yield return null;
        float timer = 0f;
        while (timer < time)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / time);
            //// 소리켜기
            Managers.Sound.SetBGMVolume(Mathf.Min(t, ConfiguredBgmVolume));
            Managers.Sound.SetEffectVolume(Mathf.Min(t, PlayerPrefs.GetFloat("CUREFFECTSOUND", 1) * PlayerPrefs.GetFloat("SAVESOUND", 1)));

            yield return null;
        }
        Managers.Sound.SetBGMVolume(ConfiguredBgmVolume);
        Managers.Sound.SetEffectVolume(PlayerPrefs.GetFloat("CUREFFECTSOUND", 1) * PlayerPrefs.GetFloat("SAVESOUND", 1));
    }
}
