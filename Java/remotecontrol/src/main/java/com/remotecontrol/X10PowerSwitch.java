package com.remotecontrol;

import java.io.BufferedReader;
import java.io.DataOutputStream;
import java.io.IOException;
import java.io.InputStreamReader;
import java.net.Socket;
import java.util.List;

import com.remotecontrol.ZoneConfiguration.Zone;

public class X10PowerSwitch implements PowerSwitch {
	
	public void init(final List<Zone> zones) throws X10ControllerException {
		for (Zone zone : zones) {
			setState (zone, "off");
		}
	}

	@Override
	public String getState(Zone zone) throws X10ControllerException {
		try (Socket clientSocket = new Socket(zone.getIp(), zone.getPort());) {
			clientSocket.setSoTimeout(1000);
			DataOutputStream outToServer = new DataOutputStream(
					clientSocket.getOutputStream());
			BufferedReader inFromServer = new BufferedReader(
					new InputStreamReader(clientSocket.getInputStream()));
			final String command = "st " + "\n";
			outToServer.write(command.getBytes());

			String currentLine = null;
			try {
				while ((currentLine = inFromServer.readLine()) != null) {

					if (currentLine.contains("House A: 1=")) {
						int index = currentLine.indexOf("House A: 1=");
						String combinedStatus = currentLine
								.substring(index + 9);

						String numericStatus = combinedStatus.split(",")[Integer
								.parseInt(zone.getOutputId()) - 1].split("=")[1];
						
						return numericStatus.equals("1") ? "on" : "off";
						
					} else {
						continue;
					}
				}
				return null;
			} catch (java.net.SocketTimeoutException e) {
				throw new X10ControllerException(e);

			}

		} catch (IOException e) {

			throw new X10ControllerException(e);

		}
	}

	@Override
	public String setState(Zone zone, String newState) throws X10ControllerException {
		try (Socket clientSocket = new Socket(zone.getIp(), zone.getPort());) {
			DataOutputStream outToServer = new DataOutputStream(
					clientSocket.getOutputStream());
			BufferedReader inFromServer = new BufferedReader(
					new InputStreamReader(clientSocket.getInputStream()));
			final String command = "rf " + zone.getOutputGroup()
					+ zone.getOutputId() + " " + newState + "\n";
			outToServer.write(command.getBytes());

			final String response = inFromServer.readLine();

			System.out.println(response);

			return response.substring(response.indexOf("Func:") + 5);

		} catch (IOException e) {

			throw new X10ControllerException(e);

		}
	}

}
