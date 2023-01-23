using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.HurricaneVR.Framework.Shared.Utilities;
using HurricaneVR.Framework.Components;
using HurricaneVR.Framework.ControllerInput;
using HurricaneVR.Framework.Core.Bags;
using HurricaneVR.Framework.Core.Player;
using HurricaneVR.Framework.Core.Utils;
using HurricaneVR.Framework.Shared;
using HurricaneVR.Framework.Shared.HandPoser;
using HurricaneVR.Framework.Shared.HandPoser.Data;
using HurricaneVR.Framework.Shared.Utilities;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace HurricaneVR.Framework.Core.Grabbers
{
    public class HVRHandGrabber : HVRGrabberBase
    {
        internal const int TrackedVelocityCount = 10;


        [Tooltip("HVRSocketBag used for placing and removing from sockets")]
        public HVRSocketBag SocketBag;

        [Header("HandSettings")]
        [Tooltip("Set to true if the HandModel is an IK target")]
        public bool InverseKinematics;

        [Header("Grab Settings")]

        [Tooltip("If true the hand will move to the grabbable instead of pulling the grabbable to the hand")]
        public bool HandGrabs;
        [Tooltip("Hand move speed when HandGrabs = true")]
        public float HandGrabSpeed = 5f;


        [Tooltip("If in a networked game, can someone take this an object from your hand?")]
        public bool AllowMultiplayerSwap;


        [Tooltip("Hold down or Toggle grabbing")]
        public HVRGrabTrigger GrabTrigger = HVRGrabTrigger.Active;
        [Tooltip("Left or right hand.")]
        public HVRHandSide HandSide;



        [Tooltip("If true the hand model will be cloned, collider removed, and used when parenting to the grabbable")]
        public bool CloneHandModel = true;

        [Tooltip("Vibration strength when hovering over something you can pick up.")]
        public float HapticsAmplitude = .1f;
        [Tooltip("Vibration durection when hovering over something you can pick up.")]
        public float HapticsDuration = .1f;

        [Tooltip("Ignores hand model parenting distance check.")]
        public bool IgnoreParentingDistance;
        [Tooltip("Ignores hand model parenting angle check.")]
        public bool IgnoreParentingAngle;

        [Tooltip("Angle to meet before hand model parents to the grabbable.")]
        public float ParentingMaxAngleDelta = 20f;
        [Tooltip("Distance to meet before hand model parents to the grabbable")]
        public float ParentingMaxDistance = .01f;

        [Tooltip("Settings used to pull and rotate the object into position")]
        public HVRJointSettings PullingSettings;

        [Tooltip("Layer mask to determine line of sight to the grabbable.")]
        public LayerMask RaycastLayermask;

        [Header("Components")]

        [Tooltip("The hand animator component, loads from children on startup if not supplied.")]
        public HVRHandAnimator HandAnimator;

        [Tooltip("Component that holds collider information about the hands. Auto populated from children if not set.")]
        public HVRHandPhysics HandPhysics;
        public HVRPlayerInputs Inputs;
        public HVRPhysicsPoser PhysicsPoser;
        public HVRForceGrabber ForceGrabber;
        public HVRGrabbableHoverBase GrabIndicator;
        public HVRGrabbableHoverBase TriggerGrabIndicator;
        public HVRControllerOffset ControllerOffset;

        [Tooltip("Default hand pose to fall back to.")]
        public HVRHandPoser FallbackPoser;

        [Header("Required Transforms")]

        [Tooltip("Object holding the hand model.")]
        public Transform HandModel;

        [Tooltip("Configurable joints are anchored here")]
        public Transform JointAnchor;
        [Tooltip("Used to shoot ray casts at the grabbable to check if there is line of sight before grabbing.")]
        public Transform RaycastOrigin;
        [Tooltip("The transform that is handling device tracking.")]
        public Transform TrackedController;

        [Tooltip("Physics hand that will prevent the grabber from going through walls while you're holding something.")]
        public Transform InvisibleHand;

        [Tooltip("Sphere collider that checks when collisions should be re-enabled between a released grabbable and this hand.")]
        public Transform OverlapSizer;

        [Header("Throw Settings")]
        [Tooltip("Factor to apply to the linear velocity of the throw.")]
        public float ReleasedVelocityFactor = 0f;

        [Tooltip("Factor to apply to the angular to linear calculation.")]
        public float ReleasedAngularConversionFactor = 1.0f;

        [Tooltip("Hand angular velocity must exceed this to add linear velocity based on angular velocity.")]
        public float ReleasedAngularThreshold = 1f;

        [Tooltip("Number of frames to average velocity for throwing.")]
        public int ThrowLookback = 5;

        [Tooltip("Number of frames to skip while averaging velocity.")]
        public int ThrowLookbackStart = 0;


        [Tooltip("If true throwing takes only the top peak velocities for throwing.")]
        public bool TakePeakVelocities;
        [DrawIf("TakePeakVelocities", true)]
        public int CountPeakVelocities = 3;

        [Tooltip("Uses the center of mass that should match with current controller type you are using.")]
        public HVRThrowingCenterOfMass ThrowingCenterOfMass;



        [Header("Debugging")]

        [Tooltip("If enabled displays vectors involved in throwing calculation.")]
        public bool DrawCenterOfMass;
        public bool GrabToggleActive;
        [SerializeField]
        private HVRGrabbable _triggerHoverTarget;
        public HVRSocket HoveredSocket;
        [SerializeField]
        private HVRGrabbable _hoverTarget;

        private HVRGrabbableHoverBase _grabIndicator;
        private HVRGrabbableHoverBase _triggerIndicator;

        public override bool IsHandGrabber => true;

        public HVRPhysicsHands PhysicsHands { get; private set; }
        public Transform HandModelParent { get; private set; }
        public Vector3 HandModelPosition { get; private set; }
        public Quaternion HandModelRotation { get; private set; }
        public Vector3 HandModelScale { get; private set; }

        public HVRRigidBodyOverrides RigidOverrides { get; private set; }

        public Collider[] InvisibleHandColliders { get; private set; }

        public Dictionary<HVRGrabbable, Coroutine> OverlappingGrabbables = new Dictionary<HVRGrabbable, Coroutine>();

        public GameObject TempGrabPoint { get; internal set; }

        public HVRController Controller => HandSide == HVRHandSide.Left ? HVRInputManager.Instance.LeftController : HVRInputManager.Instance.RightController;

        public Transform HandGraphics => _handClone ? _handClone : HandModel;

        public bool IsLineGrab { get; private set; }



        public HVRGrabbable TriggerHoverTarget
        {
            get { return _triggerHoverTarget; }
            set
            {
                _triggerHoverTarget = value;
                IsTriggerHovering = value;
            }
        }

        public bool IsTriggerHovering { get; private set; }

        public HVRTrackedController HVRTrackedController { get; private set; }

        public override Transform GrabPoint
        {
            get => base.GrabPoint;
            set
            {
                if (!value)
                {
                    PosableGrabPoint = null;
                }
                else if (GrabPoint != value)
                {
                    PosableGrabPoint = value.GetComponent<HVRPosableGrabPoint>();
                }

                base.GrabPoint = value;
            }
        }


        public HVRPosableGrabPoint PosableGrabPoint { get; private set; }

        private Transform _triggerGrabPoint;
        public Transform TriggerGrabPoint
        {
            get => _triggerGrabPoint;
            set
            {
                if (!value)
                {
                    TriggerPosableGrabPoint = null;
                }
                else if (GrabPoint != value)
                {
                    TriggerPosableGrabPoint = value.GetComponent<HVRPosableGrabPoint>();
                }

                _triggerGrabPoint = value;
            }
        }


        public HVRPosableGrabPoint TriggerPosableGrabPoint { get; private set; }



        public Quaternion PoseWorldRotation
        {
            get
            {
                if (PosableGrabPoint)
                {
                    return PosableGrabPoint.GetPoseRotation(HandSide);
                }

                if (IsPhysicsPose)
                {
                    return GrabPoint.rotation * PhysicsHandRotation;
                }
                return GrabPoint.rotation;
            }
        }

        public Vector3 PoseWorldPosition
        {
            get
            {
                if (PosableGrabPoint) return PosableGrabPoint.transform.TransformPoint(PosableGrabPoint.GetPosePositionOffset(HandSide));
                if (IsPhysicsPose)
                {
                    return GrabPoint.position + PhysicsHandPosition;
                }

                return GrabPoint.position;
            }
        }




        internal Quaternion PhysicsHandRotation { get; set; }
        internal Vector3 PhysicsHandPosition { get; set; }
        internal byte[] PhysicsPoseBytes { get; private set; }



        public override Quaternion ControllerRotation => TrackedController.rotation;

        public Transform Palm => PhysicsPoser.Palm;

        public bool IsClimbing { get; private set; }



        public bool IsPhysicsPose { get; set; }

        public Vector3 GrabAnchorLocal { get; private set; }

        public Vector3 GrabAnchorWorld
        {
            get
            {
                if (GrabbedTarget.Rigidbody && _configurableJoint)
                {
                    return GrabbedTarget.Rigidbody.transform.TransformPoint(_configurableJoint.anchor);
                }

                if (GrabPoint)
                {
                    return GrabPoint.position;
                }
                return GrabbedTarget.transform.position;
            }
        }

        public override Vector3 JointAnchorWorldPosition => JointAnchor.position;

        public Vector3 JointAnchorWorld => transform.TransformPoint(HandAnchorLocal);

        public Vector3 HandAnchorLocal { get; private set; }

        public bool IsHoveringSocket => HoveredSocket;

        public Quaternion HandWorldRotation => transform.rotation * HandModelRotation;

        public readonly CircularBuffer<Vector3> RecentVelocities = new CircularBuffer<Vector3>(TrackedVelocityCount);
        public readonly CircularBuffer<Vector3> RecentAngularVelocities = new CircularBuffer<Vector3>(TrackedVelocityCount);

        public bool CanActivate { get; private set; }

        #region Private

        private SphereCollider _overlapCollider;
        private readonly Collider[] _overlapColliders = new Collider[1000];
        private readonly List<Tuple<Collider, Vector3, float>> _physicsGrabPoints = new List<Tuple<Collider, Vector3, float>>();
        private readonly List<Tuple<GrabPointMeta, float>> _grabPoints = new List<Tuple<GrabPointMeta, float>>();
        private bool _hasHandModelParented;
        private Quaternion _previousRotation = Quaternion.identity;
        private float _pullingTimer;
        private SkinnedMeshRenderer _mainSkin;
        private SkinnedMeshRenderer _copySkin;
        private Transform _handClone;
        private HVRHandAnimator _handCloneAnimator;
        internal ConfigurableJoint _configurableJoint;
        private Transform _handOffset;
        private Transform _fakeHand;
        private Transform _fakeHandAnchor;
        private bool _isForceAutoGrab;
        private Vector3 _lineOffset;
        private bool _tightlyHeld;
        private bool _flipPose;
        private Quaternion _startRotation;
        private bool _primaryGrabPointGrab;
        private HVRPosableHand _posableHand;
        private HVRPosableHand _clonePosableHand;
        private bool _hasForceGrabber;
        private HVRHandPoseData _physicsPose;
        private bool _lateUpdatePose;

        protected bool IsGripGrabActivated;
        protected bool IsTriggerGrabActivated;
        protected bool IsGripGrabActive;
        protected bool IsTriggerGrabActive;

        private bool _checkingSwap;

        #endregion

        protected virtual void Awake()
        {
            if (TrackedController)
                HVRTrackedController = TrackedController.GetComponent<HVRTrackedController>();

            RigidOverrides = GetComponent<HVRRigidBodyOverrides>();
        }

        protected override void Start()
        {
            base.Start();

            PhysicsHands = GetComponent<HVRPhysicsHands>();

            if (!Inputs)
            {
                Inputs = GetComponentInParent<HVRPlayerInputs>();
            }

            if (!ForceGrabber)
            {
                ForceGrabber = GetComponentInChildren<HVRForceGrabber>();
                _hasForceGrabber = ForceGrabber;
            }

            if (!HandAnimator)
            {
                if (HandModel)
                {
                    HandAnimator = HandModel.GetComponentInChildren<HVRHandAnimator>();
                }
                else
                {
                    HandAnimator = GetComponentInChildren<HVRHandAnimator>();
                }
            }

            if (!PhysicsPoser)
            {
                if (HandModel)
                {
                    PhysicsPoser = HandModel.GetComponentInChildren<HVRPhysicsPoser>();
                }
                else
                {
                    PhysicsPoser = GetComponentInChildren<HVRPhysicsPoser>();
                }
            }

            if (!HandPhysics)
            {
                HandPhysics = GetComponentInChildren<HVRHandPhysics>();
            }



            if (HandModel)
            {
                if (!HandPhysics.PhysicsHand && !InverseKinematics)
                {
                    HandPhysics.PhysicsHand = HandModel;
                    HandPhysics.SetupColliders();
                }

                _posableHand = HandModel.GetComponent<HVRPosableHand>();
                HandModelParent = HandModel.parent;
                HandModelPosition = HandModel.localPosition;
                HandModelRotation = HandModel.localRotation;
                HandModelScale = HandModel.localScale;

                if (InverseKinematics && CloneHandModel)
                {
                    Debug.Log($"CloneHandModel set to false, VRIK is enabled.");
                    CloneHandModel = false;
                }

                if (CloneHandModel)
                {
                    var handClone = Instantiate(HandModel.gameObject);
                    foreach (var col in handClone.GetComponentsInChildren<Collider>().ToArray())
                    {
                        Destroy(col);
                    }

                    _handClone = handClone.transform;
                    _handClone.parent = transform;
                    _mainSkin = HandModel.GetComponentInChildren<SkinnedMeshRenderer>();
                    _copySkin = _handClone.GetComponentInChildren<SkinnedMeshRenderer>();
                    _copySkin.enabled = false;
                    _handCloneAnimator = _handClone.GetComponentInChildren<HVRHandAnimator>();
                    _clonePosableHand = _handClone.GetComponent<HVRPosableHand>();
                }

                ResetRigidBodyProperties();

                var go = new GameObject("FakeHand");
                go.transform.parent = transform;
                go.transform.localPosition = HandModelPosition;
                go.transform.localRotation = HandModelRotation;
                _fakeHand = go.transform;

                go = new GameObject("FakeHandJointAnchor");
                go.transform.parent = _fakeHand;
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                _fakeHandAnchor = go.transform;

                go = new GameObject("HandOffset");
                go.transform.parent = transform;
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                _handOffset = go.transform;
            }

            if (InvisibleHand)
            {
                InvisibleHandColliders = InvisibleHand.gameObject.GetComponentsInChildren<Collider>().Where(e => !e.isTrigger).ToArray();
            }

            if (InvisibleHandColliders != null)
            {
                HandPhysics.IgnoreCollision(InvisibleHandColliders, true);
            }

            if (OverlapSizer)
            {
                _overlapCollider = OverlapSizer.GetComponent<SphereCollider>();
            }

            if (!SocketBag)
                SocketBag = GetComponentInChildren<HVRSocketBag>();

            if (!ThrowingCenterOfMass)
                ThrowingCenterOfMass = GetComponentInChildren<HVRThrowingCenterOfMass>();

            ResetTrackedVelocities();

            if (!ControllerOffset)
            {
                if (TrackedController)
                {
                    ControllerOffset = TrackedController.GetComponentInChildren<HVRControllerOffset>();
                }
            }
        }

        protected override void Update()
        {
            if (PerformUpdate)
            {
                CheckCanActivate();
                CheckTriggerActivate();
                UpdateGrabIndicator();
                UpdateTriggerGrabIndicator();
            }

            if (PerformUpdate)
            {
                CheckBreakDistance();
                TrackVelocities();

                UpdateGrabInputs();
                CheckGrabControlSwap();
                CheckUntoggleGrab();
                IsHoldActive = UpdateHolding();
                CheckSocketUnhover();
                CheckSocketHover();
                CheckUnHover();
                CheckTriggerUnHover();
                CheckRelease();
                CheckHover();
                CheckTriggerHover();
                CheckGrab();
            }

            UpdatePose();
            CheckPoseHand();

            _previousRotation = transform.rotation;
            _hoverTarget = HoverTarget;
        }

        protected override void FixedUpdate()
        {
            CheckPullingGrabbable();
            UpdateLineGrab();
        }

        protected virtual void LateUpdate()
        {
            if (InverseKinematics && IsPhysicsPose && _physicsPose != null)
            {
                HandAnimator.Hand.Pose(_physicsPose, false);
            }
        }

        private void CheckGrabControlSwap()
        {
            if (!_checkingSwap)
                return;

            if (_grabbableControl == _currentGrabControl)
            {
                _checkingSwap = false;
                return;
            }

            //checking for socket to grabbable grab button changes
            if (_grabbableControl == HVRGrabControls.GripOnly && _currentGrabControl == HVRGrabControls.TriggerOnly)
            {
                if (IsGripGrabActive && !IsTriggerGrabActive)
                {
                    _currentGrabControl = HVRGrabControls.GripOnly;
                    _checkingSwap = false;
                }
            }
            else if (_grabbableControl == HVRGrabControls.GripOnly && _currentGrabControl == HVRGrabControls.GripOrTrigger)
            {
                if (IsGripGrabActive && !IsTriggerGrabActive)
                {
                    _currentGrabControl = HVRGrabControls.GripOnly;
                    _checkingSwap = false;
                }
            }
            else if (_grabbableControl == HVRGrabControls.TriggerOnly && _currentGrabControl == HVRGrabControls.GripOnly)
            {
                if (IsTriggerGrabActive && !IsGripGrabActive)
                {
                    _currentGrabControl = HVRGrabControls.TriggerOnly;
                    _checkingSwap = false;
                }
            }
            else if (_grabbableControl == HVRGrabControls.TriggerOnly && _currentGrabControl == HVRGrabControls.GripOrTrigger)
            {
                if (IsTriggerGrabActive && !IsGripGrabActive)
                {
                    _currentGrabControl = HVRGrabControls.TriggerOnly;
                    _checkingSwap = false;
                }
            }
            else if (_grabbableControl == HVRGrabControls.GripOrTrigger && _currentGrabControl == HVRGrabControls.TriggerOnly)
            {
                if (IsGripGrabActive && !IsTriggerGrabActive || (GrabToggleActive && !IsTriggerGrabActive && !IsGripGrabActive))
                {
                    _currentGrabControl = HVRGrabControls.GripOrTrigger;
                    _checkingSwap = false;
                }
            }
            else if (_grabbableControl == HVRGrabControls.GripOrTrigger && _currentGrabControl == HVRGrabControls.GripOnly)
            {
                if (IsTriggerGrabActive && !IsGripGrabActive || (GrabToggleActive && !IsTriggerGrabActive && !IsGripGrabActive))
                {
                    _currentGrabControl = HVRGrabControls.GripOrTrigger;
                    _checkingSwap = false;
                }
            }
        }

        private void CheckTriggerActivate()
        {
            if (IsGrabbing && CanActivate)
            {
                if (Controller.TriggerButtonState.JustActivated)
                {
                    GrabbedTarget.InternalOnActivate(this);
                }
                else if (Controller.TriggerButtonState.JustDeactivated)
                {
                    GrabbedTarget.InternalOnDeactivate(this);
                }
            }
        }

        private void UpdatePose()
        {
            if (!IsLineGrab && IsGrabbing && GrabbedTarget.Stationary && !GrabbedTarget.ParentHandModel && _hasHandModelParented)
            {
                HandModel.rotation = PoseWorldRotation;
                HandModel.position = PoseWorldPosition;
            }
        }

        protected void ResetTrackedVelocities()
        {
            for (var i = 0; i < TrackedVelocityCount; i++)
            {
                RecentVelocities.Enqueue(Vector3.zero);
                RecentAngularVelocities.Enqueue(Vector3.zero);
            }
        }

        private void DetermineGrabPoint(HVRGrabbable grabbable)
        {
            if (IsGrabbing)
                return;

            GrabPoint = GetGrabPoint(grabbable);
        }

        internal Transform GetGrabPoint(HVRGrabbable grabbable)
        {
            for (int i = 0; i < grabbable.GrabPointsMeta.Count; i++)
            {
                var grabPoint = grabbable.GrabPointsMeta[i];
                if (!grabPoint.GrabPoint)
                {
                    continue;
                }

                var angleDelta = 0f;
                var posableGrabPoint = grabPoint.PosableGrabPoint;
                Vector3 grabbableWorldAnchor;
                if (posableGrabPoint != null)
                {
                    if (HandSide == HVRHandSide.Left && !posableGrabPoint.LeftHand ||
                        HandSide == HVRHandSide.Right && !posableGrabPoint.RightHand)
                    {
                        continue;
                    }

                    var poseRotation = posableGrabPoint.GetPoseRotation(HandSide);

                    angleDelta = Quaternion.Angle(HandWorldRotation, poseRotation);
                    if (angleDelta > posableGrabPoint.AllowedAngleDifference)
                    {
                        continue;
                    }

                    grabbableWorldAnchor = grabPoint.GrabPoint.position;
                    //grabbableWorldAnchor = CalculateGrabPointWorldAnchor(grabbable, posableGrabPoint);
                }
                else
                {
                    grabbableWorldAnchor = grabPoint.GrabPoint.position;
                }

                var distance = Vector3.Distance(grabbableWorldAnchor, JointAnchorWorldPosition);
                if (grabbable.ConsiderGrabPointAngle)
                    distance += angleDelta;

                _grabPoints.Add(new Tuple<GrabPointMeta, float>(grabPoint, distance));
            }


            if (_grabPoints.Count > 0)
            {
                _grabPoints.Sort((x, y) => x.Item2.CompareTo(y.Item2));
                var temp = _grabPoints[0].Item1.GrabPoint;
                _grabPoints.Clear();
                return temp;
            }

            return null;
        }



        private void CheckCanActivate()
        {
            if (!CanActivate && !IsTriggerGrabActive)
            {
                CanActivate = true;
            }
        }

        protected override void CheckUnHover()
        {
            if (!HoverTarget)
                return;

            var closestValid = ClosestValidHover(false);

            if (!CanHover(HoverTarget) || closestValid != HoverTarget)
            {
                UnhoverGrabbable(this, HoverTarget);
            }
        }

        protected override bool CheckHover()
        {
            if (IsHovering || !AllowHovering)
            {
                if (IsHovering && !HoverTarget)
                {
                    HoverTarget = null;
                }
                else
                {
                    return true;
                }
            }

            var closestValid = ClosestValidHover(false);
            if (closestValid == null)
                return false;

            HoverGrabbable(this, closestValid);
            return true;
        }

        protected virtual void CheckTriggerUnHover()
        {
            if (!TriggerHoverTarget)
                return;

            var closestValid = ClosestValidHover(true);

            if (!CanHover(TriggerHoverTarget) || closestValid != TriggerHoverTarget)
            {
                OnTriggerHoverExit(this, TriggerHoverTarget);
            }
        }


        protected virtual bool CheckTriggerHover()
        {
            if (IsTriggerHovering || !AllowHovering)
            {
                if (IsTriggerHovering && !TriggerHoverTarget)
                {
                    TriggerHoverTarget = null;
                }
                else
                {
                    return true;
                }
            }

            var closestValid = ClosestValidHover(true);
            if (closestValid == null)
                return false;

            OnTriggerHoverEnter(this, closestValid);
            return true;
        }



        private void CheckUntoggleGrab()
        {
            if (GrabToggleActive && !_checkingSwap)
            {
                if (_currentGrabControl == HVRGrabControls.GripOrTrigger)
                {
                    if (!IsLineGrab && (IsGripGrabActivated || (IsTriggerGrabActivated && Inputs.CanTriggerGrab)))
                    {
                        GrabToggleActive = false;
                    }
                    else if (IsLineGrab && IsGripGrabActivated && !IsTriggerGrabActive)
                    {
                        //if line grab and trigger is pressed - don't allow untoggle
                        GrabToggleActive = false;
                    }
                }
                else if (_currentGrabControl == HVRGrabControls.TriggerOnly && IsTriggerGrabActivated)
                {
                    GrabToggleActive = false;
                }
                else if (_currentGrabControl == HVRGrabControls.GripOnly && IsGripGrabActivated)
                {
                    GrabToggleActive = false;
                }

                if (!GrabToggleActive)
                {
                    IsGripGrabActivated = false;
                    IsTriggerGrabActivated = false;
                }
            }
        }

        private HVRGrabControls _currentGrabControl;
        private HVRGrabControls _grabbableControl;

        private bool UpdateHolding()
        {
            if (!IsGrabbing)
                return false;

            var grabTrigger = GrabTrigger;

            if (GrabbedTarget.OverrideGrabTrigger)
            {
                grabTrigger = GrabbedTarget.GrabTrigger;
            }

            switch (grabTrigger)
            {
                case HVRGrabTrigger.Active:
                    {
                        if (GrabToggleActive)
                        {
                            return true;
                        }

                        if (IsLineGrab)
                        {
                            return IsGripGrabActive || IsTriggerGrabActive;
                        }

                        var grabActive = false;
                        switch (_currentGrabControl)
                        {
                            case HVRGrabControls.GripOrTrigger:
                                grabActive = IsGripGrabActive || (IsTriggerGrabActive && Inputs.CanTriggerGrab);
                                break;
                            case HVRGrabControls.GripOnly:
                                grabActive = IsGripGrabActive;
                                break;
                            case HVRGrabControls.TriggerOnly:
                                grabActive = IsTriggerGrabActive;
                                break;
                        }

                        return grabActive;
                    }
                case HVRGrabTrigger.Toggle:
                    {
                        return GrabToggleActive;
                    }
                case HVRGrabTrigger.ManualRelease:
                    return true;
            }

            return false;
        }

        protected override void CheckGrab()
        {

            if (!AllowGrabbing || IsGrabbing || GrabbedTarget)
            {
                return;
            }

            if (HoveredSocket && CanGrabFromSocket(HoveredSocket) && GrabActivated(HoveredSocket.GrabControl))
            {
                _primaryGrabPointGrab = true;

                if (TryGrab(HoveredSocket.GrabbedTarget, true))
                {
                    _currentGrabControl = HoveredSocket.GrabControl;

                    HoveredSocket.OnHandGrabberExited();
                    HoveredSocket = null;
                    //Debug.Log($"grabbed from socket directly");
                }
            }

            if (HoverTarget)
            {
                var grabControl = HoverTarget.GrabControl;
                if (HoverTarget.IsSocketed)
                    grabControl = HoverTarget.Socket.GrabControl;

                if (GrabActivated(grabControl) && TryGrab(HoverTarget))
                {
                    _currentGrabControl = grabControl;
                    return;
                }
            }

            if (TriggerHoverTarget)
            {
                var grabControl = TriggerHoverTarget.GrabControl;
                if (TriggerHoverTarget.IsSocketed)
                    grabControl = TriggerHoverTarget.Socket.GrabControl;
                if (GrabActivated(grabControl) && TryGrab(TriggerHoverTarget))
                {
                    _currentGrabControl = grabControl;
                    return;
                }
            }
        }


        private void UpdateGrabInputs()
        {
            IsTriggerGrabActivated = Inputs.GetTriggerGrabState(HandSide).JustActivated;
            IsGripGrabActivated = Inputs.GetGrabActivated(HandSide);

            IsTriggerGrabActive = Inputs.GetTriggerGrabState(HandSide).Active;
            IsGripGrabActive = Inputs.GetGripHoldActive(HandSide);
        }

        private bool GrabActivated(HVRGrabControls grabControl)
        {
            switch (grabControl)
            {
                case HVRGrabControls.GripOrTrigger:
                    return IsGripGrabActivated || (IsTriggerGrabActivated && Inputs.CanTriggerGrab);
                case HVRGrabControls.GripOnly:
                    return IsGripGrabActivated;
                case HVRGrabControls.TriggerOnly:
                    return IsTriggerGrabActivated;
            }

            return false;
        }


        private void UpdateGrabIndicator()
        {
            if (!IsHovering || !_grabIndicator)
                return;

            if (_grabIndicator.LookAtCamera && HVRManager.Instance.Camera)
            {
                _grabIndicator.transform.LookAt(HVRManager.Instance.Camera);
            }

            if (_grabIndicator.HoverPosition == HVRHoverPosition.Self)
                return;

            if (_grabIndicator.HoverPosition == HVRHoverPosition.GrabPoint)
                DetermineGrabPoint(HoverTarget);

            if (PosableGrabPoint && _grabIndicator.HoverPosition == HVRHoverPosition.GrabPoint)
            {
                _grabIndicator.transform.position = GetGrabIndicatorPosition(HoverTarget, PosableGrabPoint);
            }
            else
            {
                _grabIndicator.transform.position = HoverTarget.transform.position;
            }
        }

        private void UpdateTriggerGrabIndicator()
        {
            if (!IsTriggerHovering || !_triggerIndicator || IsGrabbing || TriggerHoverTarget == HoverTarget)
                return;

            if (_triggerIndicator.LookAtCamera && HVRManager.Instance.Camera)
            {
                _triggerIndicator.transform.LookAt(HVRManager.Instance.Camera);
            }

            if (_triggerIndicator.HoverPosition == HVRHoverPosition.Self)
                return;

            if (_triggerIndicator.HoverPosition == HVRHoverPosition.GrabPoint)
                TriggerGrabPoint = GetGrabPoint(TriggerHoverTarget);

            if (TriggerPosableGrabPoint && _triggerIndicator.HoverPosition == HVRHoverPosition.GrabPoint)
            {
                _triggerIndicator.transform.position = GetGrabIndicatorPosition(TriggerHoverTarget, TriggerPosableGrabPoint);
            }
            else
            {
                _triggerIndicator.transform.position = TriggerHoverTarget.transform.position;
            }
        }

        internal Vector3 GetGrabIndicatorPosition(HVRGrabbable grabbable, Transform grabPoint, bool useGrabPoint = false)
        {
            var posableGrabPoint = grabPoint.GetComponent<HVRPosableGrabPoint>();
            if (posableGrabPoint)
            {
                return GetGrabIndicatorPosition(grabbable, posableGrabPoint, useGrabPoint);
            }

            return grabPoint.position;
        }

        internal Vector3 GetGrabIndicatorPosition(HVRGrabbable grabbable, HVRPosableGrabPoint grabPoint, bool useGrabPoint = false)
        {
            if (grabPoint.IsLineGrab && !useGrabPoint)
            {
                var point = HVRUtilities.FindNearestPointOnLine(
                    grabPoint.LineStart.localPosition,
                    grabPoint.LineEnd.localPosition,
                    grabbable.transform.InverseTransformPoint(transform.TransformPoint(GetHandAnchor())));
                return grabbable.transform.TransformPoint(point);
            }

            if (grabPoint.GrabIndicatorPosition)
                return grabPoint.GrabIndicatorPosition.position;

            return grabPoint.transform.position;
        }

        protected override void OnHoverEnter(HVRGrabbable grabbable)
        {
            base.OnHoverEnter(grabbable);

            if (IsMine && !Mathf.Approximately(0f, HapticsDuration))
            {
                Controller.Vibrate(HapticsAmplitude, HapticsDuration);
            }

            if (grabbable.ShowGrabIndicator)
            {
                if (grabbable.GrabIndicator)
                {
                    _grabIndicator = grabbable.GrabIndicator;
                }
                else
                {
                    _grabIndicator = GrabIndicator;
                }

                if (_grabIndicator)
                {
                    _grabIndicator.Enable();
                    _grabIndicator.Hover();
                }
            }
        }

        protected override void OnHoverExit(HVRGrabbable grabbable)
        {
            base.OnHoverExit(grabbable);

            if (_grabIndicator)
            {
                _grabIndicator.Unhover();
                _grabIndicator.Disable();
            }
        }

        protected virtual void OnTriggerHoverEnter(HVRHandGrabber grabber, HVRGrabbable grabbable)
        {
            TriggerHoverTarget = grabbable;

            if (grabbable.ShowTriggerGrabIndicator)
            {
                if (grabbable.GrabIndicator)
                {
                    _triggerIndicator = grabbable.GrabIndicator;
                }
                else
                {
                    _triggerIndicator = TriggerGrabIndicator;
                }

                if (_triggerIndicator)
                {
                    _triggerIndicator.Enable();
                    _triggerIndicator.Hover();
                }
            }
        }

        protected virtual void OnTriggerHoverExit(HVRHandGrabber grabber, HVRGrabbable grabbable)
        {
            TriggerHoverTarget = null;

            if (_triggerIndicator)
            {
                _triggerIndicator.Unhover();
                _triggerIndicator.Disable();
            }
        }

        private void TrackVelocities()
        {
            var deltaRotation = transform.rotation * Quaternion.Inverse(_previousRotation);
            deltaRotation.ToAngleAxis(out var angle, out var axis);
            angle *= Mathf.Deg2Rad;
            var angularVelocity = axis * (angle * (1.0f / Time.fixedDeltaTime));

            RecentVelocities.Enqueue(Rigidbody.velocity);
            RecentAngularVelocities.Enqueue(angularVelocity);
        }

        private void CheckSocketUnhover()
        {
            if (!HoveredSocket)
                return;


            var swapSocket = ShouldSwapSocket();

            if (IsGrabbing || IsForceGrabbing || SocketBag.ClosestSocket == null || !SocketBag.ValidSockets.Contains(HoveredSocket) || swapSocket)
            {
                HoveredSocket.OnHandGrabberExited();
                HoveredSocket = null;
                //Debug.Log($"socket exited");
            }
        }

        protected virtual bool ShouldSwapSocket()
        {
            if (SocketBag.ClosestSocket && SocketBag.ClosestSocket != HoveredSocket)
            {
                return CanGrabFromSocket(SocketBag.ClosestSocket);
            }

            return false;
        }

        protected virtual bool CanGrabFromSocket(HVRSocket socket)
        {
            if (!socket)
            {
                return false;
            }

            if (!socket.CanGrabbableBeRemoved(this))
            {
                return false;
            }

            return socket.GrabDetectionType == HVRGrabDetection.Socket && socket.GrabbedTarget;
        }

        private void CheckSocketHover()
        {
            if (IsGrabbing || IsHoveringSocket || !SocketBag || IsForceGrabbing)
                return;

            for (var i = 0; i < SocketBag.ValidSockets.Count; i++)
            {
                var socket = SocketBag.ValidSockets[i];
                if (!CanGrabFromSocket(socket))
                    continue;

                HoveredSocket = socket;
                socket.OnHandGrabberEntered();
                break;
            }
        }

        private void CheckPullingGrabbable()
        {
            if (!IsGrabbing || !GrabPoint || !PullingGrabbable)
                return;

            _pullingTimer += Time.fixedDeltaTime;

            var distance = Vector3.Distance(JointAnchorWorld, GrabAnchorWorld);

            var angleDelta = Quaternion.Angle(PoseWorldRotation, HandWorldRotation);
            Vector3 worldLine;
            if (IsLineGrab && !_primaryGrabPointGrab)
            {
                worldLine = GrabbedTarget.transform.TransformDirection(PosableGrabPoint.Line.normalized) * (_flipPose ? -1f : 1f);
                angleDelta = Vector3.Angle(worldLine, transform.up);
            }

            var alreadyGrabbed = GrabbedTarget.GrabberCount > 1; //two handed grabs are difficult to rotate into position

            var angleComplete = angleDelta < GrabbedTarget.FinalJointMaxAngle || alreadyGrabbed;
            var distanceComplete = distance < ParentingMaxDistance;
            var timesUp = _pullingTimer > GrabbedTarget.FinalJointTimeout && GrabbedTarget.FinalJointQuick;

            if (angleComplete && distanceComplete || timesUp)
            {
                //Debug.Log($"before {angleDelta}");

                if (IsLineGrab && !_primaryGrabPointGrab)
                {
                    worldLine = GrabbedTarget.transform.TransformDirection(PosableGrabPoint.Line.normalized) * (_flipPose ? -1f : 1f);
                    var deltaRot = Quaternion.FromToRotation(worldLine, transform.up);
                    if (alreadyGrabbed)
                    {
                        transform.rotation = Quaternion.Inverse(deltaRot) * transform.rotation;
                    }
                    else
                    {
                        GrabbedTarget.transform.rotation = deltaRot * GrabbedTarget.transform.rotation;
                    }

                    worldLine = GrabbedTarget.transform.TransformDirection(PosableGrabPoint.Line.normalized) * (_flipPose ? -1f : 1f);
                    angleDelta = Vector3.Angle(worldLine, transform.up);
                }
                else
                {
                    var deltaRot = HandWorldRotation * Quaternion.Inverse(PoseWorldRotation);
                    if (alreadyGrabbed)
                    {
                        transform.rotation = Quaternion.Inverse(deltaRot) * transform.rotation;
                    }
                    else
                    {
                        GrabbedTarget.transform.rotation = deltaRot * GrabbedTarget.transform.rotation;
                    }

                    angleDelta = Quaternion.Angle(PoseWorldRotation, HandWorldRotation);
                }

                //Debug.Log($"final joint created {angleDelta}");
                PullingGrabbable = false;

                SetupConfigurableJoint(GrabbedTarget, true);

            }
        }

        private void CheckBreakDistance()
        {
            if (GrabbedTarget)
            {
                var position = GrabbedTarget.Stationary ? TrackedController.position : JointAnchorWorldPosition;
                if (Vector3.Distance(GrabAnchorWorld, position) > GrabbedTarget.BreakDistance)
                {
                    ForceRelease();
                }
            }
        }

        private void CheckPoseHand()
        {
            if (!IsGrabbing || _hasHandModelParented || !GrabbedTarget || IsPhysicsPose)
                return;

            var angleDelta = 0f;
            if (GrabbedTarget.GrabType == HVRGrabType.Snap && !IgnoreParentingAngle)
            {
                if (IsLineGrab && !_primaryGrabPointGrab)
                {
                    var worldLine = GrabbedTarget.transform.TransformDirection(PosableGrabPoint.Line.normalized) * (_flipPose ? -1f : 1f);
                    angleDelta = Vector3.Angle(worldLine, transform.up);
                }
                else
                {
                    angleDelta = Quaternion.Angle(PoseWorldRotation, HandWorldRotation);
                }
            }

            var distance = 0f;
            if (!IgnoreParentingDistance && _configurableJoint)
            {
                distance = Vector3.Distance(JointAnchorWorld, GrabAnchorWorld);
            }

            if ((IgnoreParentingAngle || angleDelta <= ParentingMaxAngleDelta) &&
                (IgnoreParentingDistance || distance <= ParentingMaxDistance) ||
                GrabbedTarget.ParentHandModelImmediately ||
                GrabbedTarget.GrabberCount > 1)
            {
                if (GrabbedTarget.ParentHandModel)
                {
                    ParentHandModel(GrabPoint, PosableGrabPoint ? PosableGrabPoint.HandPoser : FallbackPoser);
                }
                else
                {
                    PoseHand();
                }
            }
        }

        private void PoseHand()
        {
            _hasHandModelParented = true;

            if (InverseKinematics)
            {
                if (PosableGrabPoint && !IsLineGrab)
                {
                    var pose = PosableGrabPoint.HandPoser.PrimaryPose.Pose.GetPose(HandSide);
                    HandModel.parent = PosableGrabPoint.transform;
                    HandModel.localRotation = pose.Rotation;
                    HandModel.localPosition = pose.Position;
                }
            }
            else
            {
                _handOffset.localPosition = Vector3.zero;
                _handOffset.localRotation = Quaternion.identity;
                HandModel.parent = _handOffset;
            }


            if (IsPhysicsPose)
            {
                HandAnimator?.SetCurrentPoser(null);
            }
            else
            {
                HandAnimator?.SetCurrentPoser(PosableGrabPoint ? PosableGrabPoint.HandPoser : FallbackPoser, false);
            }

        }

        private void ParentHandModel(Transform parent, HVRHandPoser poser)
        {
            if (!parent)
                return;

            if (GrabbedTarget && !GrabbedTarget.ParentHandModel)
                return;

            var worldRotation = parent.rotation;
            var worldPosition = parent.position;

            var posableGrabPoint = parent.GetComponent<HVRPosableGrabPoint>();
            if (posableGrabPoint && posableGrabPoint.VisualGrabPoint)
            {
                parent = posableGrabPoint.VisualGrabPoint;
                parent.rotation = worldRotation;
                parent.position = worldPosition;
            }

            if (CloneHandModel)
            {
                _copySkin.enabled = true;
                _handClone.transform.parent = parent;
                _handCloneAnimator?.SetCurrentPoser(poser);
                _mainSkin.enabled = false;
            }
            else
            {
                HandModel.parent = parent;
                HandAnimator?.SetCurrentPoser(poser);

                if (InverseKinematics)
                {
                    if (PosableGrabPoint)
                    {
                        var pose = PosableGrabPoint.HandPoser.PrimaryPose.Pose.GetPose(HandSide);
                        HandModel.localRotation = pose.Rotation;
                        HandModel.localPosition = pose.Position;
                    }
                }
            }

            if (IsPhysicsPose && CloneHandModel)
            {
                var pose = PhysicsPoser.Hand.CreateHandPose();
                _handClone.GetComponent<HVRPosableHand>().Pose(pose);
                _handClone.localPosition = parent.InverseTransformPoint(HandModel.position);
                _handClone.localRotation = Quaternion.Inverse(parent.rotation) * HandModel.rotation;
                ResetHand(HandModel);
            }

            _hasHandModelParented = true;

            var listener = parent.gameObject.AddComponent<HVRDestroyListener>();
            listener.Destroyed.AddListener(OnGrabPointDestroyed);
        }
        private void OnGrabPointDestroyed(HVRDestroyListener listener)
        {
            if (HandGraphics && HandGraphics.transform.parent == listener.transform)
            {
                ResetHandModel();
            }
        }

        public void OverrideHandSettings(HVRJointSettings settings)
        {
            PhysicsHands.UpdateStrength(settings);
            if (settings)
            {
                //Debug.Log($"hand - {settings.name}");
            }
            else
            {
                //Debug.Log($"hand - reset");
            }
        }

        public override bool CanHover(HVRGrabbable grabbable)
        {
            if (IsForceGrabbing)
                return false;

            return CanGrab(grabbable);
        }

        private bool IsForceGrabbing => _hasForceGrabber && (ForceGrabber.IsForceGrabbing || ForceGrabber.IsAiming);

        public override bool CanGrab(HVRGrabbable grabbable)
        {
            if (!base.CanGrab(grabbable))
                return false;

            //if (HoveredSocket && HoveredSocket.CanRemoveGrabbable)
            //    return false;

            //todo reconsider how to prevent taking items from someone elses hands in multiplayer
            //this is prone to error if someone disconnects or the grab fails on the other side

            if ((!AllowMultiplayerSwap && !grabbable.AllowMultiplayerSwap) && grabbable.HoldType != HVRHoldType.ManyHands && grabbable.AnyGrabberNotMine())
            {
                return false;
            }

            if (grabbable.PrimaryGrabber && !grabbable.PrimaryGrabber.AllowSwap)
            {
                if (grabbable.HoldType == HVRHoldType.TwoHanded && grabbable.GrabberCount > 1)
                    return false;

                if (grabbable.HoldType == HVRHoldType.OneHand && !_isForceAutoGrab && grabbable.GrabberCount > 0)
                    return false;
            }

            if (GrabbedTarget != null && GrabbedTarget != grabbable)
                return false;

            if (grabbable.IsSocketed && grabbable.Socket.GrabDetectionType == HVRGrabDetection.Socket)
                return false;

            if (grabbable.RequireLineOfSight && !grabbable.IsSocketed && !grabbable.IsBeingForcedGrabbed &&
                !grabbable.IsStabbed && !grabbable.IsStabbing && !CheckLineOfSight(grabbable))
                return false;

            if (grabbable.RequiresGrabbable)
            {
                if (!grabbable.RequiredGrabbable.PrimaryGrabber || !grabbable.RequiredGrabbable.PrimaryGrabber.IsHandGrabber)
                    return false;
            }

            return true;
        }

        protected virtual bool CheckLineOfSight(HVRGrabbable grabbable)
        {
            if (grabbable.HasConcaveColliders)
                return true;
            return CheckForLineOfSight(RaycastOrigin.position, grabbable, RaycastLayermask);
        }

        protected override void OnBeforeGrabbed(HVRGrabArgs args)
        {
            if (HVRSettings.Instance.VerboseHandGrabberEvents)
            {
                Debug.Log($"{name}:OnBeforeGrabbed");
            }
            if (args.Grabbable.GrabType == HVRGrabType.Snap)
            {
                GrabPoint = null;

                if (_primaryGrabPointGrab)
                {
                    GrabPoint = args.Grabbable.GetForceGrabPoint(HandSide);
                }

                if (!GrabPoint || _isForceAutoGrab && GrabPoint == args.Grabbable.transform)
                {
                    DetermineGrabPoint(args.Grabbable);
                }


            }
            base.OnBeforeGrabbed(args);
        }

        protected override void OnGrabbed(HVRGrabArgs args)
        {
            base.OnGrabbed(args);

            if (HVRSettings.Instance.VerboseHandGrabberEvents)
            {
                Debug.Log($"{name}:OnGrabbed");
            }

            var grabbable = args.Grabbable;
            _grabbableControl = grabbable.GrabControl;
            _checkingSwap = true;

            SetToggle(grabbable);

            CanActivate = false;

            if (OverlappingGrabbables.TryGetValue(grabbable, out var routine))
            {
                if (routine != null) StopCoroutine(routine);
                OverlappingGrabbables.Remove(grabbable);
            }

            if (grabbable.DisableHandCollision)
            {
                Rigidbody.detectCollisions = false;
            }

            DisableHandCollision(grabbable);

            if (UseDynamicGrab())
            {
                if (InverseKinematics)
                {
                    IKDynamicGrab();
                }
                else
                {
                    DynamicGrab();
                }

                return;
            }


            if (!GrabPoint || args.Grabbable.GrabType == HVRGrabType.Offset)
            {
                OffsetGrab(grabbable);
            }
            else
            {
                IsLineGrab = PosableGrabPoint && PosableGrabPoint.IsLineGrab;

                if (IsLineGrab)
                {
                    SetupLineGrab(grabbable);
                }

                if ((!_isForceAutoGrab) && (HandGrabs || GrabbedTarget.Stationary || GrabbedTarget.GrabberCount > 1 || GrabbedTarget.IsStabbing
                    || GrabbedTarget.IsJointGrab && !GrabbedTarget.Rigidbody))
                {
                    StartCoroutine(MoveGrab());
                }
                else
                {
                    GrabPointGrab(grabbable);
                }
            }

            if (PosableGrabPoint && ControllerOffset)
            {
                ControllerOffset.SetGrabPointOffsets(PosableGrabPoint.HandPositionOffset, PosableGrabPoint.HandRotationOffset);
            }
        }

        private void SetToggle(HVRGrabbable grabbable)
        {
            var toggle = GrabTrigger == HVRGrabTrigger.Toggle;

            if (grabbable.OverrideGrabTrigger)
            {
                if (grabbable.GrabTrigger == HVRGrabTrigger.Toggle)
                {
                    toggle = true;
                }
            }

            if (toggle)
            {
                GrabToggleActive = true;
            }
        }


        private void OffsetGrab(HVRGrabbable grabbable)
        {
            TempGrabPoint = new GameObject(name + " OffsetGrabPoint");
            TempGrabPoint.transform.position = JointAnchorWorldPosition;
            TempGrabPoint.transform.parent = GrabbedTarget.transform;
            TempGrabPoint.transform.localRotation = Quaternion.identity;
            GrabPoint = TempGrabPoint.transform;
            TempGrabPoint.transform.rotation = HandModel.rotation;

            HandAnimator.SetCurrentPoser(null);
            HandAnimator.Hand.Pose(FallbackPoser.PrimaryPose.Pose.GetPose(HandSide));
            if (grabbable.ParentHandModel)
            {
                ParentHandModel(GrabPoint, null);
            }

            Grab(grabbable);
        }

        private void SetupLineGrab(HVRGrabbable grabbable)
        {
            var testPoint = _primaryGrabPointGrab ? GrabPoint.localPosition : GrabbedTarget.transform.InverseTransformPoint(transform.TransformPoint(GetHandAnchor()));
            _lineOffset = HVRUtilities.FindNearestPointOnLine(PosableGrabPoint.LineStart.localPosition, PosableGrabPoint.LineEnd.localPosition, testPoint) - PosableGrabPoint.LineMid;

            _flipPose = false;
            if (PosableGrabPoint.CanLineFlip && !_primaryGrabPointGrab)
            {
                _flipPose = Vector3.Dot(grabbable.transform.TransformDirection(PosableGrabPoint.Line), transform.up) < 0;
            }
        }

        private Vector3 FindClosestPoint(HVRGrabbable grabbable)
        {
            _physicsGrabPoints.Clear();

            if (grabbable.Colliders == null || grabbable.Colliders.Length == 0)
                return grabbable.transform.position;

            foreach (var gc in grabbable.Colliders)
            {
                if (!gc.enabled || !gc.gameObject.activeSelf || gc.isTrigger)
                    continue;

                var anchor = Palm.transform.position;
                Vector3 point;
                if (grabbable.HasConcaveColliders && gc is MeshCollider meshCollider && !meshCollider.convex)
                {
                    if (!gc.Raycast(new Ray(anchor, Palm.transform.forward), out var hit, .3f))
                    {
                        continue;
                    }

                    point = hit.point;
                }
                else
                {
                    point = gc.ClosestPoint(anchor);
                }

                if (point == Palm.transform.position || Vector3.Distance(Palm.transform.position, point) < .00001f)
                {
                    //palm is inside the collider or your collider is infinitely small or poorly formed and should be replaced
                    return point;
                }
                _physicsGrabPoints.Add(new Tuple<Collider, Vector3, float>(gc, point, Vector3.Distance(point, Palm.transform.position)));
            }

            if (_physicsGrabPoints.Count == 0)
                return Palm.transform.position;

            _physicsGrabPoints.Sort((x, y) => x.Item3.CompareTo(y.Item3));

            return _physicsGrabPoints[0].Item2;
        }

        private bool UseDynamicGrab()
        {
            if (GrabbedTarget.GrabType == HVRGrabType.Offset)
                return false;

            if (GrabbedTarget.Colliders.Length == 0)
            {
                return false;
            }

            return GrabbedTarget.GrabType == HVRGrabType.PhysicPoser || ((GrabPoint == null || GrabPoint == GrabbedTarget.transform) && GrabbedTarget.PhysicsPoserFallback);
        }

        private IEnumerator MoveGrab()
        {
            var target = GrabPoint.position;
            var linePoint = Vector3.zero;

            if (IsLineGrab && !_primaryGrabPointGrab)
            {
                linePoint = HVRUtilities.FindNearestPointOnLine(PosableGrabPoint.LineStart.localPosition, PosableGrabPoint.LineEnd.localPosition, GrabbedTarget.transform.InverseTransformPoint(transform.TransformPoint(GetHandAnchor())));
                target = GrabbedTarget.transform.TransformPoint(linePoint);
            }

            var time = (target - transform.position).magnitude / HandGrabSpeed;
            var elapsed = 0f;
            var start = transform.position;

            if (IsPhysicsPose)
                start = Palm.position;

            Rigidbody.detectCollisions = false;
            while (elapsed < time && GrabbedTarget)
            {
                target = IsLineGrab && !_primaryGrabPointGrab ? GrabbedTarget.transform.TransformPoint(linePoint) : GrabPoint.position;

                transform.position = Vector3.Lerp(start, target, elapsed / time);

                var targetRotation = PoseWorldRotation;

                if (!IsLineGrab || _primaryGrabPointGrab || GrabbedTarget.Stationary)
                {
                    transform.rotation = Quaternion.Slerp(HandModel.rotation, targetRotation, elapsed / time) * Quaternion.Inverse(HandModelRotation);
                }

                elapsed += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }

            Rigidbody.detectCollisions = true;

            if (!GrabbedTarget)
                yield break;

            if (GrabbedTarget.DisableHandCollision)
            {
                Rigidbody.detectCollisions = false;
            }

            var angleDelta = Quaternion.Angle(PoseWorldRotation, HandWorldRotation);
            if (IsLineGrab && !_primaryGrabPointGrab)
            {
                var worldLine = GrabbedTarget.transform.TransformDirection(PosableGrabPoint.Line.normalized) * (_flipPose ? -1f : 1f);
                angleDelta = Vector3.Angle(worldLine, transform.up);
            }

            //Debug.Log($"before {angleDelta}");

            if (IsLineGrab && !_primaryGrabPointGrab && !GrabbedTarget.Stationary)
            {
                var worldLine = GrabbedTarget.transform.TransformDirection(PosableGrabPoint.Line.normalized) * (_flipPose ? -1f : 1f);
                var deltaRot = Quaternion.FromToRotation(transform.up, worldLine);
                transform.rotation = deltaRot * transform.rotation;
                worldLine = GrabbedTarget.transform.TransformDirection(PosableGrabPoint.Line.normalized) * (_flipPose ? -1f : 1f);
                angleDelta = Vector3.Angle(worldLine, transform.up);
            }
            else
            {
                var deltaRot = HandWorldRotation * Quaternion.Inverse(PoseWorldRotation);
                transform.rotation = Quaternion.Inverse(deltaRot) * transform.rotation;
                angleDelta = Quaternion.Angle(PoseWorldRotation, HandWorldRotation);
            }

            //Debug.Log($"after movegrab {angleDelta}");

            GrabPointGrab(GrabbedTarget);
        }

        private void GrabPointGrab(HVRGrabbable grabbable)
        {
            Grab(grabbable);

            if (grabbable.ParentHandModel && grabbable.ParentHandModelImmediately)
            {
                ParentHandModel(GrabPoint, PosableGrabPoint ? PosableGrabPoint.HandPoser : FallbackPoser);
            }
            else if (!grabbable.ParentHandModel && grabbable.ParentHandModelImmediately)
            {
                PoseHand();
            }
        }

        public virtual void NetworkGrab(HVRGrabbable grabbable)
        {
            CommonGrab(grabbable);
        }

        public virtual void NetworkPhysicsGrab(HVRGrabbable grabbable)
        {
            IsPhysicsPose = true;
            if (grabbable.ParentHandModel)
            {
                ParentHandModel(GrabPoint.transform, null);
            }
            else
            {
                ResetHand(HandModel, true);
                PoseHand();
            }
            CommonGrab(grabbable);
        }

        protected virtual void Grab(HVRGrabbable grabbable)
        {
            CommonGrab(grabbable);
            Grabbed.Invoke(this, grabbable);
        }

        protected virtual void PhysicsGrab(HVRGrabbable grabbable)
        {
            IsPhysicsPose = true;
            if (grabbable.ParentHandModel)
            {
                ParentHandModel(GrabPoint.transform, null);
            }
            else
            {
                ResetHand(HandModel, true);
                PoseHand();
            }

            CommonGrab(grabbable);
            Grabbed.Invoke(this, grabbable);
        }

        private void CommonGrab(HVRGrabbable grabbable)
        {
            SetupGrab(grabbable);
            IsClimbing = grabbable.GetComponent<HVRClimbable>();
            if (grabbable.HandGrabbedClip)
                SFXPlayer.Instance.PlaySFX(grabbable.HandGrabbedClip, transform.position);
        }

        public void SetupGrab(HVRGrabbable grabbable)
        {
            if (grabbable.IsJointGrab)
            {
                bool final;
                if (!grabbable.Rigidbody)
                {
                    final = true;
                }
                else
                {
                    var handMovedToGrabbable = HandGrabs && !_isForceAutoGrab && !IsPhysicsPose || grabbable.GrabberCount > 1;
                    final = grabbable.GrabType == HVRGrabType.Offset || grabbable.Stationary || (grabbable.RemainsKinematic && grabbable.Rigidbody.isKinematic) || handMovedToGrabbable;
                    if (grabbable.TrackingType == HVRGrabTracking.FixedJoint && !handMovedToGrabbable)
                        final = false;
                }

                if (final)
                {
                    SetupConfigurableJoint(grabbable, true);
                }
                else //needs pulling and rotating into position
                {
                    SetupConfigurableJoint(grabbable);

                    PullingGrabbable = true;
                    _pullingTimer = 0f;
                }

                if (grabbable.Rigidbody && (!grabbable.Rigidbody.isKinematic || !grabbable.RemainsKinematic))
                {
                    grabbable.Rigidbody.isKinematic = false;
                    grabbable.Rigidbody.collisionDetectionMode = grabbable.CollisionDetection;
                }
            }

            if (GrabPoint)
            {
                grabbable.HeldGrabPoints.Add(GrabPoint);
            }
        }

        private Vector3 GetGrabbableAnchor()
        {
            if (IsLineGrab)
            {
                return PosableGrabPoint.LineMid;
            }

            var positionOffset = HandModelPosition;
            var rotationOffset = HandModelRotation;
            if (PosableGrabPoint)
            {
                if (PosableGrabPoint.IsJointAnchor)
                    return PosableGrabPoint.transform.localPosition;
                positionOffset = PosableGrabPoint.GetPosePositionOffset(HandSide);
                rotationOffset = PosableGrabPoint.GetPoseRotationOffset(HandSide);
            }
            else if (IsPhysicsPose)
            {
                if (InverseKinematics)
                    return GrabPoint.localPosition;
                positionOffset = PhysicsHandPosition;
                rotationOffset = PhysicsHandRotation;
            }
            else if (GrabbedTarget.GrabType == HVRGrabType.Offset)
            {
                positionOffset = HandModel.localPosition;
                rotationOffset = HandModel.localRotation;
            }

            _fakeHand.localPosition = HandModelPosition;
            _fakeHand.localRotation = HandModelRotation;
            _fakeHandAnchor.position = JointAnchorWorldPosition;
            _fakeHand.parent = GrabPoint;
            _fakeHand.localPosition = positionOffset;
            _fakeHand.localRotation = rotationOffset;

            var anchor = GrabbedTarget.transform.InverseTransformPoint(_fakeHandAnchor.position);

            _fakeHand.parent = transform;


            return anchor;

        }

        private Vector3 GetHandAnchor()
        {
            //if (IsPhysicsPose && InverseKinematics)
            //{
            //    return Palm.localPosition;
            //}    

            if (IsLineGrab)
            {
                return Quaternion.Inverse(PosableGrabPoint.GetPoseRotationOffset(HandSide) * Quaternion.Inverse(HandModelRotation)) * -PosableGrabPoint.GetPosePositionOffset(HandSide) + HandModelPosition;
            }

            if (PosableGrabPoint && PosableGrabPoint.IsJointAnchor)
            {
                return Quaternion.Inverse(PosableGrabPoint.GetPoseRotationOffset(HandSide) * Quaternion.Inverse(HandModelRotation)) * -PosableGrabPoint.GetPosePositionOffset(HandSide);
            }

            return JointAnchor.localPosition;
        }

        public Quaternion PoseLocalRotation
        {
            get
            {
                if (PosableGrabPoint)
                    return PosableGrabPoint.transform.localRotation * PosableGrabPoint.GetPoseRotationOffset(HandSide);
                return GrabPoint.localRotation;
            }
        }

        public Quaternion JointRotation
        {
            get
            {
                var poseRotation = PoseLocalRotation;

                if (IsPhysicsPose)
                {
                    poseRotation = PhysicsHandRotation;
                }
                else if (GrabbedTarget.GrabType == HVRGrabType.Offset)
                {
                    poseRotation = GrabPoint.localRotation;
                }

                return Quaternion.Inverse(GrabbedTarget.transform.rotation) * HandWorldRotation * Quaternion.Inverse(poseRotation);
            }
        }

        private void SetupConfigurableJoint(HVRGrabbable grabbable, bool final = false)
        {
            GrabAnchorLocal = GetGrabbableAnchor();
            HandAnchorLocal = GetHandAnchor();

            var axis = Vector3.right;
            var secondaryAxis = Vector3.up;

            if (IsLineGrab)
            {
                var line = PosableGrabPoint.LineEnd.localPosition - PosableGrabPoint.LineStart.localPosition;
                axis = line.normalized;
                secondaryAxis = HVRUtilities.OrthogonalVector(axis);
            }

            if (_configurableJoint)
            {
                Destroy(_configurableJoint);
            }

            var noRB = false;
            var owner = GrabbedTarget.gameObject;
            if (!GrabbedTarget.Rigidbody)
            {
                owner = gameObject;
                noRB = true;
            }

            _configurableJoint = owner.AddComponent<ConfigurableJoint>();
            _configurableJoint.autoConfigureConnectedAnchor = false;

            _configurableJoint.configuredInWorldSpace = false;

            if (noRB)
            {
                _configurableJoint.anchor = HandAnchorLocal;
                _configurableJoint.connectedAnchor = transform.TransformPoint(HandAnchorLocal);
                _configurableJoint.connectedBody = null;
            }
            else
            {
                _configurableJoint.anchor = GrabAnchorLocal;
                _configurableJoint.connectedAnchor = HandAnchorLocal;
                _configurableJoint.connectedBody = Rigidbody;
            }

            _configurableJoint.axis = axis;
            _configurableJoint.secondaryAxis = secondaryAxis;
            _configurableJoint.swapBodies = false;
            _configurableJoint.enablePreprocessing = false;

            if (IsLineGrab)
            {
                _configurableJoint.anchor = GrabAnchorLocal + _lineOffset;
            }

            if (!GrabbedTarget.Stationary)
            {
                if (IsLineGrab && (final || !_primaryGrabPointGrab))
                {
                    var handLine = GrabbedTarget.transform.InverseTransformDirection(transform.up);
                    var poseLine = PosableGrabPoint.Line * (_flipPose ? -1f : 1f);
                    var handLocal = Quaternion.FromToRotation(poseLine, handLine);

                    if (final)
                    {
                        _startRotation = Quaternion.Inverse(Quaternion.Inverse(grabbable.transform.rotation) * transform.rotation);
                        _configurableJoint.SetTargetRotationLocal(Quaternion.Inverse(Quaternion.Inverse(grabbable.transform.rotation) * transform.rotation), _startRotation);
                    }
                    else
                    {
                        _configurableJoint.SetTargetRotationLocal(GrabbedTarget.transform.localRotation * handLocal, GrabbedTarget.transform.localRotation);
                    }
                }
                else
                {
                    _configurableJoint.SetTargetRotationLocal(GrabbedTarget.transform.localRotation * (JointRotation), GrabbedTarget.transform.localRotation);
                }
            }

            grabbable.AddJoint(_configurableJoint, this);

            HVRJointSettings pullSettings = null;

            if (grabbable.PullingSettingsOverride)
            {
                pullSettings = grabbable.PullingSettingsOverride;
            }
            else if (PullingSettings)
            {
                pullSettings = PullingSettings;
            }

            if (!final && pullSettings != null)
            {
                pullSettings.ApplySettings(_configurableJoint);
            }
            else
            {
                HVRJointSettings settings;
                if (grabbable.JointOverride)
                {
                    settings = grabbable.JointOverride;
                }
                else if (IsLineGrab)
                {
                    settings = HVRSettings.Instance.LineGrabSettings;
                }
                else if (HVRSettings.Instance.DefaultJointSettings)
                {
                    settings = HVRSettings.Instance.DefaultJointSettings;
                }
                else
                {
                    Debug.LogError("HVRGrabbable:JointOverride or HVRSettings:DefaultJointSettings must be populated.");
                    return;
                }

                settings.ApplySettings(_configurableJoint);

                if (grabbable.TrackingType == HVRGrabTracking.FixedJoint)
                {
                    _configurableJoint.xMotion = ConfigurableJointMotion.Locked;
                    _configurableJoint.yMotion = ConfigurableJointMotion.Locked;
                    _configurableJoint.zMotion = ConfigurableJointMotion.Locked;
                    _configurableJoint.angularXMotion = ConfigurableJointMotion.Locked;
                    _configurableJoint.angularYMotion = ConfigurableJointMotion.Locked;
                    _configurableJoint.angularZMotion = ConfigurableJointMotion.Locked;

                    if (grabbable.CanJointBreak)
                    {
                        _configurableJoint.breakForce = grabbable.JointBreakForce;
                        _configurableJoint.breakTorque = grabbable.JointBreakTorque;
                    }
                }

                if (IsLineGrab)
                {
                    _tightlyHeld = Inputs.GetGripHoldActive(HandSide);

                    if (!_tightlyHeld)
                    {
                        SetupLooseLineGrab();
                    }
                }
            }

            if (final)
            {
                UpdateGrabbableCOM(grabbable);
            }
        }

        internal void UpdateGrabbableCOM(HVRGrabbable grabbable)
        {
            if (grabbable.Rigidbody && grabbable.PalmCenterOfMass)
            {
                //Debug.Log($"updating grabbable com { grabbable.HandGrabbers.Count}");
                if (grabbable.HandGrabbers.Count == 1)
                {
                    var p1 = grabbable.HandGrabbers[0].JointAnchorWorldPosition;
                    grabbable.Rigidbody.centerOfMass = grabbable.transform.InverseTransformPoint(p1);
                }
                else if (grabbable.HandGrabbers.Count == 2)
                {
                    var p1 = grabbable.HandGrabbers[0].JointAnchorWorldPosition;
                    var p2 = grabbable.HandGrabbers[1].JointAnchorWorldPosition;
                    grabbable.Rigidbody.centerOfMass = grabbable.transform.InverseTransformPoint((p1 + p2) / 2);
                }
            }
        }

        private void UpdateLineGrab()
        {
            if (!IsLineGrab || PullingGrabbable || !_configurableJoint)
                return;


            bool tighten;
            bool loosen;

            if (HVRSettings.Instance.LineGrabTriggerLoose)
            {
                tighten = !IsTriggerGrabActive;
                loosen = IsTriggerGrabActive;
            }
            else
            {
                tighten = GrabTrigger == HVRGrabTrigger.Active && IsGripGrabActive ||
                          GrabTrigger == HVRGrabTrigger.Toggle && !IsTriggerGrabActive;

                loosen = GrabTrigger == HVRGrabTrigger.Active && !IsGripGrabActive ||
                          GrabTrigger == HVRGrabTrigger.Toggle && IsTriggerGrabActive;
            }

            if (!_tightlyHeld && tighten)
            {
                _tightlyHeld = true;
                var settings = GrabbedTarget.JointOverride ?? HVRSettings.Instance.LineGrabSettings;
                settings.ApplySettings(_configurableJoint);

                _lineOffset = HVRUtilities.FindNearestPointOnLine(PosableGrabPoint.LineStart.localPosition, PosableGrabPoint.LineEnd.localPosition, GrabbedTarget.transform.InverseTransformPoint(transform.TransformPoint(GetHandAnchor()))) - PosableGrabPoint.LineMid;

                _configurableJoint.anchor = GrabAnchorLocal + _lineOffset;
                _configurableJoint.SetTargetRotationLocal(Quaternion.Inverse(Quaternion.Inverse(GrabbedTarget.transform.rotation) * transform.rotation), _startRotation);
                UpdateGrabbableCOM(GrabbedTarget);
            }
            else if (_tightlyHeld && loosen)
            {
                _tightlyHeld = false;
                SetupLooseLineGrab();
            }
        }

        private void SetupLooseLineGrab()
        {
            if (PosableGrabPoint.LineCanReposition)
            {
                _configurableJoint.xMotion = ConfigurableJointMotion.Limited;
                var limit = _configurableJoint.linearLimit;
                limit.limit = PosableGrabPoint.Line.magnitude / 2f;
                _configurableJoint.linearLimit = limit;
                _configurableJoint.anchor = GrabAnchorLocal;

                var xDrive = _configurableJoint.xDrive;
                xDrive.positionSpring = 0;
                xDrive.positionDamper = PosableGrabPoint.LooseDamper;
                xDrive.maximumForce = 100000f;
                _configurableJoint.xDrive = xDrive;
            }

            if (PosableGrabPoint.LineCanRotate)
            {
                _configurableJoint.angularXMotion = ConfigurableJointMotion.Free;


                var xDrive = _configurableJoint.angularXDrive;
                xDrive.positionSpring = 0;
                xDrive.positionDamper = PosableGrabPoint.LooseAngularDamper;
                xDrive.maximumForce = 100000f;
                _configurableJoint.angularXDrive = xDrive;
            }

        }

        protected override void OnReleased(HVRGrabbable grabbable)
        {
            if (HVRSettings.Instance.VerboseHandGrabberEvents)
            {
                Debug.Log($"{name}:OnReleased");
            }
            base.OnReleased(grabbable);

            if (ControllerOffset)
            {
                ControllerOffset.ResetGrabPointOffsets();
            }

            _primaryGrabPointGrab = false;
            _lineOffset = Vector3.zero;
            PullingGrabbable = false;
            _currentGrabControl = HVRGrabControls.GripOrTrigger;
            _grabbableControl = HVRGrabControls.GripOrTrigger;
            IsLineGrab = false;

            TriggerGrabPoint = null;
            ResetHandModel();

            IsPhysicsPose = false;
            _physicsPose = null;
            Rigidbody.detectCollisions = true;

            HandModel.SetLayerRecursive(HVRLayers.Hand);

            if (InvisibleHand)
            {
                InvisibleHand.gameObject.SetActive(false);
            }

            if (TempGrabPoint)
            {
                Destroy(TempGrabPoint.gameObject);
            }

            IsClimbing = false;

            if (!grabbable.BeingDestroyed)
            {
                var routine = StartCoroutine(CheckReleasedOverlap(grabbable));
                OverlappingGrabbables[grabbable] = routine;

                grabbable.HeldGrabPoints.Remove(GrabPoint);

                if (grabbable.Rigidbody)
                {
                    var throwVelocity = ComputeThrowVelocity(grabbable, out var angularVelocity, true);
                    grabbable.Rigidbody.velocity = throwVelocity;
                    grabbable.Rigidbody.angularVelocity = angularVelocity;
                }
            }

            GrabToggleActive = false;
            GrabPoint = null;
            Released.Invoke(this, grabbable);
        }

        private void SetGrabbableLayer(HVRGrabbable grabbable, int layer)
        {
            foreach (var c in grabbable.Colliders)
            {
                c.transform.gameObject.layer = layer;
            }
        }

        public Vector3 GetAverageVelocity(int frames, int start)
        {
            if (start + frames > TrackedVelocityCount)
                frames = TrackedVelocityCount - start;
            return GetAverageVelocity(frames, start, RecentVelocities, TakePeakVelocities, CountPeakVelocities);
        }

        public Vector3 GetAverageAngularVelocity(int frames, int start)
        {
            if (start + frames > TrackedVelocityCount)
                frames = TrackedVelocityCount - start;
            return GetAverageVelocity(frames, start, RecentAngularVelocities);
        }

        private static readonly List<Vector3> _peakVelocities = new List<Vector3>(10);
        private static readonly IComparer<Vector3> _velocityComparer = new VelocityComparer();


        internal static Vector3 GetAverageVelocity(int frames, int start, CircularBuffer<Vector3> recentVelocities, bool takePeak = false, int nPeak = 3)
        {
            var sum = Vector3.zero;
            for (var i = start; i < start + frames; i++)
            {
                sum += recentVelocities[i];
            }

            if (Mathf.Approximately(frames, 0f))
                return Vector3.zero;

            var average = sum / frames;

            sum = Vector3.zero;

            _peakVelocities.Clear();

            for (var i = start; i < start + frames; i++)
            {

                //removing any vectors not going in the direction of the average vector
                var dot = Vector3.Dot(average.normalized, recentVelocities[i].normalized);
                if (dot < .2)
                {
                    //Debug.Log($"Filtered {average},{recentVelocities[i]},{dot}");
                    continue;
                }

                if (takePeak)
                {
                    _peakVelocities.Add(recentVelocities[i]);
                }
                else
                {
                    sum += recentVelocities[i];
                }
            }

            if (!takePeak)
            {
                return sum / frames;
            }

            if (nPeak == 0)
                return Vector3.zero;

            sum = Vector3.zero;
            SortHelper.Sort(_peakVelocities, 0, _peakVelocities.Count, _velocityComparer);

            for (int i = _peakVelocities.Count - 1, j = 0; j < nPeak; j++, i--)
            {
                if (i < 0 || i >= _peakVelocities.Count)
                    break;
                sum += _peakVelocities[i];
            }

            return sum / nPeak;
        }



        public Vector3 ComputeThrowVelocity(HVRGrabbable grabbable, out Vector3 angularVelocity, bool isThrowing = false)
        {
            if (!grabbable.Rigidbody)
            {
                angularVelocity = Vector3.zero;
                return Vector3.zero;
            }

            var grabbableVelocity = grabbable.GetAverageVelocity(ThrowLookback, ThrowLookbackStart, TakePeakVelocities, CountPeakVelocities);
            var grabbableAngular = grabbable.GetAverageAngularVelocity(ThrowLookback, ThrowLookbackStart);

            var handVelocity = GetAverageVelocity(ThrowLookback, ThrowLookbackStart);
            var handAngularVelocity = GetAverageAngularVelocity(ThrowLookback, ThrowLookbackStart);

            var linearVelocity = ReleasedVelocityFactor * handVelocity + grabbableVelocity * grabbable.ReleasedVelocityFactor;
            var throwVelocity = linearVelocity;

            Vector3 centerOfMass;
            if (ThrowingCenterOfMass && ThrowingCenterOfMass.CenterOfMass)
            {
                centerOfMass = ThrowingCenterOfMass.CenterOfMass.position;
            }
            else
            {
                centerOfMass = Rigidbody.worldCenterOfMass;
            }

            //compute linear velocity from wrist rotation
            var grabbableCom = GrabPoint != null ? GrabPoint.position : grabbable.Rigidbody.worldCenterOfMass;

            //Debug.Log($"{handAngularVelocity.magnitude}");

            if (handAngularVelocity.magnitude > ReleasedAngularThreshold)
            {
                var cross = Vector3.Cross(handAngularVelocity, grabbableCom - centerOfMass) * grabbable.ReleasedAngularConversionFactor * ReleasedAngularConversionFactor;
                throwVelocity += cross;
            }

            angularVelocity = grabbableAngular * grabbable.ReleasedAngularFactor;

            return throwVelocity;
        }


        private IEnumerator CheckReleasedOverlap(HVRGrabbable grabbable)
        {
            if (!OverlapSizer || !_overlapCollider)
            {
                yield break;
            }

            yield return new WaitForFixedUpdate();

            var elapsed = 0f;

            while (OverlappingGrabbables.ContainsKey(grabbable))
            {
                var count = Physics.OverlapSphereNonAlloc(OverlapSizer.transform.position, _overlapCollider.radius, _overlapColliders);
                if (count == 0) break;

                var match = false;
                for (int i = 0; i < count; i++)
                {
                    if (_overlapColliders[i].attachedRigidbody == grabbable.Rigidbody)
                    {
                        match = true;
                        break;
                    }
                }

                if (!match)
                    break;

                yield return new WaitForFixedUpdate();
                elapsed += Time.fixedDeltaTime;

                if (!grabbable.RequireOverlapClearance && elapsed > grabbable.OverlapTimeout)
                {
                    break;
                }
            }

            EnableHandCollision(grabbable);
            EnableInvisibleHandCollision(grabbable);

            OverlappingGrabbables.Remove(grabbable);
        }

        private void EnableInvisibleHandCollision(HVRGrabbable grabbable)
        {
            if (InvisibleHandColliders == null || grabbable.Colliders == null)
            {
                return;
            }

            foreach (var handCollider in InvisibleHandColliders)
            {
                foreach (var grabbableCollider in grabbable.Colliders)
                {
                    if (grabbableCollider)
                        Physics.IgnoreCollision(handCollider, grabbableCollider, false);
                }

                foreach (var grabbableCollider in grabbable.AdditionalIgnoreColliders)
                {
                    Physics.IgnoreCollision(handCollider, grabbableCollider, false);
                }
            }
        }

        private void DisableInvisibleHandCollision(HVRGrabbable grabbable, Collider except = null)
        {
            if (InvisibleHandColliders == null || grabbable.Colliders == null)
            {
                return;
            }

            foreach (var handCollider in InvisibleHandColliders)
            {
                foreach (var grabbableCollider in grabbable.Colliders)
                {
                    if (grabbableCollider && except != grabbableCollider)
                        Physics.IgnoreCollision(handCollider, grabbableCollider);
                }

                foreach (var grabbableCollider in grabbable.AdditionalIgnoreColliders)
                {
                    Physics.IgnoreCollision(handCollider, grabbableCollider);
                }
            }
        }

        public void UpdateCollision(HVRGrabbable grabbable, bool enable)
        {
            if (enable)
            {
                EnableHandCollision(grabbable);
            }
            else
            {
                DisableHandCollision(grabbable);
            }
        }

        public void EnableHandCollision(HVRGrabbable grabbable)
        {
            HandPhysics.IgnoreCollision(grabbable.Colliders, false);
            HandPhysics.IgnoreCollision(grabbable.AdditionalIgnoreColliders, false);
        }

        public void DisableHandCollision(HVRGrabbable grabbable)
        {
            if (grabbable.EnableInvisibleHand && InvisibleHand)
            {
                InvisibleHand.gameObject.SetActive(true);
                DisableInvisibleHandCollision(grabbable);
            }

            HandPhysics.IgnoreCollision(grabbable.Colliders, true);
            HandPhysics.IgnoreCollision(grabbable.AdditionalIgnoreColliders, true);
        }

        private void IKDynamicGrab()
        {
            var previousLayer = GrabbedTarget.gameObject.layer;

            var layerMask = LayerMask.GetMask(HVRLayers.DynamicPose.ToString());
            SetGrabbableLayer(GrabbedTarget, LayerMask.NameToLayer(HVRLayers.DynamicPose.ToString()));

            try
            {
                IsPhysicsPose = true;

                TempGrabPoint = new GameObject(name + " GrabPoint");
                TempGrabPoint.transform.parent = GrabbedTarget.transform;
                TempGrabPoint.transform.position = FindClosestPoint(GrabbedTarget);
                TempGrabPoint.transform.localRotation = Quaternion.identity;
                GrabPoint = TempGrabPoint.transform;

                var delta = GrabPoint.position - PhysicsPoser.Palm.position;
                var palmDelta = Quaternion.FromToRotation(PhysicsPoser.Palm.forward, delta.normalized);

                PhysicsPoser.Hand.transform.rotation = palmDelta * PhysicsPoser.Hand.transform.rotation;
                HandModel.rotation = palmDelta * HandModel.rotation;

                HandAnimator?.SetCurrentPoser(null);
                PhysicsPoser.OpenFingers();

                var gpDelta = GrabPoint.position - Palm.position;
                PhysicsPoser.Hand.transform.position += gpDelta;
                HandModel.position += gpDelta;

                PhysicsPoser.SimulateClose(layerMask);
                _physicsPose = PhysicsPoser.Hand.CreateHandPose();
                PhysicsPoseBytes = _physicsPose.Serialize();

                PhysicsHandRotation = Quaternion.Inverse(GrabPoint.rotation) * HandModel.rotation;
                PhysicsHandPosition = GrabPoint.transform.InverseTransformPoint(HandModel.position);
                PhysicsPoseBytes = PhysicsPoser.Hand.CreateHandPose().Serialize();
                PhysicsGrab(GrabbedTarget);
            }
            finally
            {
                SetGrabbableLayer(GrabbedTarget, previousLayer);
            }
        }

        private void DynamicGrab()
        {
            var previousLayer = GrabbedTarget.gameObject.layer;

            var layerMask = LayerMask.GetMask(HVRLayers.DynamicPose.ToString());
            SetGrabbableLayer(GrabbedTarget, LayerMask.NameToLayer(HVRLayers.DynamicPose.ToString()));

            try
            {
                TempGrabPoint = new GameObject(name + " GrabPoint");
                TempGrabPoint.transform.parent = GrabbedTarget.transform;
                TempGrabPoint.transform.position = FindClosestPoint(GrabbedTarget);
                TempGrabPoint.transform.localRotation = Quaternion.identity;
                GrabPoint = TempGrabPoint.transform;

                var pos = HandModel.position;
                var rot = HandModel.rotation;

                HandModel.position += -Palm.forward * .3f;

                var delta = GrabPoint.position - PhysicsPoser.Palm.position;
                var palmDelta = Quaternion.FromToRotation(PhysicsPoser.Palm.forward, delta.normalized);
                HandModel.rotation = palmDelta * HandModel.rotation;
                var offset = HandModel.position - Palm.position;

                HandModel.position = GrabPoint.position + offset;

                PhysicsPoser.OpenFingers();
                PhysicsPoser.SimulateClose(layerMask);
                _physicsPose = PhysicsPoser.Hand.CreateHandPose();
                PhysicsPoseBytes = _physicsPose.Serialize();
                //PhysicsPoser.Hand.Pose();
                PhysicsHandRotation = Quaternion.Inverse(GrabPoint.rotation) * HandModel.rotation;
                PhysicsHandPosition = GrabPoint.transform.InverseTransformPoint(HandModel.position);

                //HandModel.position = pos;
                //HandModel.rotation = rot;

                PhysicsGrab(GrabbedTarget);
            }
            finally
            {
                SetGrabbableLayer(GrabbedTarget, previousLayer);
            }
        }


        public bool TryAutoGrab(HVRGrabbable grabbable)
        {
            if (GrabTrigger == HVRGrabTrigger.Active && !Inputs.GetHoldActive(HandSide))
            {
                return false;
            }

            grabbable.Rigidbody.velocity = Vector3.zero;
            grabbable.Rigidbody.angularVelocity = Vector3.zero;

            GrabPoint = grabbable.GetForceGrabPoint(HandSide) ?? grabbable.transform;
            if (!PosableGrabPoint && grabbable.PhysicsPoserFallback)
                GrabPoint = null;

            _isForceAutoGrab = true;
            _primaryGrabPointGrab = true;

            try
            {
                if (TryGrab(grabbable))
                {
                    _currentGrabControl = grabbable.GrabControl;
                    return true;
                }
            }
            finally
            {
                _isForceAutoGrab = false;
            }
            return false;
        }


        private void ResetHandModel(bool maintainPose = false)
        {
            _hasHandModelParented = false;
            if (!HandGraphics)
                return;

            if (HandGraphics.parent)
            {
                var listener = HandGraphics.parent.GetComponent<HVRDestroyListener>();
                if (listener)
                {
                    listener.Destroyed.RemoveListener(OnGrabPointDestroyed);
                    Destroy(listener);
                }
            }

            ResetHand(HandModel, maintainPose);
            if (_handClone)
            {
                ResetHand(_handClone, maintainPose);
            }

            if (_copySkin)
            {
                _copySkin.enabled = false;
            }

            if (_mainSkin)
            {
                _mainSkin.enabled = true;
            }
        }

        private void ResetHand(Transform hand, bool maintainPose = false)
        {
            hand.parent = HandModelParent;
            hand.localPosition = HandModelPosition;
            hand.localRotation = HandModelRotation;
            hand.localScale = HandModelScale;
            if (!maintainPose)
            {
                if (InverseKinematics)
                {
                    HandAnimator?.ResetToDefault();
                }
                else
                {
                    hand.GetComponent<HVRHandAnimator>()?.ResetToDefault();
                }

            }
        }

        private void ResetRigidBodyProperties()
        {
            this.ExecuteNextUpdate(() =>
            {
                Rigidbody.ResetCenterOfMass();
                Rigidbody.ResetInertiaTensor();

                if (RigidOverrides)
                {
                    RigidOverrides.ApplyOverrides();
                }
            });
        }

        internal byte[] GetPoseData()
        {
            if (CloneHandModel)
            {
                return _clonePosableHand.CreateHandPose().Serialize();
            }
            else
            {
                return _posableHand.CreateHandPose().Serialize();
            }
        }

        internal void PoseHand(byte[] data)
        {
            if (CloneHandModel)
            {
                _clonePosableHand.Pose(HVRHandPoseData.FromByteArray(data, HandSide), GrabbedTarget.ParentHandModel);
            }
            _posableHand.Pose(HVRHandPoseData.FromByteArray(data, HandSide), GrabbedTarget.ParentHandModel);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (DrawCenterOfMass && Rigidbody)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(Rigidbody.worldCenterOfMass, .03f);
            }

            //if (_configurableJoint)
            //{
            //    Gizmos.color = Color.cyan;
            //    Gizmos.DrawWireSphere(_configurableJoint.transform.TransformPoint(_configurableJoint.anchor), .02f);
            //    Gizmos.color = Color.blue;
            //    Gizmos.DrawCube(transform.TransformPoint(_configurableJoint.connectedAnchor), new Vector3(.02f, .02f, .02f));
            //}

            //if (PosableGrabPoint && (IsHovering || IsGrabbing))
            //{
            //    Gizmos.color = Color.red;
            //    Gizmos.DrawCube(PoseWorldPosition, new Vector3(.02f, .02f, .02f));
            //    //Debug.DrawLine(PoseWorldPosition, GrabPoint.position, Color.green);

            //    Gizmos.color = Color.blue;

            //    var grabbable = HoverTarget ?? GrabbedTarget;

            //    var p = Quaternion.Inverse(PosableGrabPoint.GetPoseRotationOffset(HandSide) * Quaternion.Inverse(HandModelRotation)) * -(PosableGrabPoint.GetPosePositionOffset(HandSide));

            //    Gizmos.DrawCube(transform.TransformPoint(p), new Vector3(.02f, .02f, .02f));
            //}
        }

#endif
    }

    internal class VelocityComparer : IComparer<Vector3>
    {
        public int Compare(Vector3 x, Vector3 y)
        {
            return x.magnitude.CompareTo(y.magnitude);
        }
    }

}
