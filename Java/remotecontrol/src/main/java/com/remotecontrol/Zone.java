package com.remotecontrol;

import java.io.Serializable;

import org.codehaus.jackson.annotate.JsonProperty;

public class Zone implements Serializable {
	private static final long serialVersionUID = 1L;
	
	@JsonProperty("id")
	private String id;
    
	@JsonProperty("setpointFile")
	private String setpointFile;

	@JsonProperty("feedbackFile")
	private String feedbackFile;
    
	@JsonProperty("setpoint")
	private float setpoint;
    
	@JsonProperty("feedback")
	private float feedback;
	
	@JsonProperty("outputGroup")
    private String outputGroup;
	
	@JsonProperty("outputId")
    private String outputId;
	
	@JsonProperty("currentState")
    private String currentState;
	
	@JsonProperty("hysterisis")
    private float hysterisis;
	
	@JsonProperty("auto")
    private boolean auto;

	@JsonProperty("calibration")
    private float calibration;

	@JsonProperty("ip")
    private String ip;

	@JsonProperty("port")
    private int port;
	
	public String getId() {
		return id;
	}
	public void setId(String id) {
		this.id = id;
	}
	public String getSetpointFile() {
		return setpointFile;
	}
	public void setSetpointFile(String setpointFile) {
		this.setpointFile = setpointFile;
	}
	public String getFeedbackFile() {
		return feedbackFile;
	}
	public void setFeedbackFile(String feedbackFile) {
		this.feedbackFile = feedbackFile;
	}
	public float getSetpoint() {
		return setpoint;
	}
	public void setSetpoint(float setpoint) {
		this.setpoint = setpoint;
	}
	public float getFeedback() {
		return feedback;
	}
	public void setFeedback(float feedback) {
		this.feedback = feedback;
	}
	public String getOutputGroup() {
		return outputGroup;
	}
	public void setOutputGroup(String outputGroup) {
		this.outputGroup = outputGroup;
	}
	public String getOutputId() {
		return outputId;
	}
	public void setOutputId(String outputId) {
		this.outputId = outputId;
	}
	public String getCurrentState() {
		return currentState;
	}
	public void setCurrentState(String currentState) {
		this.currentState = currentState;
	}
	public float getHysterisis() {
		return hysterisis;
	}
	public void setHysterisis(float hysterisis) {
		this.hysterisis = hysterisis;
	}
	public float getCalibration() {
		return calibration;
	}
	public void setCalibration(float calibration) {
		this.calibration = calibration;
	}
	public boolean isAuto() {
		return auto;
	}
	public void setAuto(boolean auto) {
		this.auto = auto;
	}
	public String getIp() {
		return ip;
	}
	public void setIp(String ip) {
		this.ip = ip;
	}
	public int getPort() {
		return port;
	}
	public void setPort(int port) {
		this.port = port;
	}
    
	
    
    

}
