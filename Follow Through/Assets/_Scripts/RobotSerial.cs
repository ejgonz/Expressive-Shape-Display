/// <summary>
/// Handles serial communication (receiving + sending) either in a 
/// separate thread or using coroutines for an omni robot.
/// Author: A. Siu
/// June 27, 2017
/// </summary>

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using System.IO;
using System.IO.Ports;
using System;

using System.Threading;

public class RobotSerial : MonoBehaviour
{
	// Init a static reference to serial port. 
	// Use this to access from other scripts.
	public static RobotSerial Instance;

	#region Properties

	// The serial port
	private SerialPort SerialPort;

	// Select to handle serial comm in a separate thread
	// or as coroutine
	public enum LoopUpdateMethod
	{ Threading, Coroutine }

	// Current loop update method
	public LoopUpdateMethod UpdateMethod =
		LoopUpdateMethod.Threading;

	// Thread used to recieve and send serial data
	private Thread serialThread;

	// List of all com ports available on the system
	private ArrayList comPorts =
		new ArrayList();

	// If set to true then open the port when the start
	// event is called.
	public bool OpenPortOnStart = true;

	// Current com port and set of default
	public string ComPort;

	// Current baud rate and set of default
	public int BaudRate = 115200;

	// Timeout value - This is needed to force
	// the Coroutine method to end a read if no
	// data is being sent to the port.
	public int ReadTimeout = 10;
	// Write timeout value
	public int WriteTimeout = 10;

	// Property used to run/keep alive the serial thread loop
	private bool isRunning = false;
	public bool IsRunning
	{
		get { return isRunning; }
		set { isRunning = value; }
	}

	// Raw data received
	private string rawData = "Ready";
	public string RawData
	{
		get { return rawData; }
		set { rawData = value; }
	}

	// Storage for parsed incoming data
	private string[] parsedData;
	public string[] ParsedData
	{
		get { return parsedData; }
		set { parsedData = value; }
	}

	// The values separator used to create the ParsedData
	public char ValuesSeparator = '\n';

	// Data sending variables
	// Display automatic refresh rate
	public float refreshRate = 0.002f; //[s]
	// Enable sending data to hw at fixed rate
	private bool automaticSending = false;
	private float counter;
	private bool sendRobotPos = false;
	private bool stopRobot = false;
    // variables to send over serial
    byte[] dataByteArray;
	public OmniPositionControl omniPositionControl;

	// Robot serial rate parameters
	private float lastSendTime = 0.0f;
	private float sendOmniPosRate = 0.01f;	//[s]

	// lock this object to ensure variables are not modified while sending
	object LockSerialThread = new object();

	// Message commands for omni
	private const int MOVE_CMD = 127;
	private const int  STOP_CMD = 126;

	// conversion factor
	private float convFactor = 1000; // [mm/m]
	private int maxZ = 40;

	#region Delegates & Events
	// Define a delegate and event to fire off notifications to registered objects
	// Any number of these events can be created
	// Delegate and event for when a new line is received 
	public delegate void RobotSerialReceivedDataEventHandler(string[] data, string rawData);
	public static event RobotSerialReceivedDataEventHandler RobotSerialReceivedDataEvent;
	#endregion Delegates & Events

	// Set to false for minimal print statements
	public bool debug = false;

	#endregion Properties

	#region Unity Frame Events

	/// <summary>
	/// The awake call is used to populate refs to the gui elements used in this
	/// example. These can be removed or replaced if needed with bespoke elements.
	/// This will not affect the functionality of the system. If we are using awake
	/// then the script is being run non staticaly ie. its initiated and run by
	/// being dropped onto a gameObject, thus enabling the game loop events to be
	/// called e.g. start, update etc.
	/// </summary>
	void Awake()
	{
		// Define the script Instance
		Instance = this;
	}

	/// <summary>
	/// The start call is used to populate a list of available com ports on the
	/// system. The correct port can then be selected via the respective guitext
	/// or a call to UpdateComPort();
	/// </summary>
	void Start()
	{
        
		// Population of comport list via system.io.ports
		PopulateComPorts();

		// If set to true then open the port  or look for one.
		if (OpenPortOnStart) { OpenSerialPort(); }

		// Register for a notification of the SerialDataReceivedEvent
		RobotSerialReceivedDataEvent += 
			new RobotSerialReceivedDataEventHandler(RobotSerial_SerialReceivedEvent);

		Debug.Log ("Initialized RobotSerial");
		Debug.Log ("Press 'Q' to send move command to robot.");
		Debug.Log ("Press 'W' to send stop command to robot.");

	}

