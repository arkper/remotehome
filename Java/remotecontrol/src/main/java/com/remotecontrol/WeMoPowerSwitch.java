package com.remotecontrol;

import java.io.ByteArrayInputStream;

import javax.xml.parsers.DocumentBuilder;
import javax.xml.parsers.DocumentBuilderFactory;

import org.apache.http.HttpEntity;
import org.apache.http.HttpResponse;
import org.apache.http.client.HttpClient;
import org.apache.http.client.methods.HttpPost;
import org.apache.http.entity.ByteArrayEntity;
import org.apache.http.impl.client.HttpClientBuilder;
import org.apache.http.util.EntityUtils;
import org.apache.log4j.Logger;
import org.w3c.dom.Document;

import com.remotecontrol.ZoneConfiguration.Zone;

public class WeMoPowerSwitch implements PowerSwitch{

	private static final Logger logger = Logger.getLogger(WeMoPowerSwitch.class);

	private String _serviceUri;
	
	public WeMoPowerSwitch(String serviceUri_)
	{
		_serviceUri = serviceUri_;
	}

	public String getState(Zone zone) throws Exception {
		HttpClient client = HttpClientBuilder.create().build();
		HttpPost request = new HttpPost("http://" + zone.getIp() + ":" + zone.getPort() + _serviceUri);

		String requestBody = "<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\">\n"
				+ "<s:Body xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\n"
				+ "<GetBinaryState xmlns=\"urn:Belkin:service:basicevent:1\"/>\n" + "</s:Body>\n" + "</s:Envelope>\n";

		logger.debug(requestBody);
		request.setHeader("Content-Type", "text/xml;charset=UTF-8");
		request.setHeader("SOAPAction", "\"urn:Belkin:service:basicevent:1#GetBinaryState\"");

		HttpEntity entity = new ByteArrayEntity(requestBody.getBytes("UTF-8"));
		request.setEntity(entity);

		HttpResponse response = client.execute(request);
		String result = EntityUtils.toString(response.getEntity());

		logger.debug(result);

		DocumentBuilderFactory dbFactory = DocumentBuilderFactory.newInstance();
		DocumentBuilder dBuilder = dbFactory.newDocumentBuilder();
		Document doc = dBuilder.parse(new ByteArrayInputStream(result.getBytes()));

		String state = doc.getElementsByTagName("BinaryState").item(0).getTextContent();

		logger.debug("Device state: " + state);

		return state.equals("1") ? "on" : "off";
	

	}

	
	
	public String setState(Zone zone, String newState) throws Exception {
		HttpClient client = HttpClientBuilder.create().build();
		HttpPost request = new HttpPost("http://" + zone.getIp() + ":" + zone.getPort() + _serviceUri);

		String requestBody = "<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\">\n"
				+ "<s:Body xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\n"
				+ "<SetBinaryState xmlns=\"urn:Belkin:service:basicevent:1\">\n"
				+ "<BinaryState>" + (newState.equalsIgnoreCase("on")? "1" : "0") + "</BinaryState>\n"
				+ "</SetBinaryState>\n"
 				+ "</s:Body>\n" + "</s:Envelope>\n";

		logger.debug(requestBody);
		request.setHeader("Content-Type", "text/xml;charset=UTF-8");
		request.setHeader("SOAPAction", "\"urn:Belkin:service:basicevent:1#SetBinaryState\"");

		HttpEntity entity = new ByteArrayEntity(requestBody.getBytes("UTF-8"));
		request.setEntity(entity);

		HttpResponse response = client.execute(request);
		String result = EntityUtils.toString(response.getEntity());

		logger.debug(result);

		DocumentBuilderFactory dbFactory = DocumentBuilderFactory.newInstance();
		DocumentBuilder dBuilder = dbFactory.newDocumentBuilder();
		Document doc = dBuilder.parse(new ByteArrayInputStream(result.getBytes()));

		String state = doc.getElementsByTagName("BinaryState").item(0).getTextContent();

		logger.debug("Device state: " + state);

		return state.equals("1") ? "on" : "off";

	}

	
}
