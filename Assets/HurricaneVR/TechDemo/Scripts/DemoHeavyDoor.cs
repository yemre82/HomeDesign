using HurricaneVR.Framework.Components;
using HurricaneVR.Framework.Core.Utils;
using HurricaneVR.Framework.Shared;
using UnityEngine;

namespace HurricaneVR.TechDemo.Scripts
{
    public class DemoHeavyDoor : MonoBehaviour
    {
        public Rigidbody DoorRigidbody;
        public HVRRotationTracker ValveTracker;
        public HVRRotationLimiter Limiter;

        public float MaxAngle = 130f;

        public AudioClip[] SFX;
        public float SFXAngle = 10f;

        public float Angle;

        void Start()
        {

        }

        void FixedUpdate()
        {
            var angle = HVRUtilities.Remap(ValveTracker.Angle, Limiter.MinAngle, Limiter.MaxAngle, 0f, MaxAngle);
            DoorRigidbody.MoveRotation(Quaternion.Euler(0f, angle, 0f));

            if (SFX != null && SFX.Length > 0)
            {
                if (ValveTracker.Angle > Angle + SFXAngle || ValveTracker.Angle < Angle - SFXAngle)
                {
                    var index = Random.Range(0, SFX.Length);
                    var sfx = SFX[index];
                    Angle = ValveTracker.Angle;
                    SFXPlayer.Instance?.PlaySFX(sfx, transform.position);
                }
            }
        }
    }
}
