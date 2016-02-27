using System;

namespace osuBancho.Core
{
    [Flags]
    public enum SlotStatus
    {
        Open = 1,
        Locked = 2,
        NotReady = 4,
        Ready = 8,
        NoMap = 16,
        Playing = 32,
        flag_6 = 64,
        Occupied = 124,
        Quit = 128
    }
}
