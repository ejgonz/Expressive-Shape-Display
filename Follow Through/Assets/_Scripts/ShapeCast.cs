/// <summary>
/// Raycasting script to simulate a pin shape display
/// and determine pin positions.
/// Author: A. Siu
/// March 21, 2017
/// </summary>

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.VR;

public class ShapeCast : MonoBehaviour {

	public Material supportMaterial = null;   // material for the support layer

	// ***    User defined variables    ** //
    public bool VRenabled = true;
    public bool UseOptiTrack = true;
    public OptitrackStreamingClient StreamingClient;

    public bool UseHistory = false;
    public bool UseToolglassExtrude = false;
    public bool UseToolglassExtrudeCut = false;

    #region hand tracking vars
    public bool FollowHand = false;
    public bool TrackHand = false;
    private GameObject[] handArray; // Array of GameObjects comprising hand (fingers and palm)
    public GameObject hand; // GameObject to indicate the center of the tracked hand
    #endregion hand tracking vars

    public float toolglassExtrudeOffset = 0.1f;     // tooloffset height from display
    public float toolglassExtrudeCutOffset = 0.1f;  // tooloffset height from display

	public int displayXsize = 24;         // horizontal display size (# of pins)
	public int displayZsize = 24;         // vertical display size (# of pins)

	public float pinSize = 0.0048f;        // [m] size of a pin (4.8 mm)
	public float pinHeight = 0.0635f;      // [m] height / travel path of a pin (2.5in = 63.5cm)
	public float pinSpacing = 0.003f;      // [m] spacing between pins (3 mm)
    public float maxPinTravel = 0.05f;     // [m] max travel distance for pin

	public float highSpeed = 76.0f / 1000.0f; // [m/s]
	public float deadZone = 1.0f/1000f;       // [m]

	public bool displayPin = true;      // display pin rendering simulation
	public bool speedControl = true;    // pins move at specified speed

	public float totalWidth { get; private set; }  // horizontal space to cover
	public float totalHeight { get; private set; } // vertical space to cover

    public float initialY = 0.008f;  // [m] where to place pins in Y after startup (8 mm out from the support)

    // *** Vars for sinusoidal mode *** //
	public float A = 0.0635f;  // amplitude of the sine wave

	// ***    Private variables    *** //
	private float lowSpeed;      // [m/s]
	private float currentSpeed;  // the high speed is the default speed
    
	private GameObject originObject;   // GameObject to indicate origin position for pin rendering
    private GameObject pinSupport;             // Pin support used for reference in ray casting
    private float pinCenterOffset;
    private GameObject displayBase;
   
    private GameObject[] attachedObjects; // Array of all objects attached to move with display

	private List<GameObject> pins = new List<GameObject>(); // List of pin GameObjects
	private List<float> zMap = new List<float>();

    private Vector3 newPinPos; //Update the new pin position here

    //private bool initDone; //Initialize game objects only after optitrack starts streming
    public bool initDone
    {
        get;
        private set;
    }

	private enum State
	{
		SimpleRaycast, 
		SpeedControlRaycast,
		Sinusoidal
	};

	private State currentState;

	// Use this for initialization
	void Start () {

        initDone = false;

        // Enable VR
        VRSettings.enabled = VRenabled;

		// set the default starting speed
		currentSpeed = highSpeed;
		lowSpeed = highSpeed / 5.0f;

		// set the current state
		currentState = State.SimpleRaycast;
        
		// Based on display parameters, determine the rendering plane's size
		totalWidth = displayXsize * ( pinSize + pinSpacing ) - pinSpacing;
		totalHeight = displayZsize * ( pinSize + pinSpacing ) - pinSpacing;  

        if (UseOptiTrack) {
            if ( this.StreamingClient == null ) {
                this.StreamingClient = OptitrackStreamingClient.FindDefaultClient();
                // If we still couldn't find one, disable this component.
                if ( this.StreamingClient == null )
                {
                    Debug.LogError( GetType().FullName + ": Streaming client not set, and no " + typeof( OptitrackStreamingClient ).FullName + " components found in scene; disabling this component.", this );
                    return;
                }
            }
        } else {
            InitDisplay();
        }

        // By default a pin's center is at half the height
        // Add this offset to the y-pos of the pin when changing
        // its position
        pinCenterOffset = -pinHeight / 2.0f + initialY;

	}

