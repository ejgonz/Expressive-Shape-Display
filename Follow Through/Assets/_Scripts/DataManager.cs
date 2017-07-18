/// <summary>
/// 
/// DEPRECATED - Instead use SerialThread.cs
/// 
/// Author: A. Siu
/// </summary>
/// 
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class DataManager : MonoBehaviour {

	// shape display object in unity
	private ShapeCast shapeRenderer;
	public GameObject renderingPlane; 

	// Mesh for displaying a grayscale image of the pin display
	private Mesh DisplayMesh; 
	public int length;
	private Vector3[] points;
	private int[] index;
	private Color[] colors;

	// list to store the shape display pin positions
	private List<float> zMap = new List<float>();

	const string REQ_CMD = "255";

	// Use this for initialization
	void Start () {
		// Initialize the mesh with 0s
		shapeRenderer = renderingPlane.GetComponent<ShapeCast>(); 
		//CreateGrayscaleImage ();

		Debug.Log ("Initialized DataManager");
	}
	
	// Update is called once per frame
	void Update () {

		// Refresh the display list data 
		zMap = shapeRenderer.GetZMap();

		// Send data to Teensy
		if (Input.GetKeyDown (KeyCode.S)) {
			StopShapeRendering ();
		} //endif

	} //end Update

	// Check if command from shape display has been sent
	// This function is called by the Serial manager when a new line
	// is received
	void OnSerialLine(string line) {
		Debug.Log ("Received a line " + line);
		if ( string.Equals( line.Trim(), "255" ) ) {
			Debug.Log ("Received a refresh request from ShapeDisplay Master");
			// Send the display data
			SendDisplayData ();
		}
	} // end OnSerialLine

	// Send command to stop all motors and rendering
	private void StopShapeRendering() {
		Debug.Log ("Stopping display");
		Serial.WriteLn("254");
	} // end StopShapeRendering

	// Send all the zMap data to the master shape display
	private void SendDisplayData() {
		for (int i = 0; i < ( zMap.Count - 1 ); i++) {
		//for (int i = 0; i < 2; i++) {
			Serial.WriteLn("1");
		}
		Serial.WriteLn("254");
		Debug.Log ("Sent display data");
	} // end SendDisplayData

} // end DataManager
	
