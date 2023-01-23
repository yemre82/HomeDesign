using UnityEngine;

namespace HurricaneVR.Framework.Shared
{
    //https://www.cloudwalkingames.com/en/scriptable-objects/fingersettings
    [CreateAssetMenu(menuName = "HurricaneVR/Finger Settings", fileName = "FingerSettings")]
    public class HVRFingerSettings : ScriptableObject
    {
        [Header("Non Knuckles SteamVR Finger Curl Overrides")]
        public bool OverrideThumb = true;
        public bool OverrideTrigger = true;
        public bool OverrideTriggerGrab = true; //clicky controllers bend on trigger pull, is this desirable?

        [Header("Knuckles Overrides")]
        public bool KnucklesOverrideThumb = true;
        public bool KnucklesOverrideTrigger;

        [Header("Touch Weights")]
        [Range(0, 1)]
        public float JoystickTouchWeight = 0f;
        [Range(0, 1)]
        public float TrackpadTouchWeight = 1f;
        [Range(0, 1)]
        public float PrimaryTouchWeight = 1f;
        [Range(0, 1)]
        public float SecondaryTouchWeight = 1f;
        [Range(0, 1)]
        public float TriggerTouchWeight = .65f; //steamvr default .65f

        public void SetDefaults()
        {
            OverrideThumb = true;
            OverrideTrigger = true;

            JoystickTouchWeight = 1f;
            TrackpadTouchWeight = 1f;
            PrimaryTouchWeight = 1f;
            SecondaryTouchWeight = 1f;
            TriggerTouchWeight = .65f;
        }
    }
}