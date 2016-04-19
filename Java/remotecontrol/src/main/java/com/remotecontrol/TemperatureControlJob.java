package com.remotecontrol;
import org.quartz.JobExecutionContext;
import org.quartz.JobExecutionException;
import org.springframework.scheduling.quartz.QuartzJobBean;

public class TemperatureControlJob extends QuartzJobBean {

	
	protected void executeInternal(JobExecutionContext context)
			throws JobExecutionException {
		
		TemperatureController temperatureController = (TemperatureController) context.getJobDetail().getJobDataMap().get("runMeTask");
		
		try {
			temperatureController.run();
		}
		catch (Exception e)
		{
			e.printStackTrace();
		}
	}
}
