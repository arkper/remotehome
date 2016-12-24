package com.remotecontrol;

import org.apache.log4j.Logger;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.scheduling.annotation.Scheduled;
import org.springframework.stereotype.Component;

import com.remotecontrol.ZoneConfiguration.Zone;

@Component
public class TemperatureController {
	private static final Logger logger = Logger
			.getLogger(TemperatureController.class);

	@Autowired
	private RemoteControlService remoteControlServiceBean;

	@Scheduled(fixedRateString = "${scan.interval}")
	public void run() throws Exception {
		logger.debug("Starting control loop");

		for (Zone zone : remoteControlServiceBean.getAllZones()) {
			if (zone.isTemperature() && zone.isAuto()) {
				float feedback = zone.getFeedback();

				float setpoint = zone.getSetpoint();

				float hysteresis = zone.getHysterisis();

				String currentState = zone.getCurrentState();

				String msg = String.format(
						"Zone %s, SP: %f, FB: %f, State: %s ", zone.getId(),
						setpoint, feedback, currentState);

				if (setpoint - feedback > hysteresis
						&& currentState.equalsIgnoreCase("off")) {
					msg += "Turning on";
					remoteControlServiceBean.setNewState(
							Integer.parseInt(zone.getId()), "on");
				} else if (feedback - setpoint > hysteresis
						&& currentState.equalsIgnoreCase("on")) {
					msg += "Turning off";
					remoteControlServiceBean.setNewState(
							Integer.parseInt(zone.getId()), "off");
				} else {
					msg += "Keeping " + currentState;

				}

				logger.info(msg);
			}
		}
	}

}
