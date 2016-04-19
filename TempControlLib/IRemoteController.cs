using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TempControlLib
{
    interface IRemoteController
    {
        int getCurrentState(TempZone tz_);

        void switchZone(TempZone tz_, string newState_);

    }
}
