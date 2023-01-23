using TMPro;
using UnityEngine;

namespace HurricaneVR.Samples
{
    public class KeypadDisplay : MonoBehaviour
    {
        public TextMeshPro Display;
        public void Unlocked()
        {
            if (Display)
                Display.text = "Unlocked!";
        }
    }
}
