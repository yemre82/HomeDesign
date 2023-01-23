using HurricaneVR.Framework.Core;
using HurricaneVR.Framework.Core.Grabbers;
using UnityEngine;

namespace HurricaneVR.Samples
{
    public class Holster : HVRSocket
    {
        protected override Quaternion GetRotationOffset(HVRGrabbable grabbable)
        {
            var holsertOrientation = grabbable.GetComponent<HolsterOrientation>();
            if (holsertOrientation && holsertOrientation.Orientation)
                return holsertOrientation.Orientation.localRotation;
            return base.GetRotationOffset(grabbable);
        }

        protected override Vector3 GetPositionOffset(HVRGrabbable grabbable)
        {
            var holsertOrientation = grabbable.GetComponent<HolsterOrientation>();
            if (holsertOrientation && holsertOrientation.Orientation)
                return holsertOrientation.Orientation.localPosition;
            return base.GetPositionOffset(grabbable);
        }
    }
}