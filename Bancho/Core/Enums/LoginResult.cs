using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace osuBancho.Core
{
    public enum LoginResult
    {
        Success,
        Failed,
        OldVersion,
        Banned,
        Punished,
        Error,
        NeedSupporter,
        PasswordReset,
        NeedVerification
    }
}
