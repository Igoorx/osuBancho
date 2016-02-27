using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace osuBancho.Core
{
    [Flags]
    internal enum UserTags
    {
        None = 0,
        Player = 1,
        BAT = 2,
        Supporter = 4,
        Peppy = 8,
        Admin = 16
    }
}
