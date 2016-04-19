using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using TempControlLib;
using System.IO;

namespace RemoteControl
{
    public partial class TempControl : System.Web.UI.Page
    {
        private static string tempZoneJson = null;
        static TempControl()
        {
            System.Configuration.Configuration rootWebConfig1 =
                System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration("/");

            if (rootWebConfig1.AppSettings.Settings.Count > 0)
            {
                System.Configuration.KeyValueConfigurationElement customSetting =
                    rootWebConfig1.AppSettings.Settings["TempZones"];
                if (customSetting != null)
                {
                    Console.WriteLine("TempZones application string = \"{0}\"",
                        customSetting.Value);

                    tempZoneJson = customSetting.Value;

                    TempController.init(tempZoneJson);
                }
                else
                    Console.WriteLine("No customsetting1 application string");
            }


        }
        
        protected void Page_Load(object sender, EventArgs e)
        {

            if (Request.Params["op"] != null)
            {
                string op = Request.Params["op"];
                switch (op)
                {
                    case "getState":
                        getState();
                        break;
                    case "setState":
                        string zoneId = Request.Params["zoneId"];
                        string state = Request.Params["state"];
                        setState(int.Parse(zoneId), state);
                        break;
                    case "set":
                        zoneId = Request.Params["zoneId"];
                        string sp = Request.Params["sp"];
                        setSp(int.Parse(zoneId), sp);
                        break;
                    default:
                        throw new Exception("Operation is unsupported!");
                }

            }

        }

        private void getState()
        {
            string json = TempController.getTemperatureZonesJson();

            /*
            "[{\"id\":1,\"setpointFile\":\"/var/www/remotehome/sp1\",\"feedbackFile\":\"/sys/bus/w1/devices/10-0008027cac02/w1_slave\",\"setpoint\":\"60\",\"feedback\":73.51,\"outputGroup\":\"a\",\"outputId\":\"1\",\"currentState\":\"off\"}" +
            ",{\"id\":2,\"setpointFile\":\"/var/www/remotehome/sp1\",\"feedbackFile\":\"/sys/bus/w1/devices/10-0008027cac02/w1_slave\",\"setpoint\":\"60\",\"feedback\":73.51,\"outputGroup\":\"a\",\"outputId\":\"2\",\"currentState\":\"on\"}" +
            ",{\"id\":3,\"setpointFile\":\"/var/www/remotehome/sp1\",\"feedbackFile\":\"/sys/bus/w1/devices/10-0008027cac02/w1_slave\",\"setpoint\":\"60\",\"feedback\":73.51,\"outputGroup\":\"a\",\"outputId\":\"3\",\"currentState\":\"off\"}]";
            */

            Response.Clear();
            Response.ContentType = "application/json; charset=utf-8";
            Response.Write(json);
            Response.End();
        }

        private void setState(int zoneId, string newState)
        {
            TempZone tz = TempController.getTemperatureZone(zoneId);

            TempController.switchZone(tz, newState);
            TempController.getCurrentState(tz);
            Response.Clear();
            Response.ContentType = "application/json; charset=utf-8";
            Response.Write(tz.currentState);
            Response.End();
        }

        private void setSp(int zoneId, string newSp)
        {
            TempZone tz = TempController.getTemperatureZone(zoneId);
            File.WriteAllText(tz.setpointFile, newSp);
            
        }

    
    }
}

