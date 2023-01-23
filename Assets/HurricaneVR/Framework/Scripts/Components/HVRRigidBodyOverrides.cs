using Assets.HurricaneVR.Framework.Shared.Utilities;
using UnityEngine;

namespace HurricaneVR.Framework.Components
{
    [RequireComponent(typeof(Rigidbody))]
    public class HVRRigidBodyOverrides : MonoBehaviour
    {

        public bool OverrideCOM;
        public bool OverrideRotation;
        public bool OverrideTensor;
        public Vector3 CenterOfMass;
        public Vector3 InertiaTensorRotation;
        public Vector3 InertiaTensor;

        [Header("Debug")]
        public Vector3 COMGizmoSize = new Vector3(.02f, .02f, .02f);
        public bool LiveUpdate;

        private Quaternion _inertiaTensorRotation;

        public Rigidbody Rigidbody { get; private set; }

        void Awake()
        {
            Rigidbody = GetComponent<Rigidbody>();
            _inertiaTensorRotation = Quaternion.Euler(InertiaTensorRotation);
            this.ExecuteNextUpdate(ApplyOverrides);
        }

        public void ApplyOverrides()
        {
            if (OverrideCOM)
            {
                Rigidbody.centerOfMass = CenterOfMass;
            }

            if (OverrideTensor)
            {
                Rigidbody.inertiaTensor = InertiaTensor;
            }

            if (OverrideRotation)
            {
                Rigidbody.inertiaTensorRotation = _inertiaTensorRotation;
            }
        }

        void FixedUpdate()
        {
            if (LiveUpdate)
            {
                ApplyOverrides();
            }
        }

        void OnDrawGizmosSelected()
        {
            if (OverrideCOM)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawCube(transform.TransformPoint(CenterOfMass), COMGizmoSize);
            }
        }
    }
}