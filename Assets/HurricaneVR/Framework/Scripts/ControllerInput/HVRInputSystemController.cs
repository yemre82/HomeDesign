#if ENABLE_INPUT_SYSTEM
using System;
using System.Collections;
using System.Collections.Generic;
using HurricaneVR.Framework.Shared;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HurricaneVR.Framework.ControllerInput
{

    public class HVRInputSystemController : HVRController
    {
        public static HVRInputActions InputActions = null;

        protected override void Awake()
        {
            base.Awake();

            if (InputActions == null)
            {
                InputActions = new HVRInputActions();
                InputActions.Enable();
            }
        }

        protected override void UpdateInput()
        {
            if (Side == HVRHandSide.Left)
            {
                JoystickAxis = InputActions.LeftHand.Primary2DAxis.ReadValue<Vector2>();

                SetBool(out JoystickClicked, InputActions.LeftHand.Primary2DAxisClick);
                SetBool(out TrackPadClicked, InputActions.LeftHand.Secondary2DAxisClick);

                TrackpadAxis = InputActions.LeftHand.Secondary2DAxis.ReadValue<Vector2>();

                Grip = InputActions.LeftHand.Grip.ReadValue<float>();
                Trigger = InputActions.LeftHand.Trigger.ReadValue<float>();

                SetBool(out PrimaryButton, InputActions.LeftHand.PrimaryButton);
                SetBool(out SecondaryButton, InputActions.LeftHand.SecondaryButton);

                SetBool(out PrimaryTouch, InputActions.LeftHand.PrimaryTouch);
                SetBool(out SecondaryTouch, InputActions.LeftHand.SecondaryTouch);

                SetBool(out JoystickTouch, InputActions.LeftHand.Primary2DAxisTouch);
                SetBool(out TrackPadTouch, InputActions.LeftHand.Secondary2DAxisTouch);

                SetBool(out TriggerTouch, InputActions.LeftHand.TriggerTouch);

                SetBool(out MenuButton, InputActions.LeftHand.Menu);

                SetBool(out GripButton, InputActions.LeftHand.GripPress);
                SetBool(out TriggerButton, InputActions.LeftHand.TriggerPress);
            }
            else
            {
                JoystickAxis = InputActions.RightHand.Primary2DAxis.ReadValue<Vector2>();

                SetBool(out JoystickClicked, InputActions.RightHand.Primary2DAxisClick);
                SetBool(out TrackPadClicked, InputActions.RightHand.Secondary2DAxisClick);

                TrackpadAxis = InputActions.RightHand.Secondary2DAxis.ReadValue<Vector2>();

                Grip = InputActions.RightHand.Grip.ReadValue<float>();
                Trigger = InputActions.RightHand.Trigger.ReadValue<float>();

                SetBool(out PrimaryButton, InputActions.RightHand.PrimaryButton);
                SetBool(out SecondaryButton, InputActions.RightHand.SecondaryButton);

                SetBool(out PrimaryTouch, InputActions.RightHand.PrimaryTouch);
                SetBool(out SecondaryTouch, InputActions.RightHand.SecondaryTouch);

                SetBool(out JoystickTouch, InputActions.RightHand.Primary2DAxisTouch);
                SetBool(out TrackPadTouch, InputActions.RightHand.Secondary2DAxisTouch);
                
                SetBool(out TriggerTouch, InputActions.RightHand.TriggerTouch);

                SetBool(out MenuButton, InputActions.RightHand.Menu);

                SetBool(out GripButton, InputActions.RightHand.GripPress);
                SetBool(out TriggerButton, InputActions.RightHand.TriggerPress);
            }

        }

        private void SetBool(out bool val, InputAction action)
        {
            val = false;
            if (action.activeControl != null)
            {
                var type = action.activeControl.valueType;
                if (type == typeof(bool))
                {
                    val = action.ReadValue<bool>();
                }
                else if (type == typeof(float))
                {
                    val = action.ReadValue<float>() > .5f;
                }
            }
        }

        protected override void CheckButtonState(HVRButtons button, ref HVRButtonState buttonState)
        {
            ResetButton(ref buttonState);

            switch (button)
            {
                case HVRButtons.Grip:
                    buttonState.Value = Grip;

                    if (!InputMap.GripUseAnalog)
                    {
                        SetButtonState(button, ref buttonState, GripButton);
                    }

                    if (InputMap.GripUseAnalog || InputMap.GripUseEither && !buttonState.Active)
                    {
                        SetButtonState(button, ref buttonState, Grip >= InputMap.GripThreshold);
                    }


                    break;
                case HVRButtons.Trigger:
                    buttonState.Value = Trigger;
                    if (InputMap.TriggerUseAnalog)
                        SetButtonState(button, ref buttonState, Trigger >= InputMap.TriggerThreshold);
                    else
                        SetButtonState(button, ref buttonState, TriggerButton);
                    break;
                case HVRButtons.Primary:
                    SetButtonState(button, ref buttonState, PrimaryButton);
                    break;
                case HVRButtons.PrimaryTouch:
                    SetButtonState(button, ref buttonState, PrimaryTouch);
                    break;
                case HVRButtons.Secondary:
                    SetButtonState(button, ref buttonState, SecondaryButton);
                    break;
                case HVRButtons.SecondaryTouch:
                    SetButtonState(button, ref buttonState, SecondaryTouch);
                    break;
                case HVRButtons.Menu:
                    SetButtonState(button, ref buttonState, MenuButton);
                    break;
                case HVRButtons.JoystickButton:
                    SetButtonState(button, ref buttonState, JoystickClicked);
                    break;
                case HVRButtons.TrackPadButton:
                    SetButtonState(button, ref buttonState, TrackPadClicked);
                    break;
                case HVRButtons.JoystickTouch:
                    SetButtonState(button, ref buttonState, JoystickTouch);
                    break;
                case HVRButtons.TrackPadTouch:
                    SetButtonState(button, ref buttonState, TrackPadTouch);
                    break;
                case HVRButtons.TriggerTouch:
                    SetButtonState(button, ref buttonState, TriggerTouch);
                    break;
                case HVRButtons.ThumbTouch:
                    SetButtonState(button, ref buttonState, ThumbTouch);
                    break;
                case HVRButtons.TriggerNearTouch:
                    SetButtonState(button, ref buttonState, TriggerNearTouch);
                    break;
                case HVRButtons.ThumbNearTouch:
                    SetButtonState(button, ref buttonState, ThumbNearTouch);
                    break;
                case HVRButtons.TrackPadLeft:
                    SetButtonState(button, ref buttonState, TrackPadClicked && TrackpadAxis.x <= -InputMap.Axis2DLeftThreshold);
                    break;
                case HVRButtons.TrackPadRight:
                    SetButtonState(button, ref buttonState, TrackPadClicked && TrackpadAxis.x >= InputMap.Axis2DRighThreshold);
                    break;
                case HVRButtons.TrackPadUp:
                    SetButtonState(button, ref buttonState, TrackPadClicked && TrackpadAxis.y >= InputMap.Axis2DUpThreshold);
                    break;
                case HVRButtons.TrackPadDown:
                    SetButtonState(button, ref buttonState, TrackPadClicked && TrackpadAxis.y <= -InputMap.Axis2DDownThreshold);
                    break;
            }
        }
    }
}

#endif