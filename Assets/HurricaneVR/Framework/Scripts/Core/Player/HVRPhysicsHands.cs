using System.Collections;
using HurricaneVR.Framework.Shared;
using UnityEngine;
using UnityEngine.Serialization;

namespace HurricaneVR.Framework.Core.Player
{
    [RequireComponent(typeof(Rigidbody))]
    public class HVRPhysicsHands : MonoBehaviour
    {
        [Tooltip("Target transform for position and rotation tracking")]
        [FormerlySerializedAs("Controller")]
        public Transform Target;
        public HVRJointSettings JointSettings;
        public Rigidbody ParentRigidBody;

        [Header("Debug")]
        public HVRJointSettings CurrentSettings;

        [Tooltip("If true will update the joint every update - useful for tweaking HVRJointSettings in play mode.")]
        public bool AlwaysUpdateJoint;
        public Rigidbody RigidBody { get; private set; }
        public ConfigurableJoint Joint { get; protected set; }

        public HVRJointSettings JointOverride { get; private set; }

        public bool Stopped { get; private set; }

        private JointDrive _stoppedDrive;

        protected virtual void Awake()
        {
            RigidBody = GetComponent<Rigidbody>();
            //this joint needs to be created before any offsets are applied to the controller target
            //due to how joints snapshot their initial rotations on creation
            SetupJoint();
            _stoppedDrive = new JointDrive();
            _stoppedDrive.maximumForce = 0f;
            _stoppedDrive.positionSpring = 0f;
            _stoppedDrive.positionDamper = 0f;
            
            //fixing the bug where the hand goes to world 0,0,0 at start
            StartCoroutine(StopHandsRoutine());
        }

        protected virtual IEnumerator StopHandsRoutine()
        {
            var count = 0;
            while (count < 100)
            {
                yield return new WaitForFixedUpdate();
                RigidBody.velocity = Vector3.zero;
                RigidBody.angularVelocity = Vector3.zero;
                transform.position = Target.position;
                count++;
            }
        }

        protected virtual void Start()
        {

        }

        protected virtual void FixedUpdate()
        {
            if (AlwaysUpdateJoint)
            {
                UpdateJoint();
            }
        }

        protected virtual void UpdateJoint()
        {
            if (Stopped)
                return;

            if (JointOverride != null)
            {
                JointOverride.ApplySettings(Joint);
            }
            else if (JointSettings != null)
            {
                JointSettings.ApplySettings(Joint);
            }
        }

        public virtual void SetupJoint()
        {

        }

        public virtual void UpdateStrength(HVRJointSettings settings)
        {
            JointOverride = settings;
            CurrentSettings = settings;
            UpdateJoint();
        }

        public virtual void ResetStrength()
        {
            JointOverride = null;
            CurrentSettings = null;
            UpdateJoint();
        }

        public virtual void Stop()
        {
            Stopped = true;
            Joint.xDrive = Joint.yDrive = Joint.zDrive = Joint.angularXDrive = Joint.angularYZDrive = Joint.slerpDrive = _stoppedDrive;
        }

        public virtual void Restart()
        {
            Stopped = false;
            UpdateJoint();
        }
    }
}