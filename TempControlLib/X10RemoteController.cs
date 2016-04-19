using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ActiveHomeScriptLib;
using log4net;

namespace TempControlLib
{

    class X10RemoteController : IRemoteController
    {
        private static readonly ILog logger =
            LogManager.GetLogger(typeof(X10RemoteController));

        private static ActiveHome ah = new ActiveHome();

        public int getCurrentState(TempZone tz_)
        {
            try
            {
                object response = ah.SendAction("queryplc", tz_.outputGroup + tz_.outputId + " on", null, null);
                tz_.currentState = (int)response == 0 ? "off" : "on";
                return (int)response;
            }
            catch (Exception e)
            {
                logger.Error("Error getting zone output state", e);
                tz_.currentState = null;
                return -1;
            }

        }

        public void switchZone(TempZone tz_, string newState_)
        {
            try
            {
                object ret = ah.SendAction("sendrf", tz_.outputGroup + tz_.outputId + " " + newState_, null, null);
                tz_.currentState = (int)ret == 0 ? "off" : "on";
            }
            catch (Exception e)
            {
                logger.Error("Error swwitching zone output", e);
            }

        }

    }
}
