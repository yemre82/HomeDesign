using System;
using System.Collections;
using System.Collections.Generic;
using System.Resources;
using HurricaneVR.Framework.Components;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace HurricaneVR.Samples
{
    public enum CombinationLockState
    {
        CamOne,
        CamTwo,
        CamThree,
        Unlocked
    }

    public class CombinationLock : HVRDial
    {
        public TextMeshPro NumberLabel;
        public TextMeshPro DebugLabel;
        public bool DisplayDebug;

        public int First = 30;
        public int Second = 15;
        public int Third = 01;

        public int CurrentNumber;

        public float CamDistance;
        public float PreviousDistance;

        public float Tolerance = 40f;

        public float LowerBound = 0f;
        public float UpperBound = 0f;

        public int AccuracyAllowance = 1;

        public UnityEvent Unlocked = new UnityEvent();

        private CombinationLockState _state;

        public CombinationLockState State
        {
            get { return _state; }
            set
            {
                _state = value;
                ComputeBounds();
            }
        }

        public int NumberOfRotations
        {
            get
            {
                return ((int)Mathf.Abs(CamDistance)) / 355;
            }
        }

        protected override void Start()
        {
            base.Start();
            ResetLockState(CombinationLockState.CamOne);

            if (DebugLabel)
            {
                DebugLabel.text = $"Code:{First},{Second},{Third}\r\n Dist: {CamDistance:f0}\r\nState: {State}\r\nTolerance: {Tolerance:f0}\r\nL_Bound: {LowerBound:f0}\r\nU_Bound: {UpperBound:f0}";
            }
        }

        private void ComputeBounds()
        {
            switch (State)
            {
                case CombinationLockState.CamOne:
                    LowerBound = 0f;
                    UpperBound = 1080f;
                    break;
                case CombinationLockState.CamTwo:
                    LowerBound = -360f - (360 - Second * StepSize);
                    UpperBound = 0f + Tolerance;
                    break;
                case CombinationLockState.CamThree:

                    if (Third < Second)
                    {
                        UpperBound = (Steps - Second + Third) * StepSize;
                    }
                    else
                    {
                        UpperBound = (Third - Second) * StepSize;
                    }

                    LowerBound = 0f;

                    break;
                case CombinationLockState.Unlocked:
                    break;
            }

            LowerBound -= Tolerance;
            UpperBound += Tolerance;
        }

        protected override void Update()
        {
            base.Update();
        }

        public bool IsFirstInRange => CurrentNumber >= First - AccuracyAllowance && CurrentNumber <= First + AccuracyAllowance;
        public bool IsSecondInRange => CurrentNumber >= Second - AccuracyAllowance && CurrentNumber <= Second + AccuracyAllowance;
        public bool IsThirdInRange => CurrentNumber >= Third - AccuracyAllowance && CurrentNumber <= Third + AccuracyAllowance;


        public void ResetLockState(CombinationLockState state)
        {
            State = state;
            CamDistance = 0f;
            PreviousDistance = 0f;
        }

        protected override void OnStepChanged(int step, bool raiseEvents)
        {
            base.OnStepChanged(step, raiseEvents);

            CurrentNumber = step;

            NumberLabel.text = CurrentNumber.ToString("n0");

            if (DebugLabel)
            {
                DebugLabel.text = $"Code:{First},{Second},{Third}\r\n Dist: {CamDistance:f0}\r\nState: {State}\r\nTolerance: {Tolerance:f0}\r\nL_Bound: {LowerBound:f0}\r\nU_Bound: {UpperBound:f0}";
            }
        }

        protected override void OnAngleChanged(float angle, float delta, float percent, bool raiseEvents)
        {
            CamDistance += delta;

            if (CamDistance < LowerBound)
            {
                ResetLockState(CombinationLockState.CamOne);
            }
            else if (CamDistance > UpperBound && State != CombinationLockState.CamOne)
            {
                if (State == CombinationLockState.CamTwo)
                {
                    CamDistance = 1080f;
                    State = CombinationLockState.CamOne;
                }
                else
                {
                    ResetLockState(CombinationLockState.CamOne);
                }
            }

            if (State == CombinationLockState.CamOne && NumberOfRotations >= 3 && IsFirstInRange)
            {
                ResetLockState(CombinationLockState.CamTwo);
            }
            else if (State == CombinationLockState.CamTwo && NumberOfRotations == 1 && IsSecondInRange)
            {
                ResetLockState(CombinationLockState.CamThree);
            }
            else if (State == CombinationLockState.CamThree && IsThirdInRange)
            {
                State = CombinationLockState.Unlocked;
                Unlocked.Invoke();
            }

        }
    }
}