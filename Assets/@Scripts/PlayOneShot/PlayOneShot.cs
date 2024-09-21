using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayOneShot : MonoBehaviour
{
    public AudioClip _audioClip;
    GameObject MainText;
    GameObject PessAnyKeyText;
    // Start is called before the first frame update
    void Start()
    {
        MainText = GameObject.Find("MainTitle_Text");
        PessAnyKeyText = GameObject.Find("PessAnyKeyText");
        MainText.SetActive(false);
        PessAnyKeyText.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void PlaySwordImpactSound()
    {
        MainText.SetActive(true);
        PessAnyKeyText.SetActive(true);

        AudioSource audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.PlayOneShot(_audioClip);
    }
}
