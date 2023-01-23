using System.Collections.Generic;
using HurricaneVR.Framework.Core.Grabbers;
using HurricaneVR.Framework.Core.Player;
using UnityEngine;

namespace HurricaneVR.Framework.Core
{
    public class HVRManager : MonoBehaviour
    {
        public static HVRManager Instance { get; private set; }

        public HVRGrabberManager GrabberManager;
        public HVRPlayerController PlayerController;
        public GameObject HighlightPrefab;
        public Transform Camera;
        public HVRScreenFade ScreenFader { get; private set; }

        private void Awake()
        {
            if (!Instance)
            {
                Instance = this;
                DontDestroyOnLoad(this.gameObject);
                if (!GrabberManager)
                {
                    GrabberManager = gameObject.AddComponent<HVRGrabberManager>();
                }
            }
            else
            {
                Destroy(this);
            }

            var finder = FindObjectOfType<HVRGlobalFadeFinder>();
            if (finder)
            {
                ScreenFader = finder.gameObject.GetComponent<HVRScreenFade>();
            }

            if (!PlayerController)
            {
                PlayerController = FindObjectOfType<HVRPlayerController>();
            }
        }

        public void RegisterGrabber(HVRGrabberBase grabber)
        {
            if (GrabberManager)
            {
                GrabberManager.RegisterGrabber(grabber);
            }
        }

        public void UnregisterGrabber(HVRGrabberBase grabber)
        {
            if (GrabberManager)
            {
                GrabberManager.UnregisterGrabber(grabber);
            }
        }

        public GameObject SetupHighlight(Transform target)
        {
            if (!HighlightPrefab)
                return null;

            var obj = Instantiate(HighlightPrefab);
            obj.transform.SetParent(target.transform, false);
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;
            return obj;
        }

        public void IgnorePlayerCollision(IEnumerable<Collider> colliders)
        {
            if (PlayerController)
                PlayerController.IgnoreCollision(colliders);
        }

        public void ScreenFade(float alpha, float speed)
        {
            if(ScreenFader)
                ScreenFader.Fade(alpha, speed);
        }
    }
}
