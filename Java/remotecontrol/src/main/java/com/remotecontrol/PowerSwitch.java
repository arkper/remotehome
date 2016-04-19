package com.remotecontrol;

public interface PowerSwitch {
	String getState(Zone zone) throws Exception;
	String setState(Zone zone, String newState) throws Exception;

}
