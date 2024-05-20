using System.Collections;
using UnityEngine;

namespace ProjectGamedev.Shaders
{
    /// <summary>
    /// The following script demonstrates how the directional dissolve effect can be dynamically controlled via a script.
    /// </summary>
    public class DirectionalDissolveController : MonoBehaviour
    {
        [SerializeField]
        private float startDissolveAmount = -1.0f;
        [SerializeField]
        private float endDissolveAmount = 1.0f;
        [SerializeField]
        private float effectLengthSeconds = 1.0f;
        [SerializeField]
        private float effectSmoothness = 150;
        [SerializeField]
        [Tooltip("Assign every sprite renderer you wish to apply the effect to. Useful for multi-layered objects (e.g. enemies) that have a separate sprite for each body part.")]
        private SpriteRenderer[] spriteRenderers;

        private Material[] materials;

        private const string shaderVariableName = "_DissolveEffectController";

        private void Awake()
        {
            //Note: using the object's sprite renderer and modifying its material's values will ensure all changes remain locked to the object.
            //This means changes are not globally applied to the material, but rather only to the CLONE of the material that this object is currently using.
            materials = new Material[spriteRenderers.Length];
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = spriteRenderers[i].material;
            }
        }

        private void Start()
        {
            foreach (Material material in materials)
            {
                StartCoroutine(
                    CustomLerpShaderVariable<float>(
                        material, shaderVariableName, startDissolveAmount, endDissolveAmount, 1.0f / effectSmoothness, effectLengthSeconds
                    )
                );
            }
        }

        /// <summary>
        /// Method lerps a material property variable of type float from a minimal value to a maximal value for a passed duration in seconds.
        /// Parameter "step" determines the smoothness of the effect – higher values result in a smoother transition.
        /// </summary>
        public IEnumerator CustomLerpShaderVariable<Float>(Material material, string varName, float initVal, float endVal, float step, float seconds)
        {
            float sign = Mathf.Sign(endVal - initVal);
            float stepsCount = Mathf.Abs(endVal - initVal) / step;

            //Progressively increase/decrease the value of the defined variable.
            for (int i = 0; i < stepsCount; i++)
            {
                material.SetFloat(varName, initVal + step * sign * (i + 1)); //controls if negative or positive           

                //For whatever reason, WaitForSeconds(Realtime) encounters issues with very small numbers, so only update it once every 5 times
                if (i % 5 == 0)
                    yield return new WaitForSeconds((seconds / stepsCount) * 5);
            }

            //In the end, apply the end value in case it wasn't fully reached.
            material.SetFloat(varName, endVal);

            //Pause for a second before initiating the "return" effect.
            yield return new WaitForSeconds(1.0f);

            if (Mathf.Abs(initVal - startDissolveAmount) <= 0.1f)
            {
                //We were increasing the shader variable until now. Time for decreasing it.
                StartCoroutine(
                    CustomLerpShaderVariable<float>(
                        material, shaderVariableName, endDissolveAmount, startDissolveAmount, 1.0f / effectSmoothness, effectLengthSeconds
                    )
                );
            }
            else
            {
                //We were decreasing the shader variable until now. Time for increasing it.
                StartCoroutine(
                    CustomLerpShaderVariable<float>(
                        material, shaderVariableName, startDissolveAmount, endDissolveAmount, 1.0f / effectSmoothness, effectLengthSeconds
                    )
                );
            }
        }
    }
}
