using System.Collections;
using UnityEngine;

namespace ProjectGamedev.Shaders
{
    /// <summary>
    /// The following script demonstrates how the vertex displacement effect can be dynamically controlled via a script.
    /// </summary>
    public class VertexDisplacementController : MonoBehaviour
    {
        [SerializeField]
        private float startVertexDisplacementAmount = 0.0f;
        [SerializeField]
        private float endVertexDisplacementAmount = 400.0f;
        [SerializeField]
        private float effectLengthSeconds = 1.0f;
        [SerializeField]
        private float effectSmoothness = 5;
        [SerializeField]
        [Tooltip("Assign every sprite renderer you wish to apply the effect to. Useful for multi-layered objects (e.g. enemies) that have a separate sprite for each body part.")]
        private SpriteRenderer[] spriteRenderers;

        private bool activeEffect = false;
        private Material[] materials;

        private const string shaderVariableName = "_VertexDisplacementAmount";

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

        public void ProgressiveVertexDisplacementButton_OnClick()
        {
            if (!activeEffect)
            {
                activeEffect = true;
                StartCoroutine(EffectController());
            }
        }

        private IEnumerator EffectController()
        {
            foreach (Material material in materials)
            {
                StartCoroutine(
                    API.LerpShaderVariable<float>(
                        material, shaderVariableName, startVertexDisplacementAmount, endVertexDisplacementAmount, 1.0f / effectSmoothness, effectLengthSeconds
                    )
                );
            }

            //Wait until the first lerp is finished.
            yield return new WaitForSeconds(effectLengthSeconds);

            //Revert to the old value.
            foreach (Material material in materials)
            {
                StartCoroutine(
                    API.LerpShaderVariable<float>(
                        material, shaderVariableName, endVertexDisplacementAmount, startVertexDisplacementAmount, 1.0f / effectSmoothness, effectLengthSeconds
                    )
                );
            }

            //Wait until the second lerp is finished.
            yield return new WaitForSeconds(effectLengthSeconds);

            activeEffect = false;
        }
    }
}