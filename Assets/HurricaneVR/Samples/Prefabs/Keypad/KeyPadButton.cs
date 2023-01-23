using HurricaneVR.Framework.Components;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace HurricaneVR.Samples.Prefabs.Keypad
{
    [RequireComponent(typeof(AudioSource))]
    public class KeyPadButton : HVRButton
    {
        [FormerlySerializedAs("Number")]
        public char Key;
        public TextMeshPro Text;
        public TextMeshPro Ring;
    }
}
