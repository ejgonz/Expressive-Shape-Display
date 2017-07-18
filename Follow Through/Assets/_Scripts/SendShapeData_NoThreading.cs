using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.IO;
using System.IO.Ports;
using System;


public class SendShapeData_NoThreading : MonoBehaviour {
	public bool debug = false;

	// Data sending variables
	// Display automatic refresh rate
	public float refreshRate = 0.05f; //[s]

	// Enable sending data to hw at fixed rate
	private bool automaticSending = false;
	private float counter;
	private bool setupSlaves = false;
	private bool refreshDisplay = false;    //true if there is pos data to send
	private bool resetDisplay = false;
	private bool stopDisplay = false;
	private List<float> zMap = new List<float>(); //array to send

	// shape display object in unity
	private ShapeCast shapeRenderer;
	public GameObject renderingPlane;

	// Message commands
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

	// conversion factor
	private float convFactor = 1000; // [mm/m]

	// Use this for initialization
	void Start () {
		// Get the shape display object
		shapeRenderer = renderingPlane.GetComponent<ShapeCast> ();

		// Initially send slave setup data
		setupSlaves = true;

		Debug.Log ("Initialized SerialThread");
		Debug.Log ("Press 'S' to send data continuously.");
		Debug.Log ("Press 'A' to send data once.");
		Debug.Log ("Press 'R' to reset the display.");
		Debug.Log ("Press 'X' to stop the display.");
		Debug.Log ("Press 'P' to print the current values on the display.");
		Debug.Log ("Press 'I' to resend slave parameters");
	}
	
	// Update is called once per frame
	void Update () {

		// Press 'S' to send data to Teensy continuously
		if (Input.GetKeyDown (KeyCode.S)) {
			automaticSending = !automaticSending;
			counter = refreshRate;
			if (debug) {
				if (automaticSending) {
					Debug.Log ("shape sending on");
				} else {
					Debug.Log ("shape sending off");
				}
			}
		}

		// Press 'A' to send data to Teensy manually
		else if (Input.GetKeyDown (KeyCode.A)) {
			// Refresh the display list data 
			zMap = shapeRenderer.GetZMap ();
			// Set flag to send data
			RefreshShapeDisplay();	// Added
			refreshDisplay = true;
		}

		// Press 'R' to zero and reset the display
		else if (Input.GetKeyDown (KeyCode.R)) {
			ResetShapeDisplay (); 	// Added
			resetDisplay = true;
			refreshDisplay = false;
			automaticSending = false;
			if (debug) {
				Debug.Log("Reset display");
			}
		}

		// Press 'X' to stop the display
		else if (Input.GetKeyDown (KeyCode.X)) {
			StopShapeDisplay ();	// Added
			stopDisplay = true;
			refreshDisplay = false;
			automaticSending = false;
		}

		// Press 'I' to resend slave parameters
		else if (Input.GetKeyDown (KeyCode.I)) {
			SetupSlaves ();			// Added
			setupSlaves = false;
			refreshDisplay = false;
			automaticSending = false;
		}

		// Press 'P' to print the current map data 
		else if (Input.GetKeyDown (KeyCode.P)) {
			// Refresh the display list data 
			zMap = shapeRenderer.GetZMap ();
			printZmap ();
		}

		// If auto mode enabled
		if (automaticSending) {
			counter -= Time.deltaTime; //decrement counter
			// if counter done, send the data
			if (counter < 0.0f) {
				// Refresh the display list data 
				zMap = shapeRenderer.GetZMap ();
				RefreshShapeDisplay (); 	//Added
				refreshDisplay = true;
				counter = refreshRate;
			}
		}
		
	}

