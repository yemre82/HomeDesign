using System;
using HurricaneVR.Framework.Shared;
using UnityEngine;
using UnityEngine.XR;

namespace HurricaneVR.Framework.ControllerInput
{


    enum ButtonReadType
    {
        None = 0,
        Binary,
        Axis1D,
        Axis2DUp,
        Axis2DDown,
        Axis2DLeft,
        Axis2DRight
    }

    struct ButtonInfo
    {
        public ButtonInfo(string name, ButtonReadType type)
        {
            this.name = name;
            this.type = type;
        }

        public string name;
        public ButtonReadType type;
    }



    public class HVRXRInputController : HVRController
    {

        static ButtonInfo[] s_ButtonData = new ButtonInfo[]
        {
            new ButtonInfo("", ButtonReadType.None),
            new ButtonInfo("MenuButton", ButtonReadType.Binary),
            new ButtonInfo("Trigger", ButtonReadType.Axis1D),
            new ButtonInfo("Grip", ButtonReadType.Axis1D),
            new ButtonInfo("TriggerPressed", ButtonReadType.Binary),
            new ButtonInfo("GripPressed", ButtonReadType.Binary),
            new ButtonInfo("PrimaryButton", ButtonReadType.Binary),
            new ButtonInfo("PrimaryTouch", ButtonReadType.Binary),
            new ButtonInfo("SecondaryButton", ButtonReadType.Binary),
            new ButtonInfo("SecondaryTouch", ButtonReadType.Binary),
            new ButtonInfo("Primary2DAxisTouch", ButtonReadType.Binary),
            new ButtonInfo("Primary2DAxisClick", ButtonReadType.Binary),
            new ButtonInfo("Secondary2DAxisTouch", ButtonReadType.Binary),
            new ButtonInfo("Secondary2DAxisClick", ButtonReadType.Binary),
            new ButtonInfo("Primary2DAxis", ButtonReadType.Axis2DUp),
            new ButtonInfo("Primary2DAxis", ButtonReadType.Axis2DDown),
            new ButtonInfo("Primary2DAxis", ButtonReadType.Axis2DLeft),
            new ButtonInfo("Primary2DAxis", ButtonReadType.Axis2DRight),
            new ButtonInfo("Secondary2DAxis", ButtonReadType.Axis2DUp),
            new ButtonInfo("Secondary2DAxis", ButtonReadType.Axis2DDown),
            new ButtonInfo("Secondary2DAxis", ButtonReadType.Axis2DLeft),
            new ButtonInfo("Secondary2DAxis", ButtonReadType.Axis2DRight),
        };




        protected virtual InputFeatureUsage<Vector2> JoystickAxisFeature => InputMap.JoystickAxis == InputAxes.Primary2DAxis ?
            CommonUsages.primary2DAxis : CommonUsages.secondary2DAxis;

        protected virtual InputFeatureUsage<Vector2> TrackPadAxisFeature => InputMap.TrackPadAxis == InputAxes.Primary2DAxis ?
            CommonUsages.primary2DAxis : CommonUsages.secondary2DAxis;



        protected override void UpdateInput()
        {
            if (InputMap)
            {
                Device.TryGetFeatureValue(JoystickAxisFeature, out JoystickAxis);
                Device.TryGetFeatureValue(TrackPadAxisFeature, out TrackpadAxis);
            }
        }

        protected override void CheckButtonState(HVRButtons button, ref HVRButtonState buttonState)
        {
            ResetButton(ref buttonState);

            if (!InputMap)
                return;

            var trackPadIsPrimary = InputMap.TrackPadAxis == InputAxes.Primary2DAxis;

            switch (button)
            {
                case HVRButtons.Grip:
                    Device.TryGetFeatureValue(CommonUsages.grip, out Grip);
                    buttonState.Value = Grip;
                    SetButtonState(button, ref buttonState, Grip >= InputMap.GripThreshold);
                    break;
                case HVRButtons.Trigger:
                    Device.TryGetFeatureValue(CommonUsages.trigger, out Trigger);
                    buttonState.Value = Trigger;
                    SetButtonState(button, ref buttonState, Trigger >= InputMap.TriggerThreshold);
                    break;
                case HVRButtons.Primary:
                    SetButtonState(button, ref buttonState, IsPressed(Device, InputMap.Primary));
                    PrimaryButton = buttonState.Active;
                    break;
                case HVRButtons.PrimaryTouch:
                    SetButtonState(button, ref buttonState, IsPressed(Device, InputMap.PrimaryTouch));
                    PrimaryTouch = buttonState.Active;
                    break;
                case HVRButtons.Secondary:
                    SetButtonState(button, ref buttonState, IsPressed(Device, InputMap.Secondary));
                    SecondaryButton = buttonState.Active;
                    break;
                case HVRButtons.SecondaryTouch:
                    SetButtonState(button, ref buttonState, IsPressed(Device, InputMap.SecondaryTouch));
                    SecondaryTouch = buttonState.Active;
                    break;
                case HVRButtons.Menu:
                    SetButtonState(button, ref buttonState, IsPressed(Device, InputMap.Menu));
                    MenuButton = buttonState.Active;
                    break;
                case HVRButtons.JoystickButton:
                    SetButtonState(button, ref buttonState, IsPressed(Device, InputMap.JoystickButton));
                    JoystickClicked = buttonState.Active;
                    break;
                case HVRButtons.TrackPadButton:
                    SetButtonState(button, ref buttonState, IsPressed(Device, InputMap.TrackPadButton));
                    TrackPadClicked = buttonState.Active;
                    break;
                case HVRButtons.JoystickTouch:
                    SetButtonState(button, ref buttonState, IsPressed(Device, InputMap.JoystickTouch));
                    JoystickTouch = buttonState.Active;
                    break;
                case HVRButtons.TrackPadTouch:
                    SetButtonState(button, ref buttonState, IsPressed(Device, InputMap.TrackPadTouch));
                    TrackPadTouch = buttonState.Active;
                    break;
                case HVRButtons.TriggerTouch:
#if USING_XR_MANAGEMENT
                    Device.TryGetFeatureValue(indexTouch, out  TriggerTouch);
#else
                    Device.TryGetFeatureValue(legacyIndexTouch, out var temp);
                    
                    TriggerTouch = temp > 0f;
#endif

                    SetButtonState(button, ref buttonState, TriggerTouch);
                    break;
                case HVRButtons.TrackPadLeft:
                    SetButtonState(button,
                        ref TrackPadLeft,
                        IsPressed(Device, trackPadIsPrimary ? HVRXRInputFeatures.PrimaryAxis2DLeft : HVRXRInputFeatures.SecondaryAxis2DLeft));
                    break;
                case HVRButtons.TrackPadRight:
                    SetButtonState(button,
                        ref TrackPadRight,
                        IsPressed(Device, trackPadIsPrimary ? HVRXRInputFeatures.PrimaryAxis2DRight : HVRXRInputFeatures.SecondaryAxis2DRight));
                    break;
                case HVRButtons.TrackPadUp:
                    SetButtonState(button,
                        ref TrackPadUp,
                        IsPressed(Device, trackPadIsPrimary ? HVRXRInputFeatures.PrimaryAxis2DUp : HVRXRInputFeatures.SecondaryAxis2DUp));
                    break;
                case HVRButtons.TrackPadDown:
                    SetButtonState(button,
                        ref TrackPadDown,
                        IsPressed(Device, trackPadIsPrimary ? HVRXRInputFeatures.PrimaryAxis2DDown : HVRXRInputFeatures.SecondaryAxis2DDown));
                    break;
            }
        }

