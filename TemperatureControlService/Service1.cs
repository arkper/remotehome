using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using TempControlLib;
using System.Threading;
using log4net;
using log4net.Config;



namespace TemperatureControlService
{

    public partial class Service1 : ServiceBase
    {
        private static readonly ILog logger =
           LogManager.GetLogger(typeof(Service1));

        public Service1()
        {
            XmlConfigurator.Configure();
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {



            Thread t = new Thread(run);

            t.Start();

        }

        protected override void OnStop()
        {
        }

        static void run()
        {
            int zones = TemperatureControlService.Properties.Settings.Default.Zones;
            int interval = TemperatureControlService.Properties.Settings.Default.Interval;

            logger.Debug("Initializing...");

            try
            {
                string tempZonesJson = TemperatureControlService.Properties.Settings.Default.TempZones;

                logger.Debug("Got configuration info");

                TempController.init(tempZonesJson);

                logger.Debug("Initialized. Starting control loop...");

                while (true)
                {
                    for (int z = 1; z <= zones; z++)
                    {
                        TempZone tz = TempController.getTemperatureZone(z);

                        string msg = String.Format("Zone: {0}, SP: {1}, T: {2}, Current state: {3}", tz.id, tz.setpoint, tz.feedback, tz.currentState);

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

                        logger.Info(msg);
                        //EventLog.WriteEntry("Service1", msg, EventLogEntryType.Warning);

                    }
                    Thread.Sleep(interval);

                }
            }
            catch (Exception ex)
            {
                logger.Error ("Fatal Failure in the control loop", ex);
            }

        }
    }
}