    // FixedUpdate called at physics rate
    void FixedUpdate()
    {

        // If using optitrack and not yet initialized return
        if (UseOptiTrack && !initDone)
        {
            return;
        }

        // Iterate through each pin
        for (int i = 0; i < pins.Count; i++)
        {

            GameObject pin = pins[i]; // save the curr game object pin
                                      // raycast to get new pin pos
            float targetPos;

            // depending on current state update pin pos
            if (currentState == State.SpeedControlRaycast)
            {
                // set the target pos based on raycast
                Raycast(pin, out targetPos);
                BangBangControl(pin, targetPos);

            }
            else if (currentState == State.Sinusoidal)
            {
                A = (float)pinHeight / 5.2f;
                targetPos = A * Mathf.Sin(2 * Mathf.PI * Time.time * pin.transform.position.x)
                          + A * Mathf.Sin(2 * Mathf.PI * Time.time * pin.transform.position.z);
                BangBangControl(pin, targetPos);
            }

            // default to simple raycast mode
            else
            {
                // set the target pos based on raycast
                Raycast(pin, out targetPos);

                if (targetPos > maxPinTravel)
                {
                    targetPos = maxPinTravel;
                }
                else if (targetPos < 0.0005f)
                {
                    targetPos = 0;
                }

                // set target pos relative to origin object
                newPinPos.x = pin.transform.localPosition.x;
                newPinPos.y = targetPos + pinCenterOffset;
                newPinPos.z = pin.transform.localPosition.z;
                pin.transform.localPosition = newPinPos;
            }

            // save the position in a list for serial comm
            zMap[i] = targetPos;

        } // end for each pin

    } // end fixedupdate

    void Update () {
        
        if (UseOptiTrack) {
            // If OptiTrak is streaming and we haven't initialized 
            if (StreamingClient.streamingFrames && initDone == false) {
                InitDisplay(); // init pins
                initDone = true;
            }
            else if (StreamingClient.streamingFrames == false){
                return;
            }
        }
        
        if (!UseOptiTrack) {
			// check for moving the plane
			if ( Input.GetKey(KeyCode.DownArrow) )
			{
				originObject.transform.Translate (new Vector3(0f,0.002f,0.0f));
			} else if (Input.GetKey(KeyCode.UpArrow))
			{
				originObject.transform.Translate (new Vector3(0.0f,-0.002f,0.0f));
			} else if (Input.GetKey(KeyCode.RightArrow))
			{
				originObject.transform.Translate (new Vector3(0.0f,0.0f,0.002f));
			} else if (Input.GetKey(KeyCode.LeftArrow))
			{
				originObject.transform.Translate (new Vector3(0.0f,0.0f,-0.002f));
			}
        }

        // update the display to track the position of the hand
        if (TrackHand) {
            hand.transform.position = calculateCentroid(handArray);
            if (FollowHand) {  
                //originObject.transform.position = new Vector3(hand.transform.position.x,  originObject.transform.position.y, hand.transform.position.z);
             }
        }

		// check for toggling pin speeds
		if (Input.GetKeyDown (KeyCode.Q)) {
			currentSpeed = lowSpeed;
		} else if (Input.GetKeyDown (KeyCode.W)) { 
			currentSpeed = highSpeed;
		}

        // debugging pin positions
        if (Input.GetKeyDown(KeyCode.P))
        {
            for (int i = 0; i < pins.Count; i++)
            {
                Debug.Log("pin: " + i + "; pos: " + pins[i].transform.position.z);
            }
        }
            
	} // end update

