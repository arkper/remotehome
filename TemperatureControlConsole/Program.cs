using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TempControlLib;
using System.Threading;
using log4net;
using log4net.Config;

namespace TemperatureControlConsole
{
    class Program
    {
        private static readonly ILog logger =
           LogManager.GetLogger(typeof(Program));

        static void Main(string[] args)
        {
            XmlConfigurator.Configure();

            int zones = TemperatureControlConsole.Properties.Settings.Default.Zones;
            int interval = TemperatureControlConsole.Properties.Settings.Default.Interval;

            logger.Debug("Initializing...");

            string tempZonesJson = TemperatureControlConsole.Properties.Settings.Default.TempZones;

            logger.Debug("Got configuration info");

            TempController.init(tempZonesJson);

            logger.Debug("Initialized. Starting control loop...");

            while (true)
            {
                for (int z = 1; z <= zones; z++)
                {
                    TempZone tz = TempController.getTemperatureZone(z);

                    string msg = String.Format("Zone: {0}, SP: {1}, T: {2}, State: {3}", tz.id, tz.setpoint, tz.feedback, tz.currentState);

                    if (tz.auto)
                    {
                        if (tz.currentState == "on" && (tz.feedback - tz.setpoint) > tz.hysterisis)
                        {
                            TempController.switchZone(tz, "off");
                            msg += ": Turning the output off";
                        }
                        else if (tz.currentState == "off" && (tz.setpoint - tz.feedback) > tz.hysterisis)
                        {
                            TempController.switchZone(tz, "on");
                            msg += ": Turning the output on";
                        }
                        else
                        {
                            msg += ": Keeping things where they are";
                        }

                    }

                    //Console.WriteLine (msg);
                    logger.Info (msg);

                }
                Thread.Sleep(interval);
            }



        }
    }
}
