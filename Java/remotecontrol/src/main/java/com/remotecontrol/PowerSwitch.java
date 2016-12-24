package com.remotecontrol;

import com.remotecontrol.ZoneConfiguration.Zone;

public interface PowerSwitch {
	String getState(Zone zone) throws Exception;
	String setState(Zone zone, String newState) throws Exception;

}
