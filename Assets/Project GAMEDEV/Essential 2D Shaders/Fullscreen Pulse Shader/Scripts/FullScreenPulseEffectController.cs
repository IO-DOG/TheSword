using System.Collections;
using UnityEngine;

namespace ProjectGamedev.Shaders
{
    public class FullScreenPulseEffectController : MonoBehaviour
    {
        [SerializeField]
        private bool fadeOnScreenPulse = false;
        [SerializeField]
        private Material fullScreenPulseEffectMaterial;
        [SerializeField]
        private Camera mainCamera;

        private bool activeEffect = false;

        public void PerformFullscreenPulse()
        {
            if (!activeEffect)
                StartCoroutine(LaunchFullScreenPulseEffect());
        }

        private IEnumerator LaunchFullScreenPulseEffect()
        {
            activeEffect = true;

            //In order to preserve the circular form of the pulse effect, we need the screen aspect ratio, else the effect will be distored.
            //This is automatically calculated here.
            fullScreenPulseEffectMaterial.SetFloat("_ScreenSizeRatio", mainCamera.aspect);

            fullScreenPulseEffectMaterial.SetInt("enable", 1);

            float length = fullScreenPulseEffectMaterial.GetFloat("_PulseLength");

            //Pulse effect
            StartCoroutine(
                API.LerpShaderVariable<float>(
                    fullScreenPulseEffectMaterial, "_Controller", 0.0f, 1.0f, 0.001f, length
                )
            );

            if (fadeOnScreenPulse) //Only if enabled by the user!
            {
                //Fade in screen effect
                StartCoroutine(
                    API.LerpShaderVariable<float>(
                        fullScreenPulseEffectMaterial, "_FadeController", 0.0f, 1.2f, 0.001f, length
                    )
                );
            }

            //Wait for the previous effect(s) to finish.
            yield return new WaitForSeconds(length + 0.2f); //Add a little buffer to the wait time

            if (fadeOnScreenPulse) //Only if enabled by the user!
            {
                //Fade out screen effect
                StartCoroutine(
                    API.LerpShaderVariable<float>(
                        fullScreenPulseEffectMaterial, "_FadeController", 1.2f, 0.0f, 0.001f, length
                    )
                );

                yield return new WaitForSeconds(length + 0.2f); //Add a little buffer to the wait time
            }

            activeEffect = false;
        }
    }
}