    /// <summary>
    /// Returns a list with each pin's height
    /// </summary>
    /// <returns></returns>
	public List<float> GetZMap () {
		//List<float> returnlist = new List<float>();
		//foreach( float pos in zMap ) {
		//	returnlist.Add (pos);
		//} // endfor
		return zMap;
	} 

    /// <summary>
    /// Returns an array with each pin's height
    /// </summary>
    /// <returns></returns>
	public float[] GetZMapArray () {
		return GetZMap().ToArray();
	} 

    /// <summary>
    /// Get pin's speed.
    /// </summary>
    /// <returns></returns>
	public string GetSpeed() {
		if (currentSpeed == lowSpeed) {
			return "low";
		} 
		return "high";
	} 

    /// <summary>
    /// Get current display state/mode.
    /// </summary>
    /// <returns></returns>
	public string GetState() {
		if (currentState == State.SimpleRaycast) {
			return "simple raycast";
		} else if (currentState == State.SpeedControlRaycast) {
			return "speed control raycast";
		} else if (currentState == State.Sinusoidal) {
			return "sinusoidal";
		}
		return "";
	} 
		
    //// *************** Private Functions *************** ////

    private void InitDisplay () {

		// Create the origin game object for reference
		originObject = new GameObject ("OriginObject");
        if ( UseOptiTrack )
        {
            originObject.transform.parent = this.transform;
            originObject.transform.localRotation = Quaternion.AngleAxis(90, Vector3.up); //Quaternion.identity;
            originObject.transform.localPosition = Vector3.zero;
        }

        // Find all attached objects and fix to display
        attachedObjects = GameObject.FindGameObjectsWithTag("Attach");
        foreach (GameObject attached_object in attachedObjects) {
            attached_object.transform.SetParent(originObject.transform);
        }

        // Create a GameObject to represent the center of the hand from the tracked finger and palm positions
        if (TrackHand) {
            //TODO I changed Finger3 to Finger1
            handArray = new GameObject[]{ GameObject.Find("Finger1"),  GameObject.Find("Finger2"), GameObject.Find("Palm") };
            hand.transform.position = calculateCentroid(handArray);
            hand.transform.localScale = new Vector3 (0.03f, 0.03f, 0.03f);
        }
        
		// Create the pin support object
		pinSupport = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pinSupport.name = "Pin Support";
	    pinSupport.transform.parent = originObject.transform;
        pinSupport.transform.localScale = new Vector3(totalWidth, 0.0001f, totalHeight);
        pinSupport.transform.localPosition = new Vector3( -totalWidth / 2, 0, totalHeight / 2 );
        pinSupport.transform.localRotation = Quaternion.identity;
        pinSupport.GetComponent<Renderer>().material = supportMaterial;

        // Create base of the shape display
        displayBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
        displayBase.name = "Display Base";
        displayBase.transform.parent = pinSupport.transform;
        displayBase.transform.localScale = 
            new Vector3( (totalWidth + 0.005f)*1/displayBase.transform.parent.localScale.x, 
            (0.3f)*1/displayBase.transform.parent.localScale.y, 
            (totalHeight + 0.005f)*1/displayBase.transform.parent.localScale.z);
        displayBase.transform.localPosition = new Vector3(0, -0.3f / 2 * 1 / displayBase.transform.parent.localScale.y, 0);
        displayBase.transform.localRotation = Quaternion.identity;
        displayBase.GetComponent<Collider>().enabled = false;
        displayBase.GetComponent<Renderer>().material = supportMaterial;

        // generate whole list of pin objects
        for (int i = 0; i < displayXsize; i++) {
			for (int j = 0; j < displayZsize; j++) {
				GameObject singlePin = GameObject.CreatePrimitive(PrimitiveType.Cube);
				singlePin.transform.parent = originObject.transform;
				singlePin.name = "Pin" + (j + i * displayZsize);

                // set pins inactive if not displaying
				if (!displayPin) {
					singlePin.GetComponent<Renderer>().enabled = false;
				}

				singlePin.GetComponent<Collider>().enabled = false;
				singlePin.transform.localScale = new Vector3 (pinSize, pinHeight, pinSize);

                newPinPos = new Vector3 ( -(i*pinSize + i*pinSpacing + pinSize/2.0f),
										    pinCenterOffset,
                                          (j*pinSize + j*pinSpacing + pinSize/2.0f) );
                singlePin.transform.localPosition = newPinPos;
                
				// Initialize the lists
				pins.Add ( singlePin ); 
				zMap.Add ( initialY );	 // to send to serial

			} // end for cols
		} // end rows

    }