        private readonly InputFeatureUsage<bool> indexTouch = new InputFeatureUsage<bool>("IndexTouch");
        private static InputFeatureUsage<float> legacyIndexTouch = new InputFeatureUsage<float>("IndexTouch");

        protected override void AfterInputUpdate()
        {
            SetButtonState(HVRButtons.ThumbNearTouch, ref ThumbNearTouchState, ThumbTouch);
            SetButtonState(HVRButtons.TriggerNearTouch, ref TriggerNearTouchState, TriggerTouch);
            SetButtonState(HVRButtons.ThumbTouch, ref ThumbTouchState, PrimaryTouch || SecondaryTouch || TrackPadTouch || JoystickTouch);
        }

        public bool CheckAdditionalFeature(HVRXRInputFeatures input)
        {
            if (input == HVRXRInputFeatures.SecondaryAxis2DUp ||
                input == HVRXRInputFeatures.SecondaryAxis2DDown ||
                input == HVRXRInputFeatures.SecondaryAxis2DLeft ||
                input == HVRXRInputFeatures.SecondaryAxis2DRight)
            {
                return IsPressed(Device, InputMap.TrackPadButton);
            }

            if (input == HVRXRInputFeatures.PrimaryAxis2DUp ||
                input == HVRXRInputFeatures.PrimaryAxis2DDown ||
                input == HVRXRInputFeatures.PrimaryAxis2DLeft ||
                input == HVRXRInputFeatures.PrimaryAxis2DRight)
            {
                return IsPressed(Device, InputMap.TrackPadButton);
            }

            return true;
        }

        public bool IsPressed(InputDevice device, HVRXRInputFeatures inputFeature, float threshold = 0f)
        {
            if ((int)inputFeature >= s_ButtonData.Length)
            {
                throw new ArgumentException("[InputHelpers.IsPressed] The value of <button> is out or the supported range.");
            }

            //button down check in addition to track pad check
            if (!CheckAdditionalFeature(inputFeature))
                return false;

            var info = s_ButtonData[(int)inputFeature];
            switch (info.type)
            {
                case ButtonReadType.Binary:
                    {
                        if (device.TryGetFeatureValue(new InputFeatureUsage<bool>(info.name), out bool value))
                        {
                            return value;
                        }
                    }
                    break;
                case ButtonReadType.Axis1D:
                    {
                        if (device.TryGetFeatureValue(new InputFeatureUsage<float>(info.name), out float value))
                        {
                            return value >= threshold;
                        }
                    }
                    break;
                case ButtonReadType.Axis2DUp:
                    {
                        if (device.TryGetFeatureValue(new InputFeatureUsage<Vector2>(info.name), out Vector2 value))
                        {
                            return value.y >= InputMap.Axis2DUpThreshold;
                        }
                    }
                    break;
                case ButtonReadType.Axis2DDown:
                    {
                        if (device.TryGetFeatureValue(new InputFeatureUsage<Vector2>(info.name), out Vector2 value))
                        {
                            return value.y <= -InputMap.Axis2DDownThreshold;
                        }
                    }
                    break;
                case ButtonReadType.Axis2DLeft:
                    {
                        if (device.TryGetFeatureValue(new InputFeatureUsage<Vector2>(info.name), out Vector2 value))
                        {
                            return value.x <= -InputMap.Axis2DLeftThreshold;
                        }
                    }
                    break;
                case ButtonReadType.Axis2DRight:
                    {
                        if (device.TryGetFeatureValue(new InputFeatureUsage<Vector2>(info.name), out Vector2 value))
                        {
                            return value.x >= InputMap.Axis2DRighThreshold;
                        }
                    }
                    break;
            }

            return false;
        }
    }
}