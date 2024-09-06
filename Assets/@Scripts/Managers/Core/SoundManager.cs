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
                audioSource.volume = 1 / (float)_totalEffectCount; // 오디오 수에 따를 볼륨 조절
                _totalEffectCount++;
                //if (Managers.Game.EffectSoundOn)
                float audioLength = audioClip.length;

                CoroutineManager.StartCoroutine(CoTotalEffectCountControl(audioLength)); // 오디오가 끝나면 오디오수 조절
                audioSource.PlayOneShot(audioClip);
                
            });
        }
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
}
