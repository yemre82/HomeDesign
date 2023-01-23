using HurricaneVR.Framework.Core.Sockets;

namespace HurricaneVR.Samples
{
    public class AmmoSocketable : HVREnumSocketable<AmmoType>
    {

    }

    public enum AmmoType
    {
        Mag556,
        ShotgunShell
    }
}