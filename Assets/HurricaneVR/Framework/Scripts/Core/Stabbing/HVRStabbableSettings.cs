using HurricaneVR.Framework.Shared.HandPoser;
using UnityEngine;

namespace HurricaneVR.Framework.Core.Stabbing
{
    [CreateAssetMenu(menuName = "HurricaneVR/Stabbable Settings", fileName = "StabSettings")]
    public class HVRStabbableSettings : ScriptableObject
    {
        [Header("Settings")]
        [Tooltip("If true depth and unstab checks will use raycasts on the stabbable colliders")]
        public bool CanBeImpaled = false;

        [Tooltip("Required velocity to initiate the stab")]
        public float RequiredVelocity = 1f;

        [Header("Friction")]
        [Tooltip("Joint Friction In effect until OuterShellDepth is breached by the stabber")]
        public float OuterShellDamper = 5000;
        [Tooltip("How thick is the outer shell (like a skull or box)")]
        public float OuterShellThickness = .025f; // default 1 inch?

        [Tooltip("Base Damper Scaled By InnerDamperCurve")]
        public float Damper = 1000;

        [Tooltip("If true uses the damper curve against depth / blade length, otherwise flat damper will be used")]
        public bool UseDamperCurve = true;
        [Tooltip("Joint Damper curve that takes over once the outer shell is breached, defaults to 1 if not provided")]
        public AnimationCurve InnerDamperCurve;


        [Header("Limits")]
        public bool LimitStabDepth;
        [DrawIf("LimitStabDepth", true)]
        public float StabDepthLimit;

        [Header("Locking")]
        public bool CanLock = true;
        public float LockVelocity = .1f;
        public float LockTime = .25f;
        public float UnlockForce = 300;

        [Header("Full Stab Settings")]
        [Tooltip("Can FullStabbed event be executed many times")]
        public bool ManyFullStabs = true;
        [Tooltip("Distance Required to execute the FullStabbed event")]
        
        public float FullStabDepth = .15f;
        [Tooltip("Distance required to reset full stab if allowed")]
        public float FullStabResetDepth = 0.05f;

        public float LockVelocitySquared => LockVelocity * LockVelocity;
        public float UnlockForceSquared => UnlockForce * UnlockForce;

        public void CheckCurve()
        {
            if (InnerDamperCurve == null)
            {
                InnerDamperCurve = new AnimationCurve();
            }

            if (InnerDamperCurve.keys.Length == 0)
            {
                InnerDamperCurve.AddKey(0f, 1f);
                InnerDamperCurve.AddKey(1f, 1f);
            }

        }
    }
}