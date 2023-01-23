using System;
using System.Collections;
using HurricaneVR.Framework.Core;
using HurricaneVR.Framework.Shared;
using HurricaneVR.Framework.Shared.HandPoser;
using HurricaneVR.Framework.Shared.HandPoser.Data;
using UnityEngine;

namespace HurricaneVR.Framework.Components
{
    public class HVRHandPoseRecorder : MonoBehaviour
    {
        public KeyCode LeftHandSaveKey = KeyCode.L;
        public KeyCode RightHandSaveKey = KeyCode.R;

        public HVRPosableHand LeftHand;
        public HVRPosableHand RightHand;

        public HVRHandPhysics LeftPhysics;
        public HVRHandPhysics RightPhysics;

        public float FadeTimer = 10f;
        public bool RemoveClones = true;

        public bool DisablePhysics;

        private bool _previousDisable;

        public string Folder;
        public int Counter = 0;

        public void Start()
        {
            Folder = DateTime.Now.ToString("yyyyMMdd_HH_mm");
        }

        void Update()
        {
#if UNITY_EDITOR

            if (DisablePhysics && !_previousDisable)
            {
                if (LeftPhysics)
                {
                    LeftPhysics.DisableCollision();
                }

                if (RightPhysics)
                {
                    RightPhysics.DisableCollision();
                }
            }
            else if (!DisablePhysics && _previousDisable)
            {
                if (LeftPhysics)
                {
                    LeftPhysics.EnableCollision();
                }

                if (RightPhysics)
                {
                    RightPhysics.EnableCollision();
                }
            }

            _previousDisable = DisablePhysics;

            CheckSnapshot();
#endif
        }

        private void CheckSnapshot()
        {
            HVRPosableHand hand;

            if (Input.GetKeyDown(LeftHandSaveKey))
            {
                hand = LeftHand;
            }
            else if (Input.GetKeyDown(RightHandSaveKey))
            {
                hand = RightHand;
            }
            else
                return;

            if (!hand)
                return;

            Snapshot(hand);
        }

        public void SnapshotLeft()
        {
            if (!gameObject.activeSelf)
                return;
            if (LeftHand)
            {
                Snapshot(LeftHand);
            }
        }

        public void SnapshotRight()
        {
            if (!gameObject.activeSelf)
                return;

            if (RightHand)
            {
                Snapshot(RightHand);
            }
        }

        private void Snapshot(HVRPosableHand hand)
        {

#if UNITY_EDITOR
            var pose = hand.CreateFullHandPoseWorld(hand.MirrorAxis);

            HVRSettings.Instance.SaveRunTimePose(pose, Counter++.ToString(), Folder);

            var clone = Instantiate(HVRSettings.Instance.GetPoserHand(hand.Side));

            var posableHand = clone.GetComponent<HVRPosableHand>();
            if (posableHand != null)
            {
                posableHand.Pose(pose.GetPose(hand.Side));
                clone.transform.position = hand.transform.position;
                clone.transform.rotation = hand.transform.rotation;
            }

            var colliders = clone.GetComponentsInChildren<Collider>();
            foreach (var c in colliders)
            {
                c.enabled = false;
            }

            if (RemoveClones)
            {
                StartCoroutine(RemoveClone(clone));
            }
#endif
        }

        public IEnumerator RemoveClone(GameObject clone)
        {
            yield return new WaitForSeconds(FadeTimer);
            Destroy(clone);
        }
    }
}
