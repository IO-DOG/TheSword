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

        if (type == Define.Sound.Bgm)
        {
            LoadAudioClip(key, (audioClip) =>
            {
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
                audioSource.pitch = Managers.Game.GameSpeed;

                //audioSource.pitch = pitch;
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
        else
        {
            audioSource.pitch = pitch;
            //if (Managers.Game.EffectSoundOn)
            audioSource.PlayOneShot(audioClip);
        }
    }

    /// <summary>
    /// 게임 배속에 따라서 pitch가 달라짐
    /// </summary>
    /// <param name="type"></param>
    /// <param name="key"></param>
    public void PlayByGameSpeed(Define.Sound type, string key)
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
                audioSource.pitch = Managers.Game.GameSpeed;
                //if (Managers.Game.EffectSoundOn)
                audioSource.PlayOneShot(audioClip);
            });
        }
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

        if (!_audioClips.ContainsKey(key))
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
        float volume = audioSource.volume;
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
            Managers.Sound.SetBGMVolume(Mathf.Min(t, PlayerPrefs.GetFloat("CURBGMSOUND", 1) * PlayerPrefs.GetFloat("SAVESOUND", 1)));
            Managers.Sound.SetEffectVolume(Mathf.Min(t, PlayerPrefs.GetFloat("CUREFFECTSOUND", 1) * PlayerPrefs.GetFloat("SAVESOUND", 1)));

            yield return null;
        }
        Managers.Sound.SetBGMVolume(PlayerPrefs.GetFloat("CURBGMSOUND", 1) * PlayerPrefs.GetFloat("SAVESOUND", 1));
        Managers.Sound.SetEffectVolume(PlayerPrefs.GetFloat("CUREFFECTSOUND", 1) * PlayerPrefs.GetFloat("SAVESOUND", 1));
    }
}
