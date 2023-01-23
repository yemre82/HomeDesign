using System;
using HurricaneVR.Framework.Shared;
using UnityEngine;

namespace HurricaneVR.Framework.ControllerInput
{
    public class HVRPlayerInputs : MonoBehaviour
    {
        [Header("Grab Settings")]
        public bool CanDistanceGrab = true;
        public bool CanTriggerGrab;
        
        [Tooltip("For non flick style force grabber")]
        public HVRForceGrabActivation ForceGrabActivation = HVRForceGrabActivation.Grip;

        [Range(0f, 1f)]
        public float TriggerGrabThreshold = .7f;

        [Header("Inputs Debugging")]
        public Vector2 MovementAxis;
        public Vector2 TurnAxis;

        public bool IsTeleportActivated;
        public bool IsTeleportDeactivated;

        public bool IsSprintingActivated;
        public bool SprintRequiresDoubleClick;

        public bool IsCrouchActivated;
        public bool IsStandActivated;

        public HVRButtonState JumpState;
        public HVRButtonState CrouchState;
        public HVRButtonState StandState;

        public HVRButtonState LeftTriggerGrabState;
        public HVRButtonState RightTriggerGrabState;

        public bool IsLeftGrabActivated;
        public bool IsLeftHoldActive;
        public bool IsLeftGripHoldActive;
        public bool IsLeftTriggerHoldActive;

        public bool IsRightGrabActivated;
        public bool IsRightHoldActive;
        public bool IsRightGripHoldActive;
        public bool IsRightTriggerHoldActive;

        public bool IsLeftForceGrabActive;
        public bool IsRightForceGrabActive;

        public bool IsLeftForceGrabActivated;
        public bool IsRightForceGrabActivated;

        public bool IsJumpActivated;

        public HVRHandSide TeleportHandSide = HVRHandSide.Right;
        public bool SwapMovementAxis;

        public bool UpdateInputs { get; set; } = true;

        public HVRController RightController => HVRInputManager.Instance.RightController;
        public HVRController LeftController => HVRInputManager.Instance.LeftController;

        public HVRControllerType RightControllerType => RightController.ControllerType;
        public HVRControllerType LeftControllerType => LeftController.ControllerType;

        public HVRController TeleportController => TeleportHandSide == HVRHandSide.Left ? HVRInputManager.Instance.LeftController : HVRInputManager.Instance.RightController;

        public void Update()
        {
            UpdateInput();
            AfterInputUpdate();
        }

        protected virtual void UpdateInput()
        {
            if (!UpdateInputs)
                return;

            ResetState(ref LeftTriggerGrabState);
            ResetState(ref RightTriggerGrabState);
            SetState(ref LeftTriggerGrabState, LeftController.Trigger > TriggerGrabThreshold);
            SetState(ref RightTriggerGrabState, RightController.Trigger > TriggerGrabThreshold);

            MovementAxis = GetMovementAxis();
            TurnAxis = GetTurnAxis();
            IsTeleportActivated = GetTeleportActivated();
            IsTeleportDeactivated = GetTeleportDeactivated();
            IsSprintingActivated = GetSprinting();

            IsCrouchActivated = GetCrouch();

            IsLeftGrabActivated = GetIsLeftGrabActivated();
            IsLeftHoldActive = GetIsLeftHoldActive();

            IsRightGrabActivated = GetIsRightGrabActivated();
            IsRightHoldActive = GetIsRightHoldActive();

            GetForceGrabActivated(out IsLeftForceGrabActivated, out IsRightForceGrabActivated);
            GetForceGrabActive(out IsLeftForceGrabActive, out IsRightForceGrabActive);

            IsJumpActivated = GetIsJumpActivated();
            IsStandActivated = GetStand();

            ResetState(ref CrouchState);
            ResetState(ref StandState);
            ResetState(ref JumpState);

            SetState(ref CrouchState, IsCrouchActivated);
            SetState(ref StandState, IsStandActivated);
            SetState(ref JumpState, IsJumpActivated);
        }

        protected virtual void AfterInputUpdate()
        {

        }



        protected void ResetState(ref HVRButtonState buttonState)
        {
            buttonState.JustDeactivated = false;
            buttonState.JustActivated = false;
            buttonState.Value = 0f;
        }

        protected void SetState(ref HVRButtonState buttonState, bool pressed)
        {
            if (pressed)
            {
                if (!buttonState.Active)
                {
                    buttonState.JustActivated = true;
                    buttonState.Active = true;
                }
            }
            else
            {
                if (buttonState.Active)
                {
                    buttonState.Active = false;
                    buttonState.JustDeactivated = true;
                }
            }
        }


        protected virtual bool GetStand()
        {
            return false;
        }

        protected virtual bool GetIsJumpActivated()
        {
            if (RightController.ControllerType == HVRControllerType.Vive)
            {
                return false;//todo
            }

            return false;
        }

        protected virtual void GetForceGrabActivated(out bool left, out bool right)
        {
            if (!CanDistanceGrab)
            {
                left = false;
                right = false;
                return;
            }

            if (ForceGrabActivation == HVRForceGrabActivation.Grip)
            {
                left = LeftController.GripButtonState.JustActivated;
                right = RightController.GripButtonState.JustActivated;
            }
            else
            {
                left = LeftController.GripButtonState.Active && LeftController.TriggerButtonState.JustActivated;
                right = RightController.GripButtonState.Active && RightController.TriggerButtonState.JustActivated;
            }
        }

        protected virtual void GetForceGrabActive(out bool left, out bool right)
        {
            if (!CanDistanceGrab)
            {
                left = false;
                right = false;
                return;
            }

            left = LeftController.GripButtonState.Active;
            right = RightController.GripButtonState.Active;
        }

