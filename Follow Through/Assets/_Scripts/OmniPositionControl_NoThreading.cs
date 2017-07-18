using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;

//TODO: Eliminate stop command and/or add "slow to stop"
//			 (z)	
//			 -
//		  	back
// -  left-------right +   (x)
//          front
//			  +

public class OmniPositionControl_NoThreading : MonoBehaviour {

	public GameObject targetObject;		// Object to be tracked
	bool trackObject = false;

	public PID PIDx;
	public PID PIDz;
    public System.Int16 offsetAngle = 100;

	public System.Int16 targetSpeed = 100;
	public System.Int16 targetDirection = 45;
	public System.Int16 targetOmega = 0;
	private byte omniAddress = 155;
	private byte moveCommand = 127;
	private byte stopCommand = 126;

    private bool sending = false;
    private float lastSendTime = 0.0F;
    private float sendRate = 0.01F;

    public float angle;

    // TODO: make these private, clean up
	public float xOutput;
	public float zOutput;
	public Vector3 dir;

	// Use this for initialization
	void Start () {
		Debug.Log ("Press 'S' to start Omni Platform position tracking.");
        Debug.Log ("Press 'X' to send stop command to Omni Platform.");
	}
	
	// Update is called once per frame
	void Update () {
		
		// Get positions
		Vector3 targetPosition = targetObject.transform.position;
		Vector3 currentPosition = transform.position;

		// Use PID for x and z velocity outputs
		xOutput = PIDx.Update(targetPosition.x,currentPosition.x, Time.deltaTime);
		zOutput = PIDz.Update(targetPosition.z,currentPosition.z, Time.deltaTime);

		// Compute magnitude
		float outputMagnitude = Mathf.Sqrt(xOutput*xOutput + zOutput*zOutput);

		// Compute direction
		//Vector3 dir = targetPosition - currentPosition;
		dir = new Vector3(xOutput, 0, zOutput);							// direction vector based on outputs
		dir = transform.InverseTransformDirection(dir); //targetObject.transform.InverseTransformDirection(dir);	// Note: may not need "targetObject"
		angle = ((Mathf.Atan2(dir.z, dir.x) * Mathf.Rad2Deg) + offsetAngle + 360)%360;

		// Push to commands
		targetSpeed	= (System.Int16)outputMagnitude;
		targetDirection = (System.Int16)angle;			// TODO: may need to add an offset before sending
		targetOmega = 0;

		byte [] speedBytes = BitConverter.GetBytes(targetSpeed);
		byte [] directionBytes = BitConverter.GetBytes(targetDirection);
		byte [] omegaBytes = BitConverter.GetBytes(targetOmega);

		byte[] msg = {
			//omniAddress,
			moveCommand,
			speedBytes [0],
			speedBytes [1],
			directionBytes [0],
			directionBytes [1],
			omegaBytes [0],
			omegaBytes [1]
		};


        if (sending && ((Time.time - lastSendTime)>sendRate)) {
            lastSendTime = Time.time;
            Serial.Write (msg, 0, 7);
            //Debug.Log ("Sent move command to omni.");
        }

		if (Input.GetKeyDown (KeyCode.X)) {
			byte[] stopMSG = {moveCommand, 0, 0, 0, 0, 0, 0};
			Serial.Write (stopMSG, 0, 7);
			Debug.Log ("Sent stop command to omni.");
            sending = false;
		}
		// Press 'S' to send data to Omni Platform
		else if (Input.GetKeyDown (KeyCode.S)) {
			sending = !sending;
		}
			
	}

    void OnSerialLine(string line) {
 		Debug.Log(line);
 	}


}

