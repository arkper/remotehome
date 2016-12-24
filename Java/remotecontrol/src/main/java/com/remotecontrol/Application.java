package com.remotecontrol;

import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.boot.context.properties.EnableConfigurationProperties;
import org.springframework.context.annotation.Bean;

@SpringBootApplication
@EnableConfigurationProperties
public class Application {

	public static void main(String[] args) {
		SpringApplication.run(Application.class, args);
	}

	@Bean(name = "temperatureReader")
	public TemperatureReader temperatureReader() {
		return new OneWireTemperatureReader();
	}

	@Bean(name = "powerSwitch")
	public PowerSwitch powerSwitch(final ZoneConfiguration zoneConfiguration) {
		final X10PowerSwitch x10 = new X10PowerSwitch();
		x10.init(zoneConfiguration.getZones());
		return x10;
	}


}