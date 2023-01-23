using System;
using HurricaneVR.Framework.Core.Sockets;

namespace HurricaneVR.Samples
{
    public class ItemsSocketable : HVREnumFlagsSocketable<Items>
    {
    }

    [Flags]
    public enum Items
    {
        None = 0,
        Item = 1 << 0,
        LargeItem = 1 << 1,
        SmallWeapon = 1 << 2,
        MadsonD9Magazine = 1<<3,
        Ball = 1<<4,
        All = ~0
    }
}