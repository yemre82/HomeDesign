using System.Collections;
using System.Collections.Generic;
using HurricaneVR.Framework.ControllerInput;
using HurricaneVR.Framework.Core.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DemoUIManager : MonoBehaviour
{
    public HVRPlayerController Player;
    public HVRCameraRig CameraRig;
    public HVRPlayerInputs Inputs;

    public TextMeshProUGUI SitStandText;
    public TextMeshProUGUI ForceGrabText;
    public Slider TurnRateSlider;
    public Slider SnapTurnSlider;
    public TextMeshProUGUI TurnRateText;
    public TextMeshProUGUI SnapRateText;
    public Toggle SmoothTurnToggle;

    void Start()
    {
        UpdateSitStandButton();
        UpdateForceGrabButton();
        TurnRateSlider.value = Player.SmoothTurnSpeed;
        SnapTurnSlider.value = Player.SnapAmount;

        TurnRateText.text = Player.SmoothTurnSpeed.ToString();
        SnapRateText.text = Player.SnapAmount.ToString();

        SmoothTurnToggle.isOn = Player.RotationType == RotationType.Smooth;

        TurnRateSlider.onValueChanged.AddListener(OnTurnRateChanged);
        SnapTurnSlider.onValueChanged.AddListener(OnSnapTurnRateChanged);
        SmoothTurnToggle.onValueChanged.AddListener(OnSmoothTurnChanged);
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void OnSitStandClicked()
    {
        var index = (int)CameraRig.SitStanding;
        index++;
        if (index > 2)
        {
            index = 0;
        }

        CameraRig.SetSitStandMode((HVRSitStand)index);
        UpdateSitStandButton();
    }

    public void OnForceGrabClicked()
    {
        var index = (int)Inputs.ForceGrabActivation;
        index++;
        if (index > 1)
        {
            index = 0;
        }

        Inputs.ForceGrabActivation = (HVRForceGrabActivation)index;
        UpdateForceGrabButton();
    }

    private void UpdateForceGrabButton()
    {
        ForceGrabText.text = Inputs.ForceGrabActivation.ToString();
    }

    private void UpdateSitStandButton()
    {
        SitStandText.text = CameraRig.SitStanding.ToString();
    }

    public void OnTurnRateChanged(float rate)
    {
        Player.SmoothTurnSpeed = rate;
        TurnRateText.text = Player.SmoothTurnSpeed.ToString();
    }

    public void OnSnapTurnRateChanged(float rate)
    {
        Player.SnapAmount = rate;
        SnapRateText.text = Player.SnapAmount.ToString();
    }

    public void OnSmoothTurnChanged(bool smooth)
    {
        Player.RotationType = smooth ? RotationType.Smooth : RotationType.Snap;
    }
}
