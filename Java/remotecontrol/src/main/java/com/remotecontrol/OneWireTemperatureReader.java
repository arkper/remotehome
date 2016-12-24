package com.remotecontrol;

import java.io.BufferedReader;
import java.io.FileReader;
import java.math.BigDecimal;

import com.remotecontrol.ZoneConfiguration.Zone;

public class OneWireTemperatureReader implements TemperatureReader
{
   public float read(Zone zone_) throws Exception
   {
	   BufferedReader reader = null;
	   
	   try
	   {
	       reader = new BufferedReader( new FileReader (zone_.getFeedbackFile()));
	       reader.readLine();
	       String line = reader.readLine();
	       String tempStr = line.split("t=")[1];
	       float reading = Float.parseFloat(tempStr);
	       reading =  reading * (float)0.0018 + 32;
	       reading = BigDecimal.valueOf(reading).setScale(1, BigDecimal.ROUND_HALF_UP).floatValue();
	       //System.out.println(reading);
	       zone_.setFeedback(reading);
	       
	       return reading;
	   }
	   finally {
		   if (reader != null)
		   {
			   try {
				   reader.close();
			   }
			   catch (Exception e) {}
		   }
	   }

   }
}
