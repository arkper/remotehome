package com.remotecontrol;
import java.util.List;

import javax.ws.rs.GET;
import javax.ws.rs.Path;
import javax.ws.rs.PathParam;
import javax.ws.rs.Produces;

import org.apache.commons.io.FileUtils;
import org.apache.log4j.Logger;

import java.io.*;
//import org.springframework.stereotype.Component;

 
@Path("/zones")
public class RemoteControlService {
	
	private static final Logger logger = Logger.getLogger(RemoteControlService.class);
	
	private List<Zone> zones;
	public List<Zone> getZones() {
		return zones;
	}
	public void setZones(List<Zone> zones) {
		this.zones = zones;
	}

	private TemperatureReader temperatureReader;
	public TemperatureReader getTemperatureReader() {
		return temperatureReader;
	}
	public void setTemperatureReader(TemperatureReader temperatureReader) {
		this.temperatureReader = temperatureReader;
	}
	
	private PowerSwitch powerSwitch;
	public PowerSwitch getPowerSwitch() {
		return powerSwitch;
	}
	public void setPowerSwitch(PowerSwitch powerSwitch) {
		this.powerSwitch = powerSwitch;
	}

	@GET
	@Path("/all")
	@Produces("application/json")
	public List<Zone> getAllZones() throws Exception {
		for (int i=0; i<zones.size(); i++)
		{
			getZone(i);
		}
		return zones;
	}

	@GET
	@Path("/{param}")
	@Produces("application/json")
	public Zone getZone(@PathParam("param") Integer zoneId_) throws Exception {
		Zone zone = zones.get(zoneId_);
		
		getSetpoint(zone);
		
		getFeedback(zone);
		
		getState(zone);
		
		return zone;
	}
	
	@GET
	@Path("/{zoneId}/sp/{newSp}")
	@Produces("application/json")
	public List<Zone> setNewSetpoint(@PathParam("zoneId") Integer zoneId_, @PathParam("newSp") Float newSp_ ) throws Exception {
		logger.debug(String.format("setNewSetpoint for %s new sp %s", zoneId_.toString(), newSp_.toString()));
		
		Zone zone = zones.get(zoneId_-1);

		FileUtils.writeStringToFile(new File(zone.getSetpointFile()), newSp_.toString() + "," + zone.isAuto());
		
		zone.setSetpoint(newSp_);
		
		logger.debug(String.format("setNewSetpoint for %s new sp %s completed", zoneId_.toString(), newSp_.toString()));

		return getAllZones();
	}
	

	@GET
	@Path("/{zoneId}/auto/{newAuto}")
	@Produces("application/json")
	public List<Zone> setNewAutomode(@PathParam("zoneId") Integer zoneId_, @PathParam("newAuto") Boolean newAuto_ ) throws Exception {
		logger.debug(String.format("setNewAutomode for %s new sp %s", zoneId_.toString(), newAuto_.toString()));
		
		Zone zone = zones.get(zoneId_-1);

		FileUtils.writeStringToFile(new File(zone.getSetpointFile()), "" + zone.getSetpoint() + "," + newAuto_);
		
		zone.setAuto(newAuto_);
		
		logger.debug(String.format("setNewAutomode for %s new sp %s completed", zoneId_.toString(), newAuto_));

		return getAllZones();
	}
	

	@GET
	@Path("/{zoneId}/newState/{newState}")
	@Produces("application/json")
	public List<Zone> setNewState(@PathParam("zoneId") Integer zoneId_, @PathParam("newState") String newState_ ) throws Exception {
		logger.debug(String.format("setNewState for %s new state %s", zoneId_.toString(), newState_));
		
		Zone zone = zones.get(zoneId_-1);

		
		zone.setCurrentState(powerSwitch.setState(zone, newState_));
		
		logger.debug(String.format("setNewState for %s new state %s completed", zoneId_.toString(), newState_));

		return getAllZones();
	}
	
	public void getSetpoint(Zone zone) throws Exception
	{
		String setpointString = 
			FileUtils.readFileToString(new File(zone.getSetpointFile()));
		
		String[] data = setpointString.split(",");
		
		if (data != null && data.length > 0)
			zone.setSetpoint(Float.parseFloat(data[0]));
		else
			throw new Exception ("Couldn't read setpoint setting");
			
		
		if (data.length > 1)
			zone.setAuto (Boolean.parseBoolean(data[1]));
		else
			throw new Exception ("Couldn't read auto setting");
	}
	
	public void getFeedback(Zone zone) throws Exception
	{
		temperatureReader.read(zone);
	}
	
	public void getState(Zone zone) throws Exception
	{
		zone.setCurrentState (powerSwitch.getState(zone));
		
	}
}

