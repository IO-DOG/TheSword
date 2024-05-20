using System.Collections;
using UnityEngine;

namespace ProjectGamedev.Shaders
{
    /// <summary>
    /// Additional methods used by the pack's scripts are stored here.
    /// </summary>
    public abstract class API
    {
        /// <summary>
        /// Method lerps a material property variable of type float from a minimal value to a maximal value for a passed duration in seconds.
        /// Parameter "step" determines the smoothness of the effect – higher values result in a smoother transition.
        /// </summary>
        public static IEnumerator LerpShaderVariable<Float>(Material material, string varName, float initVal, float endVal, float step, float seconds)
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
        }
    }
}