using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HurricaneVR.Framework.ControllerInput;
using HurricaneVR.Framework.Shared;

public class MenuAnimationController : MonoBehaviour
{
    public HVRController RightController => HVRInputManager.Instance.RightController;
    public HVRController LeftController => HVRInputManager.Instance.LeftController;
    public Animator animator;
    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
    }

    public float i = 0;
    public int myNum = 0;

    // Update is called once per frame
    void Update()
    {
        i += Time.deltaTime;
        if (RightController.SecondaryButton || LeftController.SecondaryButton)
        {
            float j = i - myNum;
            if (!animator.GetBool("trigger") && j > 2)
            {
                animator.SetInteger("trigger", 1);
                myNum = (int)i;
            }
            else if (animator.GetBool("trigger") && j > 2)
            {
                animator.SetInteger("trigger", 2);
                myNum = (int)i;
            }
            
        }
    }
}
