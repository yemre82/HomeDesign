using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;

namespace HurricaneVR.Samples
{
    [RequireComponent(typeof(SpotLight))]
    public class ExampleLight : MonoBehaviour
    {
        public Light Light;
        public float MaxIntensity;

        void Awake()
        {
            Light = GetComponent<Light>();
        }

        public void OnAngledChanged(float angle, float delta, float percent)
        {
            Light.intensity = percent * MaxIntensity;
        }
    }
}
