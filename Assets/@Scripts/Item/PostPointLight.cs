using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;

public class PostPointLight : MonoBehaviour
{
    Light _pointLight;
    void Start()
    {
        _pointLight = gameObject.GetComponentInChildren<Light>();
        StartCoroutine(RandomLight());
    }

    IEnumerator BlackOutLight()
    {
        float time = Random.Range(10f, 16f);
        WaitForSeconds waitForSeconds = new WaitForSeconds(time);
        WaitForSeconds waitForSeconds1 = new WaitForSeconds(0.3f);
        WaitForSeconds waitForSeconds2 = new WaitForSeconds(0.1f);
        while (true)
        {
            yield return waitForSeconds;
            _pointLight.gameObject.SetActive(false);

            yield return waitForSeconds1;
            _pointLight.gameObject.SetActive(true);

            yield return waitForSeconds2;
            _pointLight.gameObject.SetActive(false);

            yield return waitForSeconds2;
            _pointLight.gameObject.SetActive(true);
        }

    }

    IEnumerator RandomLight()
    {
        while (true)
        {
            float targetIntensity = Random.Range(0.8f, 1f);
            float changeDuration = Random.Range(0.5f, 1f);
            float timer = 0f;

            while (timer < changeDuration)
            {
                timer += Time.deltaTime;

                _pointLight.GetComponent<Light>().intensity = Mathf.Lerp(_pointLight.intensity, targetIntensity, timer / changeDuration);

                yield return null;
            }

            yield return null;
        }
    }
}
