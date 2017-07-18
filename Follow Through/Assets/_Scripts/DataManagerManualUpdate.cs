
/// <summary>
/// Data manager manual update.
/// 
/// 
/// Update 6/28/2017: script deprecated. Functionality 
/// moved to SerialThread.cs
/// 
/// </summary>

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System;
using UnityEngine.UI;

public class DataManagerManualUpdate : MonoBehaviour {

	// shape display object in unity
	private ShapeCast shapeRenderer;
	public GameObject renderingPlane; 

	private SerialThread serialport;

	// conversion factor
	private float convFactor = 1000; // [mm/m]
	private int maxZ = 40;

	// Display automatic refresh rate
	public float refreshRate = 0.002f; //[s]
	// Enable sending data to hw at fixed rate
	private bool automaticSending = false;
	private float counter;

	// list to store the shape display pin positions
	private List<float> zMap = new List<float>();

	public bool debug = false;

	private const int DataCMD  = 127;
	private const int ZeroCMD  = 126;
	private const int StopCMD  = 125;
	private const int SetupCMD = 124;

	// slave setup commands
	private const int SET_KP 			=	253;
	private const int SET_KI 			=	248;
	private const int SET_KD 			=	247;
	private const int SET_LOWERINGSPEED	=	246;
	private const int DISABLE_PIN 		=	245;

	void Start () {
		
		shapeRenderer = renderingPlane.GetComponent<ShapeCast> (); 

		// Get serialport instance
		serialport = SerialThread.Instance;
		// Register for a notification of the SerialDataReceivedEvent
		SerialThread.SerialReceivedDataEvent += 
			new SerialThread.SerialReceivedDataEventHandler(Serial_SerialReceivedEvent);

		Debug.Log ("Initialized DataManager");
		Debug.Log ("Press 'S' to send data continuously.");
		Debug.Log ("Press 'A' to send data once.");
		Debug.Log ("Press 'R' to reset the display.");
		Debug.Log ("Press 'X' to stop the display.");
		Debug.Log ("Press 'P' to print the current values on the display.");
		Debug.Log ("Press 'I' to resend slave parameters");

	}
	
	// Update is called once per frame
	void Update () {
		
	} //end Update

	void OnDestroy() {
		// If we are registered for a notification of the 
		// SerialThread events then remove the registration
		SerialThread.SerialReceivedDataEvent -= Serial_SerialReceivedEvent;
	}
		

//	#region Shape display data parse functions
//
//	// Send slave initialization parameters
//	private void SetupSlaves() {
//
//		// Grab directory
//		string currDir = Directory.GetCurrentDirectory();
//		string path = currDir + "/Assets/Scripts/SlaveParameters";
//
//		// Send setup command so Teensy knows we're sending data
//		//int val2send = SetupCMD;
//		//char command = (char) val2send;
//		//Serial.Write( command.ToString());
//
//		// For each file in "SlaveParameters" folder
//		foreach (string file in Directory.GetFiles(path, "*.txt"))
//		{
//			using (StreamReader sr = new StreamReader (file)) {
//				// Read first line (SlaveID)
//				string line = sr.ReadLine();
//				while ((line == "" || line.StartsWith ("#")) && sr.Peek() >= 0) line = sr.ReadLine(); // Handle comments or newlines
//				if (sr.EndOfStream) continue;
//
//
//				string[] separators = new string[] { ":", ","};
//				string[] parsedID = line.Split(separators, StringSplitOptions.RemoveEmptyEntries);
//				int ID = int.Parse(parsedID [1].Trim ());
//
//				// Read command lines
//				while (sr.Peek () >= 0) {
//					line = sr.ReadLine ();
//					if (line == "" || line.StartsWith ("#")) {
//						// Handle comments or new lines
//						continue;
//					}
//						
//					string[] parsedLine = line.Split (separators, StringSplitOptions.RemoveEmptyEntries);
//					int cmd = 0; 
//					int pin, val;
//
//					// Parse command
//					if (parsedLine [0].ToLower () == "kp") {
//						cmd = SET_KP;
//					} else if (parsedLine [0].ToLower () == "ki") {
//						cmd = SET_KI;
//					} else if (parsedLine [0].ToLower () == "kd") {
//						cmd = SET_KD;
//					} else if (parsedLine [0].ToLower () == "ls") {
//						cmd = SET_LOWERINGSPEED;
//					} else if (parsedLine [0].ToLower () == "disable") {
//						cmd = DISABLE_PIN;
//					} else {
//						string err = "Error: Unknown command in Slave ID " + ID.ToString ();
//						Debug.LogError(err);
//						continue;
//					}
//
//					pin = int.Parse (parsedLine [1].Trim ());
//					val = int.Parse (parsedLine [2].Trim ());
//
//					// Send message 
//					byte[] msg = {(byte)SetupCMD, (byte)ID, (byte)cmd, (byte)pin, (byte)val};
//					Serial.Write (msg, 0, 5);
//				}
//				//Debug.LogFormat("Sent parameters for Slave {0}", ID);
//			}
//		}
//	}
//
//	// Send all the zMap data to the master shape display
//	private void RefreshShapeDisplay() {
//		// First send the command byte so Teensy knows we are sending data
//		int val2send = DataCMD;
//		char data2send = (char) val2send;
//
//		serialport.SendSerialData( data2send.ToString() );
//
//		// Now send the rest of the data
//		for (int i = 0; i < ( zMap.Count ); i++) {
//			// convert from m to mm
//			val2send = (int) ( zMap [i] * convFactor );
//			if (val2send > maxZ) {
//				val2send = maxZ;
//			}
//			data2send = (char) val2send;
//			serialport.SendSerialData( data2send.ToString() );
//		}
//		//Debug.Log ("Sent display data");
//	} // end RefreshShapeDisplay
//
//	// Send command to stop all motors and rendering
//	private void ResetShapeDisplay() {
//		// Send the reset command byte 
//		int val2send = ZeroCMD;
//		char data2send = (char) val2send;
//		serialport.SendSerialData( data2send.ToString() );
//	} // end ResetShapeDisplay
//
//	// Send command to stop all motors and rendering
//	private void StopShapeDisplay() {
//		// Send the stop command byte 
//		int val2send = StopCMD;
//		char data2send = (char) val2send;
//		serialport.SendSerialData( data2send.ToString() );
//	} // end StopShapeRendering
//
//	#endregion Shape display data parse functions


	#region Notification Events
	/// <summary>
	/// Data parsed serialport notification event
	/// </summary>
	/// <param name="Data">string</param>
	/// <param name="RawData">string[]</param>
	void Serial_SerialReceivedEvent(string[] Data, string RawData)
	{
		if (debug) 
			print("Data Recieved via port: " + RawData);
	}
	#endregion Notification Events


	// Prints the current pin values, useful for debugging
	private void printZmap() {
		int countZeros = 0;
		for (int i = 0; i < ( zMap.Count ); i++) {
			// convert from m to mm
			int val = (int) ( zMap [i] * convFactor );
			Debug.Log( "[" + i + "]  " + val );
			if (val == 0) {
				countZeros++;
			}
		}
		Debug.Log ("Real num of pins at zero: " + countZeros);
		Debug.Log ("Real num of pins up: " + (zMap.Count - countZeros) );
	}

} // end DataManager
	