    /// <summary>
    /// Bang bang control for pin position.
    /// </summary>
    /// <param name="pin"> the pin of interest </param>
    /// <param name="targetPos"> the pin's target pos</param>
	private void BangBangControl(GameObject pin, float targetPos) {
        float currPos = pin.transform.position.y;
		//check if the pin has reached its target
		if (!reachedTarget (currPos, targetPos)) {
			// check if pin should move forward or back
			if (currPos > targetPos) {
				pin.transform.Translate (Vector3.back * currentSpeed * Time.deltaTime);
			} else {
				pin.transform.Translate (Vector3.forward * currentSpeed * Time.deltaTime);
			}
		}
	} 
    
    /// <summary>
    /// Checks if the current pos is within tolerance of the target pos
    /// </summary>
    /// <param name="currPos"> the current position </param>
    /// <param name="targetPos"> the target position </param>
    /// <returns></returns>
	private bool reachedTarget ( float currPos, float targetPos  ) {
		// if within deadzone 
		if ((currPos < (targetPos + deadZone)) && (currPos > (targetPos - deadZone))) {
			// pin has reached the target
			return true;
		}
		// else pin is still not at target
		return false;
	} // end reachedTarget
    
    /// <summary>
    /// Raycast to find target position of a given pin.
    /// </summary>
    /// <param name="pin"> The pin to find the position based on raycast. </param>
    /// <param name="target"> The travel distance to move the pin. </param>
    /// <returns> Returns true if successful, false otherwise. </returns>
	private bool Raycast(GameObject pin, out float target) {

        // Default to initial pos
        if ( UseToolglassExtrudeCut ) {
            target = maxPinTravel; 
        } else {
            target = 0;
        }

        // Raycast origin arbitrarily choose 100 m above and normal to the pin surface
        float hoverHeight = 0.5f;
        Vector3 rayOrig = new Vector3( pin.transform.position.x, hoverHeight, pin.transform.position.z );
		
        // Do the ray casting now to find the surface of the GameObject that will be rendered
        Ray downRay = new Ray(rayOrig, -Vector3.up);
		RaycastHit hitInfo;
        
		int IgnoreRaycastLayer =  1 << 3;

        // If we hit something and it's not in the ignore layer
		if ( Physics.Raycast (downRay, out hitInfo, IgnoreRaycastLayer ) ) { 

            target = hitInfo.point.y - pinSupport.transform.position.y; 
            //Debug.DrawRay(rayOrig, -Vector3.up, Color.red, 100.0f);

            if ( UseToolglassExtrude ) {
                target = hitInfo.point.y - pinSupport.transform.position.y - toolglassExtrudeOffset; 
            }
            else if ( UseToolglassExtrudeCut ) {
                target = hitInfo.point.y - pinSupport.transform.position.y - toolglassExtrudeCutOffset; 
            }

            return true;

        }

        return false;
	}

    /// <summary>
    /// Return centroid of multiple GameObject positions
    /// </summary>
    /// <param name="handpoints"> the array of GameObjects </param>
    private Vector3 calculateCentroid(GameObject[] handPoints) {
        Vector3 centroid = new Vector3(0,0,0);

        foreach (GameObject component in handPoints) {
            centroid += component.transform.position;
        }
        return centroid/(handPoints.Length);

    }
    
} // end ShapeCast

