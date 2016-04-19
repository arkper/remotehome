package com.remotecontrol;

import java.io.File;

import org.apache.commons.io.FileUtils;

public class FileTemperatureReader implements TemperatureReader {

	public float read(Zone zone_) throws Exception {
		String feedbackString = 
			FileUtils.readFileToString(new File(zone_.getFeedbackFile()));
			
			String[] data = feedbackString.split(",");
			
			if (data != null && data.length > 0)
				zone_.setFeedback(Float.parseFloat(data[0]));
			else
				throw new Exception ("Couldn't read feedback");
				
		return zone_.getFeedback();
	}

}