	// Send slave initialization parameters
	private void SetupSlaves() {

		// Grab directory
		string currDir = Directory.GetCurrentDirectory();
		string path = currDir + "/Assets/Scripts/SlaveParameters";

		// For each file in "SlaveParameters" folder
		foreach (string file in Directory.GetFiles(path, "*.txt"))
		{
			using (StreamReader sr = new StreamReader(file))
			{
				// Read first line (SlaveID)
				string line = sr.ReadLine();
				while ((line == "" || line.StartsWith("#")) && sr.Peek() >= 0) line = sr.ReadLine(); // Handle comments or newlines
				if (sr.EndOfStream) continue;

				string[] separators = new string[] { ":", "," };
				string[] parsedID = line.Split(separators, StringSplitOptions.RemoveEmptyEntries);
				int ID = int.Parse(parsedID[1].Trim());

				// Read command lines
				while (sr.Peek() >= 0)
				{
					line = sr.ReadLine();
					if (line == "" || line.StartsWith("#"))
					{
						// Handle comments or new lines
						continue;
					}

					string[] parsedLine = line.Split(separators, StringSplitOptions.RemoveEmptyEntries);
					int cmd = 0;
					int pin, val;

					// Parse command
					if (parsedLine[0].ToLower() == "kp")
					{
						cmd = SET_KP;
					}
					else if (parsedLine[0].ToLower() == "ki")
					{
						cmd = SET_KI;
					}
					else if (parsedLine[0].ToLower() == "kd")
					{
						cmd = SET_KD;
					}
					else if (parsedLine[0].ToLower() == "ls")
					{
						cmd = SET_LOWERINGSPEED;
					}
					else if (parsedLine[0].ToLower() == "disable")
					{
						cmd = DISABLE_PIN;
					}
					else
					{
						string err = "Error: Unknown command in Slave ID " + ID.ToString();
						print(err);
						continue;
					}

					pin = int.Parse(parsedLine[1].Trim());
					val = int.Parse(parsedLine[2].Trim());

					// Send message 
					byte[] msg = { (byte)SetupCMD, (byte)ID, (byte)cmd, (byte)pin, (byte)val };
					Serial.Write(msg, 0, 5);
				}
			}
		}
	}

	// Send command to stop all motors and rendering
	private void StopShapeDisplay() {
		// Send the stop command byte 
		int val2send = StopCMD;
		char data2send = (char) val2send;
		Serial.Write( data2send.ToString() );
	}

	// Send all the zMap data to the master shape display
	private void RefreshShapeDisplay() {

		// First send the command byte so Teensy knows we are sending data
		int val2send = DataCMD;
		char data2send = (char)val2send;

		Serial.Write(data2send.ToString());

		// Now send the rest of the data
		for (int i = 0; i < (zMap.Count); i++)
		{
			// convert from m to mm
			val2send = (int)(zMap[i] * convFactor);
			data2send = (char)val2send;
			Serial.Write( data2send.ToString() );
		}

		//        // iterate through rows
		//        for (int j = 0; j < zMap.Count / 12; j++)
		//        {
		//            // iterate through the row values
		//            for (int k = 0; k < 12; k++)
		//            {
		//                // if it's an even row
		//                if (j % 2 == 0)
		//                {
		//                    // subtract 12
		//                    val2send = (int) Math.Abs( zMap[12*j+k]-12 );
		//                }
		//                // else it's an odd row
		//                else
		//                {
		//                    // send the same
		//                    val2send = (int) zMap[12*j+k];
		//                }
		//                data2send = (char)val2send;
		//                SerialPort.Write( data2send.ToString() );
		//            }
		//        }

	}

	// Send command to stop all motors and rendering
	private void ResetShapeDisplay() {
		// Send the reset command byte 
		int val2send = ZeroCMD;
		char data2send = (char) val2send;
		Serial.Write( data2send.ToString() );
	} 

	// Prints the current pin values, useful for debugging
	private void printZmap() {
		int countZeros = 0;
		for (int i = 0; i < ( zMap.Count ); i++) {
			// convert from m to mm
			int val = (int) ( zMap [i] * convFactor );
			print( "[" + i + "]  " + val );
			if (val == 0) {
				countZeros++;
			}
		}
		Debug.Log ("Real num of pins at zero: " + countZeros);
		Debug.Log ("Real num of pins up: " + (zMap.Count - countZeros) );
	}
}
