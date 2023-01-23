using System.Collections;
using System.Collections.Generic;
using HurricaneVR.Framework.Shared.HandPoser.Data;
using UnityEngine;
using Time = UnityEngine.Time;

namespace HurricaneVR.Framework.Shared.HandPoser
{
    public class HVRHandAnimator : MonoBehaviour
    {
        [Header("Settings")]
        public bool PosePosAndRot = true;
        public float FingerCurlSpeed = 16f;

        [Header("Components")]
        public HVRPhysicsPoser PhysicsPoser;
        public HVRPosableHand Hand;
        public HVRHandPoser DefaultPoser;


        [Header("Debug View")]
        public HVRHandPoser CurrentPoser;

        private HVRHandPoseData BlendedPrimary;
        private HVRHandPoseData DefaultPrimary;

        private HVRHandPoseData TargetPrimary;
        private HVRHandPoseData CurrentPrimary;

        private readonly List<HVRHandPoseData> BlendedSecondarys = new List<HVRHandPoseData>();
        private readonly List<HVRHandPoseData> TargetSecondarys = new List<HVRHandPoseData>();



        public bool IsMine { get; set; } = true;

        public float[] FingerCurlSource { get; set; }

        private bool _poseHand = true;
        private float[] _fingerCurls;

        protected virtual void Start()
        {
            _fingerCurls = new float[5];

            if (!PhysicsPoser)
            {
                PhysicsPoser = GetComponent<HVRPhysicsPoser>();
            }

            if (!DefaultPoser)
            {
                DefaultPoser = GetComponent<HVRHandPoser>();
            }

            if (!Hand)
            {
                Hand = GetComponent<HVRPosableHand>();
            }


            DefaultPrimary = DefaultPoser.PrimaryPose.Pose.GetPose(Hand.IsLeft).DeepCopy();

            if (IsMine)
            {
                FingerCurlSource = Hand.IsLeft ? HVRController.LeftFingerCurls : HVRController.RightFingerCurls;
            }

            ResetToDefault();
        }

        protected virtual void LateUpdate()
        {
            UpdateFingerCurls();
            UpdatePoser();
        }

        protected virtual void UpdateFingerCurls()
        {
            if (FingerCurlSource == null)
                return;

            for (int i = 0; i < 5; i++)
            {
                _fingerCurls[i] = Mathf.Lerp(_fingerCurls[i], FingerCurlSource[i], Time.deltaTime * FingerCurlSpeed);
            }
        }

        public void Enable()
        {
            enabled = true;
        }

        public void Disable()
        {
            enabled = false;
        }

        private void UpdatePoser()
        {
            if (CurrentPoser == null)
            {
                return;
            }

            UpdateBlends();
            ApplyBlending();
            Hand.Pose(BlendedPrimary, _poseHand);
        }

        private void ApplyBlending()
        {
            ApplyFingerCurls(TargetPrimary, DefaultPrimary, CurrentPrimary, CurrentPoser.PrimaryPose);
            ApplyBlend(BlendedPrimary, CurrentPoser.PrimaryPose, TargetPrimary);

            if (CurrentPoser.Blends == null)
            {
                return;
            }

            var targetBlends = CurrentPoser.Blends;
            for (int i = 0; i < targetBlends.Count; i++)
            {
                var targetBlend = targetBlends[i];

                if (targetBlend.Disabled)
                {
                    continue;
                }

                if (i >= BlendedSecondarys.Count || i >= TargetSecondarys.Count)
                {
                    //user didn't set a pose on a blend, skip it
                    continue;
                }

                ApplyFingerCurls(BlendedSecondarys[i], CurrentPrimary, TargetSecondarys[i], targetBlend);

                ApplyBlend(BlendedPrimary, targetBlend, BlendedSecondarys[i]);
            }
        }

        private void ApplyFingerCurls(HVRHandPoseData targetHand, HVRHandPoseData defaultHand, HVRHandPoseData sourceHand, HVRHandPoseBlend blend)
        {
            for (int i = 0; i < targetHand.Fingers.Length; i++)
            {
                var targetFinger = targetHand.Fingers[i];
                var defaultFinger = defaultHand.Fingers[i];
                var sourceFinger = sourceHand.Fingers[i];

                var fingerType = blend.GetFingerType(i);
                var fingerStart = blend.GetFingerStart(i);
                var curl = _fingerCurls[i];
                var remainder = 1 - fingerStart;
                curl = fingerStart + curl * remainder;
                curl = Mathf.Clamp(curl, 0f, 1f);


                for (int j = 0; j < targetFinger.Bones.Count; j++)
                {
                    if (fingerType == HVRFingerType.Close)
                    {
                        targetFinger.Bones[j].Position = Vector3.Lerp(sourceFinger.Bones[j].Position, defaultFinger.Bones[j].Position, 1 - curl);
                        targetFinger.Bones[j].Rotation = Quaternion.Lerp(sourceFinger.Bones[j].Rotation, defaultFinger.Bones[j].Rotation, 1 - curl);
                    }
                }
            }
        }

