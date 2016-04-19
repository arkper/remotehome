package com.remotecontrol;

import org.apache.log4j.Logger;
import org.springframework.context.ApplicationContext;
import org.springframework.context.support.AbstractApplicationContext;
import org.springframework.context.support.ClassPathXmlApplicationContext;

public class TemperatureController {
	private static final Logger logger = Logger.getLogger(TemperatureController.class);

	private RemoteControlService remoteControlServiceBean;
	
	public RemoteControlService getRemoteControlServiceBean() {
		return remoteControlServiceBean;
	}

	public void setRemoteControlServiceBean(RemoteControlService remoteControlServiceBean) {
		this.remoteControlServiceBean = remoteControlServiceBean;
	}

	public void run() throws Exception
	{
		logger.debug("Starting control loop");
		
		for (Zone zone : remoteControlServiceBean.getAllZones())
		{
			float feedback = zone.getFeedback();
			
			float setpoint = zone.getSetpoint();
			
			float hysteresis = zone.getHysterisis();
			
			String currentState = zone.getCurrentState();
			
			String msg = String.format("Zone %s, SP: %f, FB: %f, State: %s ", zone.getId(), setpoint, feedback, currentState);
			
			if (setpoint - feedback > hysteresis && currentState.equalsIgnoreCase("off") )
			{
				msg += "Turning on";
				remoteControlServiceBean.setNewState( Integer.parseInt(zone.getId()), "on");
			}
			else if (feedback - setpoint > hysteresis && currentState.equalsIgnoreCase("on") )
			{
				msg += "Turning off";
				remoteControlServiceBean.setNewState( Integer.parseInt(zone.getId()), "off");
			}
			else
			{
				msg += "Keeping " + currentState;
				
			}
			
			logger.info(msg);
		}
	}
	
	public static void main(String[] args) throws Exception
	{
		ApplicationContext context = 
	             new ClassPathXmlApplicationContext("applicationContext-cron.xml");
		
		TemperatureController controller = (TemperatureController)context.getBean("runMeTask");
		
		controller.run();
		
		((AbstractApplicationContext)context).close();
	}

}
