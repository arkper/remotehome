package com.remotecontrol;
import java.util.List;

import javax.annotation.PostConstruct;
import javax.ws.rs.GET;
import javax.ws.rs.Produces;

import org.apache.commons.io.FileUtils;
import org.apache.log4j.Logger;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;
import org.springframework.web.bind.annotation.PathVariable;




import com.remotecontrol.ZoneConfiguration.Zone;

import java.io.*;

 

@RestController
@RequestMapping("/rest/zones")
public class RemoteControlService {
	
	private static final Logger logger = Logger.getLogger(RemoteControlService.class);
	
	@Autowired
	private ZoneConfiguration zoneConfig;
	
	private List<Zone> zones;
	
	public List<Zone> getZones() {
		return zoneConfig.getZones();
	}

	@PostConstruct
	public void init() {
		this.zones = zoneConfig.getZones();
	}
	
	
	@Autowired
	private TemperatureReader temperatureReader;
	public TemperatureReader getTemperatureReader() {
		return temperatureReader;
	}
	public void setTemperatureReader(TemperatureReader temperatureReader) {
		this.temperatureReader = temperatureReader;
	}
	
	@Autowired
	private PowerSwitch powerSwitch;
	public PowerSwitch getPowerSwitch() {
		return powerSwitch;
	}
	public void setPowerSwitch(PowerSwitch powerSwitch) {
		this.powerSwitch = powerSwitch;
	}

	@GET
	@RequestMapping("/all")
	@Produces("application/json")
	public List<Zone> getAllZones() throws Exception {
		for (int i=0; i<zones.size(); i++)
		{
			getZone(i);
		}
		return zones;
	}

	@GET
	@RequestMapping("/{zoneId}")
	@Produces("application/json")
	public Zone getZone(@PathVariable ("zoneId") Integer zoneId_) throws Exception {
		Zone zone = zones.get(zoneId_);
		
		getState(zone);

		if (zone.isTemperature()) {
			getSetpoint(zone);
			
			getFeedback(zone);
		}
		
		return zone;
	}
	
	@GET
	@RequestMapping("/{zoneId}/sp/{newSp}")
	@Produces("application/json")
	public List<Zone> setNewSetpoint(@PathVariable("zoneId") Integer zoneId_, @PathVariable("newSp") Float newSp_ ) throws Exception {
		logger.debug(String.format("setNewSetpoint for %s new sp %s", zoneId_.toString(), newSp_.toString()));
		
		Zone zone = zones.get(zoneId_-1);

		FileUtils.writeStringToFile(new File(zone.getSetpointFile()), newSp_.toString() + "," + zone.isAuto());
		
		zone.setSetpoint(newSp_);
		
		logger.debug(String.format("setNewSetpoint for %s new sp %s completed", zoneId_.toString(), newSp_.toString()));

		return getAllZones();
	}
	

	@GET
	@RequestMapping("/{zoneId}/auto/{newAuto}")
	@Produces("application/json")
	public List<Zone> setNewAutomode(@PathVariable("zoneId") Integer zoneId_, @PathVariable("newAuto") Boolean newAuto_ ) throws Exception {
		logger.debug(String.format("setNewAutomode for %s new sp %s", zoneId_.toString(), newAuto_.toString()));
		
		Zone zone = zones.get(zoneId_-1);

		FileUtils.writeStringToFile(new File(zone.getSetpointFile()), "" + zone.getSetpoint() + "," + newAuto_);
		
		zone.setAuto(newAuto_);
		
		logger.debug(String.format("setNewAutomode for %s new sp %s completed", zoneId_.toString(), newAuto_));

		return getAllZones();
	}
	

	@GET
	@RequestMapping("/{zoneId}/newState/{newState}")
	@Produces("application/json")
	public List<Zone> setNewState(@PathVariable("zoneId") Integer zoneId_, @PathVariable("newState") String newState_ ) throws Exception {
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