        private void ApplyBlend(HVRHandPoseData targetHand, HVRHandPoseBlend targetBlend, HVRHandPoseData sourceHand)
        {
            //var target = targetBlend.Pose.GetPose(Hand.IsLeft);
            var lerp = targetBlend.Value * targetBlend.Weight;

            if (targetBlend.Mask == HVRHandPoseMask.None || targetBlend.Mask.HasFlag(HVRHandPoseMask.Hand))
            {
                targetHand.Position = Vector3.Lerp(targetHand.Position, sourceHand.Position, lerp);
                targetHand.Rotation = Quaternion.Lerp(targetHand.Rotation, sourceHand.Rotation, lerp);
            }

            for (var i = 0; i < targetHand.Fingers.Length; i++)
            {
                var targetFinger = targetHand.Fingers[i];
                var sourceFinger = sourceHand.Fingers[i];

                HVRHandPoseMask mask;
                if (i == 0) mask = HVRHandPoseMask.Thumb;
                else if (i == 1) mask = HVRHandPoseMask.Index;
                else if (i == 2) mask = HVRHandPoseMask.Middle;
                else if (i == 3) mask = HVRHandPoseMask.Ring;
                else if (i == 4) mask = HVRHandPoseMask.Pinky;
                else continue;

                if (targetBlend.Mask == HVRHandPoseMask.None || targetBlend.Mask.HasFlag(mask))
                {
                    for (var j = 0; j < targetFinger.Bones.Count; j++)
                    {
                        var targetBone = targetFinger.Bones[j];
                        var sourceBone = sourceFinger.Bones[j];
                        targetBone.Position = Vector3.Lerp(targetBone.Position, sourceBone.Position, lerp);
                        targetBone.Rotation = Quaternion.Lerp(targetBone.Rotation, sourceBone.Rotation, lerp);
                    }
                }
            }
        }

        private void UpdateBlends()
        {
            if (!IsMine)
                return;

            UpdateBlend(CurrentPoser.PrimaryPose);

            if (CurrentPoser.Blends == null)
            {
                return;
            }

            var blends = CurrentPoser.Blends;
            for (int i = 0; i < blends.Count; i++)
            {
                var blend = blends[i];
                if (blend.Disabled)
                {
                    continue;
                }
                UpdateBlend(blend);
            }
        }

        private void UpdateBlend(HVRHandPoseBlend blend)
        {
            var blendTarget = 0f;

            if (blend.Type == BlendType.Immediate)
            {
                blendTarget = 1f;
            }
            else if (blend.ButtonParameter)
            {
                var button = HVRController.GetButtonState(Hand.Side, blend.Button);
                if (blend.Type == BlendType.BooleanParameter)
                {
                    blendTarget = button.Active ? 1f : 0f;
                }
                else if (blend.Type == BlendType.FloatParameter)
                {
                    blendTarget = button.Value;
                }
            }
            else if (!string.IsNullOrWhiteSpace(blend.AnimationParameter) && blend.AnimationParameter != "None")
            {
                if (blend.Type == BlendType.BooleanParameter)
                {
                    blendTarget = HVRAnimationParameters.GetBoolParameter(Hand.Side, blend.AnimationParameter) ? 1f : 0f;
                }
                else if (blend.Type == BlendType.FloatParameter)
                {
                    blendTarget = HVRAnimationParameters.GetFloatParameter(Hand.Side, blend.AnimationParameter);
                }
            }

            if (blend.Speed > .1f)
            {
                blend.Value = Mathf.Lerp(blend.Value, blendTarget, Time.deltaTime * blend.Speed);
            }
            else
            {
                blend.Value = blendTarget;
            }

        }


        public void ResetToDefault()
        {
            _poseHand = true;
            if (DefaultPoser != null)
            {
                SetCurrentPoser(DefaultPoser);
            }
            else
            {
                Debug.Log($"Default poser not set.");
            }
        }

        public void SetCurrentPoser(HVRHandPoser poser, bool poseHand = true)
        {
            BlendedSecondarys.Clear();
            TargetSecondarys.Clear();

            _poseHand = poseHand;
            if (!PosePosAndRot)
            {
                _poseHand = false;
            }

            CurrentPoser = poser;
            if (poser == null)
                return;

            if (poser.PrimaryPose == null)
            {
                return;
            }

            BlendedPrimary = poser.PrimaryPose.Pose.GetPose(Hand.IsLeft).DeepCopy();
            TargetPrimary = poser.PrimaryPose.Pose.GetPose(Hand.IsLeft).DeepCopy();
            CurrentPrimary = poser.PrimaryPose.Pose.GetPose(Hand.IsLeft);

            for (var i = 0; i < poser.Blends.Count; i++)
            {
                var blend = poser.Blends[i];
                if (blend.Pose)
                {
                    BlendedSecondarys.Add(blend.Pose.GetPose(Hand.IsLeft).DeepCopy());
                    TargetSecondarys.Add(blend.Pose.GetPose(Hand.IsLeft).DeepCopy());
                }
            }

            //if (poser.PrimaryPose.Type == BlendType.Immediate)
            //{
            //    Hand.Pose(poser.PrimaryPose.Pose.GetPose(Hand.Side), _poseHand);
            //}
        }
    }


}