        public bool GetForceGrabActivated(HVRHandSide side)
        {
            if (!CanDistanceGrab)
            {
                return false;
            }

            return side == HVRHandSide.Left ? IsLeftForceGrabActivated : IsRightForceGrabActivated;
        }

        public bool GetForceGrabActive(HVRHandSide side)
        {
            if (!CanDistanceGrab)
            {
                return false;
            }

            return side == HVRHandSide.Left ? IsLeftForceGrabActive : IsRightForceGrabActive;
        }

        public bool GetGrabActivated(HVRHandSide side)
        {
            return side == HVRHandSide.Left ? IsLeftGrabActivated : IsRightGrabActivated;
        }

        public bool GetHoldActive(HVRHandSide side)
        {
            return side == HVRHandSide.Left ? IsLeftHoldActive : IsRightHoldActive;
        }

        public bool GetGripHoldActive(HVRHandSide side)
        {
            return side == HVRHandSide.Left ? IsLeftGripHoldActive : IsRightGripHoldActive;
        }

        public HVRButtonState GetTriggerGrabState(HVRHandSide side)
        {
            return side == HVRHandSide.Left ? LeftTriggerGrabState : RightTriggerGrabState;
        }

        protected virtual bool GetIsLeftGrabActivated()
        {
            return LeftController.GripButtonState.JustActivated;
        }


        protected virtual bool GetIsLeftHoldActive()
        {
            IsLeftTriggerHoldActive = LeftController.Trigger > TriggerGrabThreshold;
            IsLeftGripHoldActive = LeftController.GripButtonState.Active;
            if (CanTriggerGrab && IsLeftTriggerHoldActive)
            {
                return true;
            }
            return LeftController.GripButtonState.Active;
        }

        protected virtual bool GetIsRightGrabActivated()
        {
            return RightController.GripButtonState.JustActivated;
        }


        protected virtual bool GetIsRightHoldActive()
        {
            IsRightTriggerHoldActive = RightController.Trigger > TriggerGrabThreshold;
            IsRightGripHoldActive = RightController.GripButtonState.Active;
            if (CanTriggerGrab && IsRightTriggerHoldActive)
            {
                return true;
            }
            return RightController.GripButtonState.Active;
        }

        protected virtual Vector2 GetMovementAxis()
        {
            if (SwapMovementAxis)
            {
                if (RightController.ControllerType == HVRControllerType.Vive)
                {
                    if (RightController.TrackpadButtonState.Active)
                        return RightController.TrackpadAxis;
                    return Vector2.zero;
                }

                return RightController.JoystickAxis;
            }

            if (LeftController.ControllerType == HVRControllerType.Vive)
            {
                if (LeftController.TrackpadButtonState.Active)
                    return LeftController.TrackpadAxis;
                return Vector2.zero;
            }

            return LeftController.JoystickAxis;
        }

        protected virtual Vector2 GetTurnAxis()
        {
            if (SwapMovementAxis)
            {
                if (LeftController.ControllerType == HVRControllerType.Vive)
                {
                    if (Mathf.Abs(LeftController.TrackpadAxis.y) > .6f)
                        return Vector2.zero;

                    if (LeftController.TrackpadButtonState.Active)
                    {
                        return LeftController.TrackpadAxis;
                    }
                    return Vector2.zero;
                }

                return LeftController.JoystickAxis;
            }

            if (RightController.ControllerType == HVRControllerType.Vive)
            {
                if (Mathf.Abs(RightController.TrackpadAxis.y) > .6f)
                    return Vector2.zero;

                if (RightController.TrackpadButtonState.Active)
                {

                    return RightController.TrackpadAxis;
                }
                return Vector2.zero;
            }

            return RightController.JoystickAxis;
        }

        protected virtual bool GetTeleportDeactivated()
        {
            if (HVRInputManager.Instance.RightController.ControllerType == HVRControllerType.Vive)
            {
                return HVRController.GetButtonState(HVRHandSide.Right, HVRButtons.Menu).JustDeactivated;
            }

            return TeleportController.JoystickAxis.y > -.25f;
        }

        protected virtual bool GetTeleportActivated()
        {
            if (HVRInputManager.Instance.RightController.ControllerType == HVRControllerType.Vive)
            {
                return HVRController.GetButtonState(HVRHandSide.Right, HVRButtons.Menu).Active;
            }

            return TeleportController.JoystickAxis.y < -.5f && Mathf.Abs(TeleportController.JoystickAxis.x) < .30;
        }

        protected virtual bool GetSprinting()
        {
            if (LeftController.ControllerType == HVRControllerType.Vive)
            {
                SprintRequiresDoubleClick = true;
                return LeftController.TrackpadButtonState.JustActivated;
            }

            SprintRequiresDoubleClick = false;
            if (RightController.ControllerType == HVRControllerType.WMR)
            {
                return RightController.TrackPadRight.JustActivated;
            }

            //controls that allow you to depress the joystick (wmr opens up steamvr)
            return LeftController.JoystickButtonState.JustActivated;
        }

        protected virtual bool GetCrouch()
        {
            if (RightController.ControllerType == HVRControllerType.Vive)
            {
                return RightController.TrackPadUp.JustActivated;
            }

            if (RightController.ControllerType == HVRControllerType.WMR)
            {
                return RightController.TrackPadDown.JustActivated;
            }

            return RightController.SecondaryButtonState.JustActivated;
        }
    }

    public enum HVRForceGrabActivation
    {
        Grip,
        GripHoldTriggerPress
    }
}