	/// <summary>
	/// The update frame call is used to provide caps for sending data to the arduino
	/// triggered via keypress. This can be replaced via use of the static functions
	/// SendSerialData() & SendSerialDataAsLine(). Additionaly this update uses the
	/// RawData property to update the gui. Again this can be removed etc.
	/// </summary>
	void Update()
	{
		// Check if the serial port exists and is open
		if (SerialPort == null || SerialPort.IsOpen == false) { return; }

		// Press 'Q' to send data to Teensy continuously
		if (Input.GetKeyDown (KeyCode.Q) ) {
            automaticSending = !automaticSending;
			counter = refreshRate;
			if (debug) {
				if (automaticSending) {
					Debug.Log ("robot sending on");
				} else {
					Debug.Log ("robot sending off");
					stopRobot = true;
				}
			}
		}

		else if (Input.GetKeyDown (KeyCode.W)) {
			stopRobot = true;
		}

        
        // If auto mode enabled
        if (automaticSending)
        {
            counter -= Time.deltaTime; //decrement counter if counter done, send the data
            if (counter < 0.0f)
            {
                dataByteArray = omniPositionControl.getDataMessage ();
                sendRobotPos = !sendRobotPos;
            }
        }

    }

	/// <summary>
	/// This function is called when the MonoBehaviour will be destroyed.
	/// OnDestroy will only be called on game objects that have previously
	/// been active.
	/// </summary>
	void OnDestroy()
	{
		// Remove event notifiation registration
		if (RobotSerialReceivedDataEvent != null)
			RobotSerialReceivedDataEvent -= RobotSerial_SerialReceivedEvent;
	}

	/// <summary>
	/// Clean up the thread and close the port on application close event.
	/// </summary>
	void OnApplicationQuit()
	{
		// Call to cloase the serial port
		CloseSerialPort();

		Thread.Sleep(500);

		if (UpdateMethod == LoopUpdateMethod.Threading)
		{
			// Call to end and cleanup thread
			StopSerialThread();
		}

		if (UpdateMethod == LoopUpdateMethod.Coroutine)
		{
			// Call to end and cleanup coroutine
			StopSerialCoroutine();
		}

		Thread.Sleep(500);
	}

	#endregion Unity Frame Events


	#region Notification Events
	/// <summary>
	/// Data parsed serialport notification event
	/// </summary>
	/// <param name="Data">string</param>
	/// <param name="RawData">string[]</param>
	void RobotSerial_SerialReceivedEvent(string[] Data, string RawData)
	{
		print("Data recieved from robot port: " + RawData);
	}
	#endregion Notification Events


	#region Object Serial Port

	/// <summary>
	/// Opens the defined serial port and starts the serial thread
	/// </summary>
	public void OpenSerialPort()
	{
		try
		{

			if (ComPort == "") {
				// try opening first port on startup
				ComPort = GetDefaultPort ();
				// if it still couln't find a port then return
				if ( ComPort == "" ) {
					print ("Error: Couldn't find robot serial port.");
					return;
				}
			} else {
				print("Opening robot serial port: " + ComPort);
			}

			// Initialise the serial port
			SerialPort = new SerialPort(ComPort, BaudRate);

			SerialPort.ReadTimeout = ReadTimeout;

			SerialPort.WriteTimeout = WriteTimeout;

			// Open the serial port
			SerialPort.Open();

			// clear input buffer from previous garbage
			SerialPort.DiscardInBuffer ();

			if (UpdateMethod == LoopUpdateMethod.Threading)
			{
				// If the thread does not exist then start it
				if (serialThread == null) { StartSerialThread(); }
			}

			if (UpdateMethod == LoopUpdateMethod.Coroutine)
			{
				if (isRunning == false)
				{
					StartSerialCoroutine();
				}
				else
				{
					isRunning = false;

					// Give it chance to timeout
					Thread.Sleep(100);

					try
					{
						StopCoroutine("SerialCoroutineLoop");
					}
					catch(Exception ex)
					{
						print("Error N: " + ex.Message.ToString());
					}

					// Restart it once more
					StartSerialCoroutine();
				}
			}

			if (debug) {
				print("Robot serial successfully opened!");
			}

		}
		catch (Exception ex)
		{
			// Failed to open com port or start serial thread
			Debug.Log("Error 1: " + ex.Message.ToString());
		}
	}

