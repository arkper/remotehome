using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Web.Script.Serialization;
using log4net;


namespace TempControlLib
{
    public class TempController
    {
        private static readonly ILog logger =
           LogManager.GetLogger(typeof(TempController));

        private static IRemoteController remoteController;

        private static TempZone[] zones = null;

        public static void switchZone(TempZone tz_, string newState_)
        {
            remoteController.switchZone(tz_, newState_);
        }

        public static String getTemperatureZonesJson()
        {
            foreach (TempZone zone in zones)
            {
                getTemperatureZone(int.Parse(zone.id));
            }

            try
            {
                return new JavaScriptSerializer().Serialize(zones);
            }
            catch (Exception e)
            {
                logger.Error("Error getting zone information JSON", e);
                return null;
            }

        }

        public static TempZone getTemperatureZone(int zoneId_)
        {
            if (zones == null)
                init("");

            TempZone zone = getTempZone(zoneId_);

            try
            {
                //Get current state of the output for the zone
                logger.Debug("Getting output state for zone " + zone.id);

                remoteController.getCurrentState(zone);

                //Get the setpoint for the zone
                logger.Debug("Getting SP for zone " + zone.id);
                getSetpoint(zone);

                //Get temperature for the zone
                logger.Debug("Getting temperature for zone " + zone.id);

                TEMPer tempr = new TEMPer(TEMPerDeviceType.TEMPerHUM, 0);
                //logger.Debug("Raw Temperature for zone " + zone.id + " is " + tempr.Temperature);
                if (tempr.Temperature != 0)
                {
                    zone.feedback = (float)tempr.Temperature * 9 / 5 + 32 + zone.calibration;

                    if (zone.feedbackFile != null && !zone.feedbackFile.Equals("") )
                    {
                        byte[] info = new UTF8Encoding(true).GetBytes(zone.feedback.ToString() + "," + zone.currentState);
                        File.WriteAllBytes(zone.feedbackFile, info);
                    }
                }

                //Console.WriteLine("Current temperature is : " + zone.feedback);

            }
            catch (Exception e)
            {
                logger.Error("Error getting zone temperature", e);
                return null;
            }

            return zone;
        }

        public static void getSetpoint(TempZone tz_)
        {
            try
            {
                string data = File.ReadAllText(tz_.setpointFile);
                tz_.setpoint = float.Parse (data.Split(new char[]{','})[0]);
                tz_.auto = bool.Parse(data.Split(new char[] { ',' })[1]);
            }
            catch (Exception ex)
            {
                logger.Error("Error getting setpoint for zone " + tz_.id, ex);
            }
        }

        public static void init(string tempZonesJson_)
        {

            logger.Debug("Setting configuration info...");


            remoteController = new WeMoRemoteController();
            
            zones = new JavaScriptSerializer().Deserialize<TempZone[]>(tempZonesJson_);

            getTemperatureZonesJson();
        }

        private static TempZone getTempZone(int id_)
        {
            foreach (TempZone tz in zones)
            {
                if (tz.id == "" + id_)
                    return tz;
            }
            return null;
        }

    }




    public class TempZone
    {
        public string id { get; set; }
        public string setpointFile { get; set; }
        public string feedbackFile { get; set; }
        public float setpoint { get; set; }
        public float feedback { get; set; }
        public string outputGroup { get; set; }
        public string outputId { get; set; }
        public string currentState { get; set; }
        public float hysterisis { get; set; }
        public float calibration { get; set; }
        public bool auto { get; set; }
        public string ip { get; set; }
        public int port { get; set; }

        public TempZone() { }
    
    }


}
