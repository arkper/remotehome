using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using TempControlLib.ServiceReference1;
using System.ServiceModel;

namespace TempControlLib
{
    class WeMoRemoteController : IRemoteController
    {

        private static readonly ILog logger =
            LogManager.GetLogger(typeof(WeMoRemoteController));


        public int getCurrentState(TempZone tz_)
        {
            try
            {
                GetBinaryStateResponse state = getClient(tz_).GetBinaryState(new GetBinaryState());
                logger.Debug(String.Format("Switch is currently set to: {0}", state.BinaryState));

                tz_.currentState = state.BinaryState.Equals("1") ? "on" : "off";

                return Int16.Parse(state.BinaryState);

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
                logger.Debug("Turning Switch " + newState_);

                SetBinaryState message = new SetBinaryState { BinaryState = (newState_ == "on"? "1" : "0") };


                BasicServicePortTypeClient client = getClient(tz_);

                client.SetBinaryState(message);

                GetBinaryStateResponse state = client.GetBinaryState(new GetBinaryState());
                logger.Debug(String.Format("Switch is currently set to: {0}", state.BinaryState));

                tz_.currentState = state.BinaryState.Equals("1") ? "on" : "off";

            }
            catch (Exception e)
            {
                logger.Error("Error swwitching zone output", e);
            }

        }


        private BasicServicePortTypeClient getClient(TempZone tz_)
        {
            var client = new BasicServicePortTypeClient();
            client.Endpoint.Address = new EndpointAddress(string.Format("http://{0}:{1}/upnp/control/basicevent1", tz_.ip, tz_.port));

            return client;

        }


    }
}
