using HurricaneVR.Framework.Shared;
using UnityEngine;
using UnityEngine.Events;

namespace HurricaneVR.Framework.Core.Player
{
   
    public class HVRJointHand : HVRPhysicsHands
    {
        public UnityEvent MaxDistanceReached = new UnityEvent();
        public UnityEvent ReturnedToController = new UnityEvent();

        [Header("Torque Control")]
        [Tooltip("If true the joint will use controller angular velocity as it's target")]
        public bool EnableTargetAngularVelocity;
        [Tooltip("Scaling factor to apply to target angular velocity")]
        [Range(0, 1)]
        public float AngularVelocityScale = 1f;

        public HVRHandPhysics HandPhysics;

        public float MaxDistance = .8f;
        public bool DisablePhysicsOnReturn;
        public bool IsReturningToController;
        
        private Vector3 _previousControllerPosition;
        private Quaternion _previousRotation;

        protected override void Start()
        {
            base.Start();

            _previousControllerPosition = Target.position;
        }

        public void Disable()
        {
            RigidBody.collisionDetectionMode = CollisionDetectionMode.Discrete;
            RigidBody.isKinematic = true;
        }

        public void Enable()
        {
            RigidBody.isKinematic = false;
            RigidBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        public override void SetupJoint()
        {
            //Debug.Log($"{name} joint created.");
            //this joint needs to be created before any offsets are applied to the controller target
            //due to how joints snapshot their initial rotations on creation
            Joint = ParentRigidBody.transform.gameObject.AddComponent<ConfigurableJoint>();
            Joint.autoConfigureConnectedAnchor = false;
            Joint.connectedBody = RigidBody;
            Joint.connectedAnchor = Vector3.zero;
            Joint.anchor = ParentRigidBody.transform.InverseTransformPoint(Target.position);
            Joint.enableCollision = false;
            Joint.enablePreprocessing = false;
            Joint.rotationDriveMode = RotationDriveMode.Slerp;

            UpdateStrength(JointSettings);
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
            UpdateTargetVelocity();

            if (Vector3.Distance(transform.position, Target.position) > MaxDistance)
            {
                if (!IsReturningToController)
                {
                    IsReturningToController = true;
                    MaxDistanceReached.Invoke();

                    if (HandPhysics && DisablePhysicsOnReturn)
                    {
                        HandPhysics.SetAllToTrigger();
                    }
                }

                if (!HandPhysics || !DisablePhysicsOnReturn)
                {
                    transform.position = Vector3.MoveTowards(transform.position, Target.position, MaxDistance / 2f);
                }
            }
            else if (IsReturningToController)
            {
                if (HandPhysics && DisablePhysicsOnReturn)
                {
                    HandPhysics.ResetToNonTrigger();
                }
                IsReturningToController = false;
                ReturnedToController.Invoke();
            }
        }

        protected override void UpdateJoint()
        {
            if (JointOverride != null)
            {
                JointOverride.ApplySettings(Joint);
            }
            else if (JointSettings != null)
            {
                JointSettings.ApplySettings(Joint);
            }
            else
            {
                Debug.LogError($"JointSettings field is empty, must be populated with HVRJointSettings scriptable object.");
            }
        }

        public virtual void UpdateTargetVelocity()
        {
            var worldVelocity = (Target.position - _previousControllerPosition) / Time.fixedDeltaTime;
            _previousControllerPosition = Target.position;
            Joint.targetVelocity = ParentRigidBody.transform.InverseTransformDirection(worldVelocity);

            var angularVelocity = Target.rotation.AngularVelocity(_previousRotation);
            if (EnableTargetAngularVelocity)
            {
                angularVelocity.Scale(new Vector3(AngularVelocityScale, AngularVelocityScale, AngularVelocityScale));
                Joint.targetAngularVelocity = Quaternion.Inverse(ParentRigidBody.transform.rotation) * angularVelocity;

                if (Joint.rotationDriveMode == RotationDriveMode.XYAndZ)
                {
                    Joint.targetAngularVelocity *= -1;
                }
            }
            else
            {
                Joint.targetAngularVelocity = Vector3.zero;
            }

            _previousRotation = Target.rotation;
        }

        private void OnDrawGizmos()
        {
            if (RigidBody)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(RigidBody.worldCenterOfMass, .017f);
            }
        }
    }
}
