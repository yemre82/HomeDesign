using System;
using UnityEngine;

namespace HurricaneVR.Framework.Shared
{
    [CreateAssetMenu(menuName = "HurricaneVR/Input Settings", fileName = "InputSettings")]
    public class HVRInputSettings : ScriptableObject
    {
        public InputAxes JoystickAxis;
        public InputAxes TrackPadAxis;
        public HVRXRInputFeatures Primary = HVRXRInputFeatures.PrimaryButton;
        public HVRXRInputFeatures Secondary = HVRXRInputFeatures.SecondaryButton;
        public HVRXRInputFeatures Menu = HVRXRInputFeatures.MenuButton;
        public HVRXRInputFeatures PrimaryTouch = HVRXRInputFeatures.PrimaryTouch;
        public HVRXRInputFeatures SecondaryTouch = HVRXRInputFeatures.SecondaryTouch;
        public HVRXRInputFeatures JoystickButton = HVRXRInputFeatures.Primary2DAxisClick;
        public HVRXRInputFeatures TrackPadButton = HVRXRInputFeatures.Secondary2DAxisClick;

        public HVRXRInputFeatures JoystickTouch = HVRXRInputFeatures.Primary2DAxisTouch;
        public HVRXRInputFeatures TrackPadTouch = HVRXRInputFeatures.Secondary2DAxisTouch;

        public float GripThreshold = .7f;
        public float TriggerThreshold = .7f;

        public float Axis2DUpThreshold = .7f;
        public float Axis2DDownThreshold = .7f;
        public float Axis2DLeftThreshold = .7f;
        public float Axis2DRighThreshold = .7f;

        public bool GripUseEither = false;
        public bool GripUseAnalog = true;
        public bool TriggerUseAnalog = true;

        public Vector3 ControllerPositionOffset;
        public Vector3 ControllerRotationOffset;
    }

    [Serializable]
    public enum InputAxes
    {
        None,
        Primary2DAxis = 1,
        Secondary2DAxis = 2,
    };


}