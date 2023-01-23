using UnityEngine;
using UnityEngine.Serialization;

namespace HurricaneVR.Framework.Shared.HandPoser
{
    public class HVRPosableGrabPoint : MonoBehaviour
    {
        [Header("Settings")]
        public bool IsJointAnchor;

        [Range(0f, 360f)]
        public float AllowedAngleDifference = 360f;


        public bool LeftHand = true;
        public bool RightHand = true;

        [Header("Controller Tracking Offsets")]
        [FormerlySerializedAs("jointOffset")]
        public Vector3 HandRotationOffset;
        public Vector3 HandPositionOffset;

        [Header("Transforms")]
        public Transform GrabIndicatorPosition;
        public Transform VisualGrabPoint;

        public HVRHandPoser HandPoser;


        [Header("Line Grab")]
        public bool IsLineGrab;
        [DrawIf("IsLineGrab", true)]
        public Transform LineStart;
        [DrawIf("IsLineGrab", true)]
        public Transform LineEnd;
        [DrawIf("IsLineGrab", true)]
        public bool CanLineFlip = true;
        [DrawIf("IsLineGrab", true)]
        public float LooseDamper = 100;
        [DrawIf("IsLineGrab", true)]
        public float LooseAngularDamper = 1;
        [DrawIf("IsLineGrab", true)]
        public bool LineCanReposition = true;
        [DrawIf("IsLineGrab", true)]
        public bool LineCanRotate = true;

        public Vector3 Line { get; private set; }
        public Vector3 LineMid { get; private set; }

        public Quaternion LeftPoseOffset { get; private set; }
        public Quaternion RightPoseOffset { get; private set; }

        public Vector3 LeftPosePositionOffset { get; private set; }

        public Vector3 RightPosePositionOffset { get; private set; }

        void Start()
        {
            HandPoser = GetComponent<HVRHandPoser>();

            LeftPoseOffset = Quaternion.identity;
            RightPoseOffset = Quaternion.identity;

            if (HandPoser)
            {
                var grabbable = transform.parent?.parent;

                if (HandPoser && HandPoser.PrimaryPose != null && HandPoser.PrimaryPose.Pose && HandPoser.PrimaryPose.Pose.RightHand != null)
                {
                    RightPoseOffset = Quaternion.Euler(HandPoser.PrimaryPose.Pose.RightHand.Rotation.eulerAngles);
                    RightPosePositionOffset = HandPoser.PrimaryPose.Pose.RightHand.Position;
                }
                else if (RightHand)
                {
                    Debug.LogWarning($"Right Hand pose missing! {grabbable?.name}.{this.name}");
                }

                if (HandPoser && HandPoser.PrimaryPose != null && HandPoser.PrimaryPose.Pose && HandPoser.PrimaryPose.Pose.LeftHand != null && LeftHand)
                {
                    LeftPoseOffset = Quaternion.Euler(HandPoser.PrimaryPose.Pose.LeftHand.Rotation.eulerAngles);
                    LeftPosePositionOffset = HandPoser.PrimaryPose.Pose.LeftHand.Position;
                }
                else if (LeftHand)
                {
                    Debug.LogWarning($"Left Hand pose missing! {grabbable?.name}.{this.name}");
                }
            }

            if (IsLineGrab)
            {
                Line = LineEnd.localPosition - LineStart.localPosition;
                LineMid = (LineEnd.localPosition + LineStart.localPosition) / 2f;
            }
        }

        public Vector3 GetPosePositionOffset(HVRHandSide side)
        {
            if (side == HVRHandSide.Left)
                return LeftPosePositionOffset;
            return RightPosePositionOffset;
        }

        public Quaternion GetPoseRotationOffset(HVRHandSide side)
        {
            if (side == HVRHandSide.Left)
                return LeftPoseOffset;
            return RightPoseOffset;
        }

        public Quaternion GetPoseRotation(HVRHandSide side)
        {
            if (side == HVRHandSide.Left)
                return transform.rotation * LeftPoseOffset;
            return transform.rotation * RightPoseOffset;
        }

#if UNITY_EDITOR

        private void OnDrawGizmos()
        {
            // Show Grip Points
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.015f);
        }

#endif
    }
}