	/// <summary>
	/// Closes the serial port so that changes can be made or communication
	/// ended.
	/// </summary>
	public void CloseSerialPort()
	{
		try
		{
			// Close the serial port
			SerialPort.Close();
		}
		catch (Exception ex)
		{
			if (SerialPort == null || SerialPort.IsOpen == false)
			{
				Debug.Log("Robot serial port already closed.");	
			}
			else
			{
				// Failed to close the serial port
				Debug.Log("Error 2B: " + ex.Message.ToString());
			}
		}

		print("Robot serial port closed!");

	}

	/// <summary>
	/// Look for available ports and return the first.
	/// </summary>
	/// <returns>The port name.</returns>
	static string GetDefaultPort ()
	{

		string[] portNames;

		switch (Application.platform) {

		case RuntimePlatform.OSXPlayer:
		case RuntimePlatform.OSXEditor:
		case RuntimePlatform.OSXDashboardPlayer:
		case RuntimePlatform.LinuxPlayer:

			portNames = System.IO.Ports.SerialPort.GetPortNames ();

			if (portNames.Length == 0) {
				portNames = System.IO.Directory.GetFiles ("/dev/");                
			}

			foreach (string portName in portNames) {                                
				if (portName.StartsWith ("/dev/tty.usb") || portName.StartsWith ("/dev/ttyUSB"))
					return portName;
			}                
			return "";

		default: // Windows

			portNames = System.IO.Ports.SerialPort.GetPortNames ();

			// Defaults to last port in list (most chance to be an Arduino port)
			if (portNames.Length > 0)
				return portNames [portNames.Length - 1];
			else
				return "";
		}
	}

	#endregion Object Serial Port


	#region Serial Thread

	/// <summary>
	/// Function used to start seperate thread for reading serial
	/// data.
	/// </summary>
	public void StartSerialThread()
	{
		try
		{
			// define the thread and assign function for thread loop
			serialThread = new Thread(new ThreadStart(SerialThreadLoop));
			// Boolean used to determine the thread is running
			isRunning = true;
			// Start the thread
			serialThread.Start();

			if (debug) {
				print("Robot serial thread started!");
			}
		}
		catch (Exception ex)
		{
			// Failed to start thread
			Debug.Log("Error 3: " + ex.Message.ToString());
		}
	}

	/// <summary>
	/// The serial thread loop. A seperate thread used to recieve
	/// serial data and sending the zMap array thus not affecting 
	/// generic unity playback etc.
	/// </summary>
	private void SerialThreadLoop()
	{
		while (isRunning)
		{ 

			GenericSerialLoop(); 

			// lock variables when used inside the thread
			lock(LockSerialThread) {
				SendRobotData();
			}

		}

		if (debug) {
			print ("Ending robot serial thread!");
		}
	}

	/// <summary>
	/// Function used to stop the serial thread and kill
	/// off any instance
	/// </summary>
	public void StopSerialThread()
	{
		// Set isRunning to false to let the while loop
		// complete and drop out on next pass
		isRunning = false;

		// Pause a little to let this happen
		Thread.Sleep(100);

		// If the thread still exists kill it
		// A bit of a hack using Abort :p
		if (serialThread != null)
		{
			serialThread.Abort();
			// serialThread.Join();
			Thread.Sleep(100);
			serialThread = null;
		}

		// Reset the serial port to null
		if (SerialPort != null)
		{ SerialPort = null; }

		if (debug) {
			print ("Ended robot serial loop thread!");
		}

	}

	#endregion Serial Thread


	#region Serial Coroutine

	/// <summary>
	/// Function used to start coroutine for reading serial
	/// data.
	/// </summary>
	public void StartSerialCoroutine()
	{
		isRunning = true;

		StartCoroutine("SerialCoroutineLoop");
	}

	/// <summary>
	/// A Coroutine used to recieve serial data thus not
	/// affecting generic unity playback etc.
	/// </summary>
	public IEnumerator SerialCoroutineLoop()
	{
		while (isRunning)
		{
			// Handle receiving data
			GenericSerialLoop();

			// Send data back
			SendRobotData ();

			yield return null;
		}

		print("Ending robot coroutine!");
	}

	/// <summary>
	/// Function used to stop the coroutine and kill
	/// off any instance
	/// </summary>
	public void StopSerialCoroutine()
	{
		isRunning = false;

		Thread.Sleep(100);

		try
		{
			StopCoroutine("SerialCoroutineLoop");
		}
		catch (Exception ex)
		{
			print("Error 2A: " + ex.Message.ToString());
		}

		// Reset the serial port to null
		if (SerialPort != null)
		{ SerialPort = null; }

		print("Ended robot serial loop coroutine!");
	}

	#endregion Serial Coroutine


	#region Static Functions

