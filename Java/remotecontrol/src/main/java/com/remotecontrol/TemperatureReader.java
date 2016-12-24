package com.remotecontrol;

import com.remotecontrol.ZoneConfiguration.Zone;

public interface TemperatureReader
{
   float read(Zone zone_) throws Exception;
}
