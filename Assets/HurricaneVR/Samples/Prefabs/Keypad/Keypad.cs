using HurricaneVR.Framework.Components;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace HurricaneVR.Samples.Prefabs.Keypad
{
    public class Keypad : MonoBehaviour
    {
        public UnityEvent Unlocked = new UnityEvent();

        public string Code;
        public TextMeshPro Display;
        public string Entry = "";


        public KeyPadButton LeftActive;
        public KeyPadButton RightActive;

        public int Index => Entry?.Length ?? 0;

        public int MaxLength;

        private bool _unlocked;

        protected virtual void Start()
        {
            var buttons = GetComponentsInChildren<KeyPadButton>();
            var colliders = GetComponentsInChildren<Collider>();
            foreach (var keyCollider in colliders)
            {
                foreach (var ourCollider in GetComponents<Collider>())
                {
                    Physics.IgnoreCollision(ourCollider, keyCollider);
                }
            }

            for (int i = 1; i <= buttons.Length; i++)
            {
                var button = buttons[i - 1];
                button.ButtonDown.AddListener(OnButtonDown);
                if (i >= 1 && i <= 9)
                {
                    button.Key = i.ToString()[0];
                }
                else if (i == 11)
                {
                    button.Key = '0';
                }
                else if (i == 10)
                {
                    button.Key = '<';
                }
                else if (i == 12)
                {
                    button.Key = '+';
                }

                button.Text.text = button.Key.ToString();
            }

            Entry = "";
            if (Display)
            {
                Display.text = "******";
            }
        }

        protected virtual void OnButtonDown(HVRButton button)
        {
            var keyPadButton = button as KeyPadButton;

            if (keyPadButton.Key == '<')
            {
                if (Entry.Length > 0)
                {
                    Entry = Entry.Substring(0, Entry.Length - 1);
                }
                else
                {
                    return;
                }
            }
            else if (keyPadButton.Key == '+')
            {
                if (Code == Entry)
                {
                    if (!_unlocked)
                        Unlocked.Invoke();
                    _unlocked = true;
                }
            }
            else if (Index >= 0 && Index < MaxLength)
            {
                Entry += keyPadButton.Key;
            }

            if (Display)
            {
                Display.text = Entry.PadLeft(6, '*');
            }
        }

        // Update is called once per frame
        void Update()
        {

        }


    }
}