	/// <summary>
	/// Function used to send string data over serial with
	/// an included line return
	/// </summary>
	/// <param name="data">string</param>
	public void SendSerialDataAsLine( string data )
	{
		if (SerialPort != null)
		{ SerialPort.WriteLine(data); }
	}

	/// <summary>
	/// Function used to send string data over serial without
	/// a line return included.
	/// </summary>
	/// <param name="data"></param>
	public void SendSerialData( string data )
	{
		if (SerialPort != null)
		{ SerialPort.Write(data); }
	}

	#endregion Static Functions


	#region robot command functions

	/// <summary>
	/// Forwards the display data to the hardware
	/// and resets thread-safe flags.
	/// </summary>
	private void SendRobotData () {
		if (SerialPort.IsOpen) {
			if (sendRobotPos) {
				SendRobotPos ();
                sendRobotPos = false;
			} else if (stopRobot) {
				StopRobot ();
				stopRobot = false;
			}
		}
	}

	/// <summary>
	/// Creates command to send the robot position.
	/// </summary>
	private void SendRobotPos() {
		// Send the move command byte 
		int val2send = MOVE_CMD;
		char data2send = (char) val2send;
		SerialPort.Write( data2send.ToString() );

		// Send speed, direction, omega calculated by OmniPositionControl script
		SerialPort.Write( dataByteArray, 0, 6 );
        
		if (debug)
			Debug.Log ("Sent robot data: " + dataByteArray[0] + " " + dataByteArray[1] + " " + dataByteArray[2] + " " + dataByteArray[3] + " " + dataByteArray[4] + " " + dataByteArray[5]);

	} // end SendRobotPos

	private void StopRobot() {
		// Send the move command byte 
		int val2send = STOP_CMD;
		char data2send = (char) val2send;
		SerialPort.Write( data2send.ToString() );
	}

	#endregion robot command functions

	/// <summary>
	/// The serial thread loop & the coroutine loop both utilise
	/// the same code with the exception of the null return on
	/// the coroutine.
	/// </summary>
	private void GenericSerialLoop()
	{
		try
		{
			// Check that the port is open. If not skip and do nothing
			if (SerialPort.IsOpen)
			{

				// Read serial data until a 'n' character is recieved
				string rData = SerialPort.ReadLine();

				// If the data is valid then do something with it
				if (rData != null && rData != "")
				{
					// Store the raw data
					RawData = rData;

					// Split the raw data into chunks via ValueSeparator and store it
					// into a string array
					ParsedData = RawData.Split(ValuesSeparator);

					// Parse data
					ParseSerialData(ParsedData, RawData);
				}

			}
		}
		catch (TimeoutException timeout)
		{
			// Triggered mostly by coroutine
		}
		catch (Exception ex)
		{
			// This could be thrown if we close the port whilst the thread
			// is reading data.
			if (SerialPort.IsOpen)
			{
				Debug.Log("Error 4: " + ex.Message.ToString());
			}
			else
			{
				Debug.Log("Error 5: Port Closed Exception! " + ex.Message.ToString());
			}
		}
	}

	/// <summary>
	/// Function to parse data received
	/// </summary>
	/// <param name="data">string of raw data</param>
	private void ParseSerialData(string[] data, string rawData)
	{
		// If received data is valid, fire a notification to all registered objects
		if (data != null && rawData != string.Empty)
		{
			if (RobotSerialReceivedDataEvent != null)
				RobotSerialReceivedDataEvent(data, rawData);
		}
	}

	/// <summary>
	/// Function that utilises system.io.ports.getportnames() to populate
	/// a list of com ports available on the system.
	/// </summary>
	public void PopulateComPorts()
	{
		// Loop through all available ports and add them to the list
		foreach (string cPort in System.IO.Ports.SerialPort.GetPortNames())
		{
			comPorts.Add(cPort); 
		}
	}

	/// <summary>
	/// Function used to update the current selected com port
	/// </summary>
	public string UpdateComPort()
	{
		// If open close the existing port
		if (SerialPort != null && SerialPort.IsOpen)
		{ CloseSerialPort(); }

		// Find the current id of the existing port within the
		// list of available ports
		int currentComPort = comPorts.IndexOf(ComPort);

		// check against the list of ports and get the next one.
		// If we have reached the end of the list then reset to zero.
		if (currentComPort + 1 <= comPorts.Count - 1)
		{
			// Inc the port by 1 to get the next port
			ComPort = (string)comPorts[currentComPort + 1];
		}
		else
		{
			// We have reached the end of the list reset to the
			// first available port.
			ComPort = (string)comPorts[0];
		}

		// Return the new ComPort just in case
		return ComPort;
	}

}



