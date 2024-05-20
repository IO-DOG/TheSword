using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectGamedev.Shaders
{
    public class UnlitTextHighlightEffectController : MonoBehaviour
    {
        [SerializeField]
        private float startHighlightValue = -0.5f;
        [SerializeField]
        private float endHighlightValue = 1.5f;
        [SerializeField]
        private float highlightSmoothness = 50;
        [SerializeField]
        private float highlightDurationSeconds = 2;
        [SerializeField]
        private float pauseBetweenHighlightsDurationSeconds = 3;
        [SerializeField]
        private Image image;

        private Material material;

        private const string shaderVarName = "_WaveController";

        private void Awake()
        {
            //Note: using the object's image and modifying its material's values will ensure all changes remain locked to the object.
            //This means changes are not globally applied to the material, but rather only to the CLONE of the material that this object is currently using.
            material = image.material;
        }

        private void Start()
        {
            StartCoroutine(TitleHighlightEffect());
        }

        private IEnumerator TitleHighlightEffect()
        {
            while (true)
            {
                StartCoroutine(
                    API.LerpShaderVariable<float>(
                        material, shaderVarName, startHighlightValue, endHighlightValue, 1.0f / highlightSmoothness, highlightDurationSeconds
                    )
                );

                yield return new WaitForSeconds(highlightDurationSeconds);
                yield return new WaitForSeconds(pauseBetweenHighlightsDurationSeconds);
            }
        }
    }
}
